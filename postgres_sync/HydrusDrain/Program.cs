using System.Text.Json;
using HydrusReadModel;
using Npgsql;

namespace HydrusDrain;

/// <summary>
/// Drains hydrus's CDC outbox into the postgres read model.
///
/// At-least-once: a batch and its cursor advance commit in ONE postgres
/// transaction, so a crash replays the tail rather than losing it. Every
/// statement in PostgresSink is therefore idempotent.
///
/// Env:
///   HYDRUS_DB_DIR        path to the hydrus db folder (contains client.db)
///   HYDRUS_PG_CONNECTION npgsql connection string
///   HYDRUS_DRAIN_BATCH   outbox rows per transaction (default 2000)
///   HYDRUS_DRAIN_PERIOD  seconds between polls (default 5)
/// </summary>
public static class Program
{
    public static async Task<int> Main()
    {
        var dbDir = Environment.GetEnvironmentVariable("HYDRUS_DB_DIR");
        var connString = Environment.GetEnvironmentVariable("HYDRUS_PG_CONNECTION");

        if (string.IsNullOrWhiteSpace(dbDir) || string.IsNullOrWhiteSpace(connString))
        {
            Console.Error.WriteLine("set HYDRUS_DB_DIR and HYDRUS_PG_CONNECTION");
            return 2;
        }

        var batchSize = int.TryParse(Environment.GetEnvironmentVariable("HYDRUS_DRAIN_BATCH"), out var b) ? b : 2000;
        var period = int.TryParse(Environment.GetEnvironmentVariable("HYDRUS_DRAIN_PERIOD"), out var p) ? p : 5;

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        var dsb = new NpgsqlDataSourceBuilder(connString);
        dsb.MapEnum<ContentStatus>("hydrus.content_status");
        dsb.MapEnum<Filetype>("hydrus.filetype");
        dsb.MapEnum<ServiceType>("hydrus.service_type");
        dsb.MapEnum<SyncKind>("hydrus.sync_kind");
        dsb.MapEnum<DiscrepancyKind>("hydrus.discrepancy_kind");
        await using var db = dsb.Build();

        var sink = new PostgresSink(db);

        while (!cts.IsCancellationRequested)
        {
            try
            {
                var moved = await DrainOnceAsync(dbDir, db, sink, batchSize, cts.Token);

                // Only idle when we have caught up; otherwise keep pulling.
                if (!moved) await Task.Delay(TimeSpan.FromSeconds(period), cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"drain error, retrying in {period}s: {e.Message}");
                try { await Task.Delay(TimeSpan.FromSeconds(period), cts.Token); }
                catch (OperationCanceledException) { break; }
            }
        }

        return 0;
    }

    private static async Task<bool> DrainOnceAsync(
        string dbDir, NpgsqlDataSource db, PostgresSink sink, int batchSize, CancellationToken ct)
    {
        using var src = new HydrusSource(dbDir);

        var (lastSeq, storedDbId) = await sink.ReadCursorAsync(ct);
        var sourceDbId = src.SourceDbId();

        // Refuse to mix two hydrus installs into one read model -- the file_ids
        // would be meaningless and the damage is not obviously visible.
        if (storedDbId is not null && sourceDbId is not null && !storedDbId.SequenceEqual(sourceDbId))
            throw new InvalidOperationException(
                "this postgres read model was built from a different hydrus database; " +
                "point at the original, or rebuild from scratch");

        var rows = src.ReadOutbox(lastSeq, batchSize);
        if (rows.Count == 0) return false;

        await using var conn = await db.OpenConnectionAsync(ct);
        await using var tx = await conn.BeginTransactionAsync(ct);

        var serviceMap = await sink.SyncServicesAsync(conn, tx, src.ReadServices(), ct);

        foreach (var (seq, op, payload) in rows)
        {
            using var doc = JsonDocument.Parse(payload);
            await ApplyAsync(src, sink, conn, tx, serviceMap, op, doc.RootElement, ct);
        }

        var highest = rows[^1].Seq;
        await sink.CommitCursorAsync(conn, tx, highest, sourceDbId, ct);
        await tx.CommitAsync(ct);

        // Hydrus prunes its own outbox from this; the drain never writes SQLite.
        await System.IO.File.WriteAllTextAsync(
            Path.Combine(dbDir, "pg_outbox_watermark"), highest.ToString(), ct);

        Console.WriteLine($"applied {rows.Count} ops, seq -> {highest} (lag {src.MaxOutboxSeq() - highest})");

        return true;
    }

