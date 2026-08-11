using System.Text.Json;
using HydrusReadModel;
using Npgsql;
using NpgsqlTypes;

namespace HydrusDrain;

/// <summary>
/// Applies outbox operations to the postgres read model.
///
/// Raw Npgsql rather than EF: this is the hot path, every statement is a bulk
/// upsert over an array parameter, and the change tracker would only get in the
/// way. EF owns the schema and migrations; this owns the writes.
///
/// Every statement is idempotent (ON CONFLICT), because the drain is
/// at-least-once -- a crash between applying a batch and committing the cursor
/// replays that batch.
/// </summary>
public sealed class PostgresSink
{
    private readonly NpgsqlDataSource _db;

    // hydrus service_type int -> our enum. Ints from HydrusConstants.py:439-462.
    private static readonly Dictionary<int, ServiceType> ServiceTypes = new()
    {
        [0]  = ServiceType.TagRepository,
        [1]  = ServiceType.FileRepository,
        [2]  = ServiceType.LocalFileDomain,
        [3]  = ServiceType.MessageDepot,
        [5]  = ServiceType.LocalTag,
        [6]  = ServiceType.LocalRatingNumerical,
        [7]  = ServiceType.LocalRatingLike,
        [8]  = ServiceType.RatingNumericalRepository,
        [9]  = ServiceType.RatingLikeRepository,
        [10] = ServiceType.CombinedTag,
        [11] = ServiceType.CombinedFile,
        [12] = ServiceType.LocalBooru,
        [13] = ServiceType.Ipfs,
        [14] = ServiceType.LocalFileTrashDomain,
        [15] = ServiceType.HydrusLocalFileStorage,
        [16] = ServiceType.TestService,
        [17] = ServiceType.LocalNotes,
        [18] = ServiceType.ClientApiService,
        [19] = ServiceType.CombinedDeletedFile,
        [20] = ServiceType.LocalFileUpdateDomain,
        [21] = ServiceType.CombinedLocalFileDomains,
        [22] = ServiceType.LocalRatingIncDec,
        [99] = ServiceType.ServerAdmin,
        [100] = ServiceType.NullService
    };

    // hydrus CONTENT_STATUS_* -> our enum (HydrusConstants.py:179-182)
    private static readonly ContentStatus[] Statuses =
        [ContentStatus.Current, ContentStatus.Pending, ContentStatus.Deleted, ContentStatus.Petitioned];

    public PostgresSink(NpgsqlDataSource db) => _db = db;

    public static ContentStatus MapStatus(int s) => Statuses[s];

    public static ServiceType MapServiceType(int t) =>
        ServiceTypes.TryGetValue(t, out var v) ? v : ServiceType.NullService;

    // -- cursor -------------------------------------------------------------

    public async Task<(long LastSeq, byte[]? SourceDbId)> ReadCursorAsync(CancellationToken ct)
    {
        await using var cmd = _db.CreateCommand(
            "SELECT last_seq, source_db_id FROM hydrus.sync_state WHERE stream = 'outbox';");
        await using var r = await cmd.ExecuteReaderAsync(ct);

        if (!await r.ReadAsync(ct)) return (0, null);

        return (r.GetInt64(0), await r.IsDBNullAsync(1, ct) ? null : (byte[])r.GetValue(1));
    }

    // -- id resolution ------------------------------------------------------

    /// <summary>
    /// sha256 -> file_id, inserting stubs for unknown files. Files can be
    /// referenced by a tag or url change before their file_info row arrives,
    /// so this must create-if-absent rather than assume.
    /// </summary>
    public async Task<Dictionary<string, int>> EnsureFilesAsync(
        NpgsqlConnection c, NpgsqlTransaction tx, ICollection<byte[]> hashes, CancellationToken ct)
    {
        var map = new Dictionary<string, int>(hashes.Count);
        if (hashes.Count == 0) return map;

        var distinct = hashes.GroupBy(Convert.ToHexString).Select(g => g.First()).ToArray();

        await using (var ins = new NpgsqlCommand(
            "INSERT INTO hydrus.file (sha256) SELECT unnest(@h) ON CONFLICT (sha256) DO NOTHING;", c, tx))
        {
            ins.Parameters.Add(new NpgsqlParameter("h", NpgsqlDbType.Array | NpgsqlDbType.Bytea) { Value = distinct });
            await ins.ExecuteNonQueryAsync(ct);
        }

        await using var sel = new NpgsqlCommand(
            "SELECT sha256, file_id FROM hydrus.file WHERE sha256 = ANY(@h);", c, tx);
        sel.Parameters.Add(new NpgsqlParameter("h", NpgsqlDbType.Array | NpgsqlDbType.Bytea) { Value = distinct });

        await using var r = await sel.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) map[Convert.ToHexString((byte[])r.GetValue(0))] = r.GetInt32(1);