    private static async Task ApplyAsync(
        HydrusSource src, PostgresSink sink, NpgsqlConnection conn, NpgsqlTransaction tx,
        Dictionary<long, short> serviceMap, string op, JsonElement e, CancellationToken ct)
    {
        switch (op)
        {
            case "file_info":
            {
                var raw = e.GetProperty("r").EnumerateArray()
                    .Select(a => a.EnumerateArray().ToArray()).ToList();

                var hashIds = raw.Select(a => a[0].GetInt64()).ToList();
                var hashes = src.ResolveHashes(hashIds);

                var fileIds = await sink.EnsureFilesAsync(conn, tx, hashes.Values.ToList(), ct);

                var rows = new List<(int, long?, int?, int?, int?, long?, long?, bool?, long?)>();

                foreach (var a in raw)
                {
                    if (!hashes.TryGetValue(a[0].GetInt64(), out var h)) continue;
                    if (!fileIds.TryGetValue(Convert.ToHexString(h), out var fid)) continue;

                    rows.Add((fid, Num(a[1]), (int?)Num(a[2]), (int?)Num(a[3]), (int?)Num(a[4]),
                              Num(a[5]), Num(a[6]), Num(a[7]) is { } ha ? ha != 0 : null, Num(a[8])));
                }

                await sink.ApplyFileInfoAsync(conn, tx, rows, ct);
                break;
            }

            case "file_status":
            {
                if (!serviceMap.TryGetValue(e.GetProperty("s").GetInt64(), out var svc)) break;

                var hashes = src.ResolveHashes(Longs(e.GetProperty("h")));
                var fileIds = await sink.EnsureFilesAsync(conn, tx, hashes.Values.ToList(), ct);

                var ts = e.GetProperty("t").ValueKind == JsonValueKind.Null
                    ? (DateTimeOffset?)null
                    : DateTimeOffset.FromUnixTimeMilliseconds(e.GetProperty("t").GetInt64());

                await sink.ApplyFileStatusAsync(conn, tx, svc,
                    PostgresSink.MapStatus(e.GetProperty("st").GetInt32()),
                    fileIds.Values.ToArray(), ts, ct);
                break;
            }

            case "mappings":
            {
                if (!serviceMap.TryGetValue(e.GetProperty("s").GetInt64(), out var svc)) break;

                var groups = e.GetProperty("r").EnumerateArray()
                    .Select(g => (TagId: g[0].GetInt64(), HashIds: Longs(g[1]))).ToList();

                var tagText = src.ResolveTags(groups.Select(g => g.TagId));
                var hashes = src.ResolveHashes(groups.SelectMany(g => g.HashIds));

                var tagIds = await sink.EnsureTagsAsync(conn, tx, tagText.Values.ToList(), ct);
                var fileIds = await sink.EnsureFilesAsync(conn, tx, hashes.Values.ToList(), ct);

                var pairs = new List<(int, int)>();

                foreach (var g in groups)
                {
                    if (!tagText.TryGetValue(g.TagId, out var t)) continue;
                    if (!tagIds.TryGetValue(t, out var tid)) continue;

                    foreach (var hid in g.HashIds)
                        if (hashes.TryGetValue(hid, out var h) &&
                            fileIds.TryGetValue(Convert.ToHexString(h), out var fid))
                            pairs.Add((tid, fid));
                }

                await sink.ApplyMappingsAsync(conn, tx, svc,
                    PostgresSink.MapStatus(e.GetProperty("st").GetInt32()),
                    e.GetProperty("rm").GetBoolean(), pairs, ct);
                break;
            }

            case "url_add":
            case "url_del":
            {
                var fid = await SingleFileAsync(src, sink, conn, tx, e.GetProperty("h").GetInt64(), ct);
                if (fid is null) break;

                await sink.ApplyUrlAsync(conn, tx, fid.Value, e.GetProperty("u").GetString()!, op == "url_add", ct);
                break;
            }

            case "note_set":
            case "note_del":
            {
                var fid = await SingleFileAsync(src, sink, conn, tx, e.GetProperty("h").GetInt64(), ct);
                if (fid is null) break;

                await sink.ApplyNoteAsync(conn, tx, fid.Value, e.GetProperty("n").GetString()!,
                    op == "note_set" ? e.GetProperty("b").GetString() : null, ct);
                break;
            }

            case "phash_add":
            case "phash_del":
            {
                var fid = await SingleFileAsync(src, sink, conn, tx, e.GetProperty("h").GetInt64(), ct);
                if (fid is null) break;

                var phashes = src.ResolvePhashes(Longs(e.GetProperty("p"))).Values.ToArray();

                await sink.ApplyPhashesAsync(conn, tx, fid.Value, phashes, op == "phash_add", ct);
                break;
            }

            default:
                Console.Error.WriteLine($"unknown outbox op '{op}', skipping");
                break;
        }
    }

    private static async Task<int?> SingleFileAsync(
        HydrusSource src, PostgresSink sink, NpgsqlConnection conn, NpgsqlTransaction tx,
        long hashId, CancellationToken ct)
    {
        var hashes = src.ResolveHashes([hashId]);
        if (!hashes.TryGetValue(hashId, out var h)) return null;

        var ids = await sink.EnsureFilesAsync(conn, tx, [h], ct);

        return ids.TryGetValue(Convert.ToHexString(h), out var fid) ? fid : null;
    }

    private static List<long> Longs(JsonElement a) =>
        a.EnumerateArray().Select(x => x.GetInt64()).ToList();

    private static long? Num(JsonElement e) =>
        e.ValueKind == JsonValueKind.Null ? null : e.GetInt64();
}