        return map;
    }

    public async Task<Dictionary<(string, string), int>> EnsureTagsAsync(
        NpgsqlConnection c, NpgsqlTransaction tx, ICollection<(string Ns, string Sub)> tags, CancellationToken ct)
    {
        var map = new Dictionary<(string, string), int>();
        if (tags.Count == 0) return map;

        var distinct = tags.Distinct().ToArray();
        var ns = distinct.Select(t => t.Ns).ToArray();
        var sub = distinct.Select(t => t.Sub).ToArray();

        await using (var ins = new NpgsqlCommand(
            @"INSERT INTO hydrus.tag (namespace, subtag)
              SELECT * FROM unnest(@n, @s) ON CONFLICT (namespace, subtag) DO NOTHING;", c, tx))
        {
            ins.Parameters.AddWithValue("n", ns);
            ins.Parameters.AddWithValue("s", sub);
            await ins.ExecuteNonQueryAsync(ct);
        }

        await using var sel = new NpgsqlCommand(
            @"SELECT t.namespace, t.subtag, t.tag_id FROM hydrus.tag t
              JOIN unnest(@n, @s) AS w(n, s) ON w.n = t.namespace AND w.s = t.subtag;", c, tx);
        sel.Parameters.AddWithValue("n", ns);
        sel.Parameters.AddWithValue("s", sub);

        await using var r = await sel.ExecuteReaderAsync(ct);
        while (await r.ReadAsync(ct)) map[(r.GetString(0), r.GetString(1))] = r.GetInt32(2);

        return map;
    }

    /// <summary>hydrus service_id -> our service_id, via the stable service_key.</summary>
    public async Task<Dictionary<long, short>> SyncServicesAsync(
        NpgsqlConnection c, NpgsqlTransaction tx, List<HydrusSource.ServiceRow> services, CancellationToken ct)
    {
        var map = new Dictionary<long, short>();
        if (services.Count == 0) return map;

        foreach (var s in services)
        {
            await using var cmd = new NpgsqlCommand(
                @"INSERT INTO hydrus.service (service_key, name, service_type)
                  VALUES (@k, @n, @t)
                  ON CONFLICT (service_key) DO UPDATE SET name = EXCLUDED.name
                  RETURNING service_id;", c, tx);

            cmd.Parameters.AddWithValue("k", s.ServiceKey);
            cmd.Parameters.AddWithValue("n", s.Name);
            cmd.Parameters.AddWithValue("t", MapServiceType(s.ServiceType));

            map[s.ServiceId] = Convert.ToInt16(await cmd.ExecuteScalarAsync(ct));
        }

        return map;
    }

    // -- operations ---------------------------------------------------------

    public async Task ApplyFileInfoAsync(NpgsqlConnection c, NpgsqlTransaction tx,
        List<(int FileId, long? Size, int? Mime, int? W, int? H, long? Dur, long? Frames, bool? Audio, long? Words)> rows,
        CancellationToken ct)
    {
        if (rows.Count == 0) return;

        await using var cmd = new NpgsqlCommand(
            @"UPDATE hydrus.file f SET
                size = v.size, width = v.width, height = v.height,
                duration_ms = v.duration_ms, num_frames = v.num_frames,
                has_audio = v.has_audio, num_words = v.num_words,
                mime_code = m.code, synced_at = now()
              FROM unnest(@id, @size, @mime, @w, @h, @dur, @nf, @ha, @nw)
                   AS v(file_id, size, hydrus_mime, width, height, duration_ms, num_frames, has_audio, num_words)
              LEFT JOIN hydrus.mime m ON m.hydrus_enum = v.hydrus_mime
              WHERE f.file_id = v.file_id;", c, tx);

        cmd.Parameters.AddWithValue("id", rows.Select(r => r.FileId).ToArray());
        cmd.Parameters.AddWithValue("size", rows.Select(r => r.Size).ToArray());
        cmd.Parameters.AddWithValue("mime", rows.Select(r => (short?)r.Mime).ToArray());
        cmd.Parameters.AddWithValue("w", rows.Select(r => r.W).ToArray());
        cmd.Parameters.AddWithValue("h", rows.Select(r => r.H).ToArray());
        cmd.Parameters.AddWithValue("dur", rows.Select(r => r.Dur).ToArray());
        cmd.Parameters.AddWithValue("nf", rows.Select(r => r.Frames).ToArray());
        cmd.Parameters.AddWithValue("ha", rows.Select(r => r.Audio).ToArray());
        cmd.Parameters.AddWithValue("nw", rows.Select(r => r.Words).ToArray());

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task ApplyFileStatusAsync(NpgsqlConnection c, NpgsqlTransaction tx,
        short serviceId, ContentStatus status, int[] fileIds, DateTimeOffset? ts, CancellationToken ct)
    {
        if (fileIds.Length == 0) return;

        var tsCol = status == ContentStatus.Deleted ? "deleted_at" : "imported_at";

        await using var cmd = new NpgsqlCommand(
            $@"INSERT INTO hydrus.file_service_status (service_id, status, file_id, {tsCol})
               SELECT @s, @st, unnest(@f), @t
               ON CONFLICT (service_id, status, file_id) DO UPDATE SET {tsCol} = EXCLUDED.{tsCol};", c, tx);

        cmd.Parameters.AddWithValue("s", serviceId);
        cmd.Parameters.AddWithValue("st", status);
        cmd.Parameters.AddWithValue("f", fileIds);
        cmd.Parameters.AddWithValue("t", (object?)ts ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);

        // Becoming current in a service clears any prior deletion record there,
        // mirroring hydrus's own undelete.
        if (status == ContentStatus.Current)
        {
            await using var del = new NpgsqlCommand(
                @"DELETE FROM hydrus.file_service_status
                  WHERE service_id = @s AND status = 'deleted' AND file_id = ANY(@f);", c, tx);
            del.Parameters.AddWithValue("s", serviceId);
            del.Parameters.AddWithValue("f", fileIds);
            await del.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task ApplyMappingsAsync(NpgsqlConnection c, NpgsqlTransaction tx,
        short serviceId, ContentStatus status, bool removing,
        List<(int TagId, int FileId)> pairs, CancellationToken ct)
    {
        if (pairs.Count == 0) return;

        var tagIds = pairs.Select(p => p.TagId).ToArray();
        var fileIds = pairs.Select(p => p.FileId).ToArray();

        var sql = removing
            ? @"DELETE FROM hydrus.file_tag ft
                USING unnest(@t, @f) AS v(tag_id, file_id)
                WHERE ft.service_id = @s AND ft.status = @st
                  AND ft.tag_id = v.tag_id AND ft.file_id = v.file_id;"
            : @"INSERT INTO hydrus.file_tag (service_id, status, tag_id, file_id)
                SELECT @s, @st, * FROM unnest(@t, @f)
                ON CONFLICT DO NOTHING;";

        await using var cmd = new NpgsqlCommand(sql, c, tx);
        cmd.Parameters.AddWithValue("s", serviceId);
        cmd.Parameters.AddWithValue("st", status);
        cmd.Parameters.AddWithValue("t", tagIds);
        cmd.Parameters.AddWithValue("f", fileIds);
        await cmd.ExecuteNonQueryAsync(ct);

        // Hydrus's own transitions: adding to current clears deleted+pending;
        // adding to deleted clears current. Mirror them so the read model does
        // not accumulate contradictory rows.
        if (!removing && status is ContentStatus.Current or ContentStatus.Deleted)
        {
            var clear = status == ContentStatus.Current
                ? new[] { ContentStatus.Deleted, ContentStatus.Pending }
                : new[] { ContentStatus.Current };

            foreach (var s in clear)
            {
                await using var del = new NpgsqlCommand(
                    @"DELETE FROM hydrus.file_tag ft
                      USING unnest(@t, @f) AS v(tag_id, file_id)
                      WHERE ft.service_id = @s AND ft.status = @st
                        AND ft.tag_id = v.tag_id AND ft.file_id = v.file_id;", c, tx);
                del.Parameters.AddWithValue("s", serviceId);
                del.Parameters.AddWithValue("st", s);
                del.Parameters.AddWithValue("t", tagIds);
                del.Parameters.AddWithValue("f", fileIds);
                await del.ExecuteNonQueryAsync(ct);
            }
        }
    }

    public async Task ApplyUrlAsync(NpgsqlConnection c, NpgsqlTransaction tx,
        int fileId, string url, bool added, CancellationToken ct)
    {
        if (added)
        {
            await using var cmd = new NpgsqlCommand(
                @"WITH u AS (
                      INSERT INTO hydrus.url (url, domain) VALUES (@u, @d)
                      ON CONFLICT (url) DO UPDATE SET domain = EXCLUDED.domain
                      RETURNING url_id)
                  INSERT INTO hydrus.file_url (file_id, url_id)
                  SELECT @f, url_id FROM u ON CONFLICT DO NOTHING;", c, tx);
            cmd.Parameters.AddWithValue("u", url);
            cmd.Parameters.AddWithValue("d", DomainOf(url));
            cmd.Parameters.AddWithValue("f", fileId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        else
        {
            await using var cmd = new NpgsqlCommand(
                @"DELETE FROM hydrus.file_url
                  WHERE file_id = @f AND url_id = (SELECT url_id FROM hydrus.url WHERE url = @u);", c, tx);
            cmd.Parameters.AddWithValue("u", url);
            cmd.Parameters.AddWithValue("f", fileId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task ApplyNoteAsync(NpgsqlConnection c, NpgsqlTransaction tx,
        int fileId, string name, string? body, CancellationToken ct)
    {
        var sql = body is null
            ? "DELETE FROM hydrus.file_note WHERE file_id = @f AND name = @n;"
            : @"INSERT INTO hydrus.file_note (file_id, name, body) VALUES (@f, @n, @b)
                ON CONFLICT (file_id, name) DO UPDATE SET body = EXCLUDED.body;";

        await using var cmd = new NpgsqlCommand(sql, c, tx);
        cmd.Parameters.AddWithValue("f", fileId);
        cmd.Parameters.AddWithValue("n", name);
        if (body is not null) cmd.Parameters.AddWithValue("b", body);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task ApplyPhashesAsync(NpgsqlConnection c, NpgsqlTransaction tx,
        int fileId, byte[][] phashes, bool added, CancellationToken ct)
    {
        if (phashes.Length == 0) return;

        if (added)
        {
            await using var cmd = new NpgsqlCommand(
                @"WITH p AS (
                      INSERT INTO hydrus.phash (phash) SELECT unnest(@p)
                      ON CONFLICT (phash) DO NOTHING RETURNING phash_id, phash),
                  all_p AS (
                      SELECT phash_id FROM p
                      UNION SELECT phash_id FROM hydrus.phash WHERE phash = ANY(@p))
                  INSERT INTO hydrus.file_phash (file_id, phash_id)
                  SELECT @f, phash_id FROM all_p ON CONFLICT DO NOTHING;", c, tx);
            cmd.Parameters.Add(new NpgsqlParameter("p", NpgsqlDbType.Array | NpgsqlDbType.Bytea) { Value = phashes });
            cmd.Parameters.AddWithValue("f", fileId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
        else
        {
            await using var cmd = new NpgsqlCommand(
                @"DELETE FROM hydrus.file_phash
                  WHERE file_id = @f
                    AND phash_id IN (SELECT phash_id FROM hydrus.phash WHERE phash = ANY(@p));", c, tx);
            cmd.Parameters.Add(new NpgsqlParameter("p", NpgsqlDbType.Array | NpgsqlDbType.Bytea) { Value = phashes });
            cmd.Parameters.AddWithValue("f", fileId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task CommitCursorAsync(NpgsqlConnection c, NpgsqlTransaction tx,
        long seq, byte[]? sourceDbId, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(
            @"INSERT INTO hydrus.sync_state (stream, last_seq, source_db_id, updated_at)
              VALUES ('outbox', @s, @d, now())
              ON CONFLICT (stream) DO UPDATE
                SET last_seq = EXCLUDED.last_seq, updated_at = now();", c, tx);
        cmd.Parameters.AddWithValue("s", seq);
        cmd.Parameters.AddWithValue("d", (object?)sourceDbId ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static string DomainOf(string url)
    {
        try { return new Uri(url).Host; } catch { return ""; }
    }
}
