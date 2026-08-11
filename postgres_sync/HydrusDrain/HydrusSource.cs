using Microsoft.Data.Sqlite;

namespace HydrusDrain;

/// <summary>
/// Read-only view of a hydrus SQLite database.
///
/// Opened ReadOnly and never written to -- hydrus owns these files and is the
/// only process allowed to write them. Outbox pruning happens on the hydrus
/// side, driven by the watermark file this drain writes.
///
/// Note the outbox stores hydrus's internal ids; resolution to hashes and tag
/// text happens here, by joining against hydrus's own master tables.
/// </summary>
public sealed class HydrusSource : IDisposable
{
    private readonly SqliteConnection _conn;

    public HydrusSource(string dbDir)
    {
        var main = Path.Combine(dbDir, "client.db");
        if (!System.IO.File.Exists(main))
            throw new FileNotFoundException($"no client.db in {dbDir}", main);

        _conn = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = main,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString());

        _conn.Open();

        // Hydrus splits across four files; the outbox lives in main but the
        // definitions we resolve against live in master.
        Attach(Path.Combine(dbDir, "client.master.db"), "external_master");
        Attach(Path.Combine(dbDir, "client.caches.db"), "external_caches");
        Attach(Path.Combine(dbDir, "client.mappings.db"), "external_mappings");
    }

    private void Attach(string path, string name)
    {
        if (!System.IO.File.Exists(path)) return;

        using var cmd = _conn.CreateCommand();
        cmd.CommandText = $"ATTACH DATABASE $p AS {name};";
        cmd.Parameters.AddWithValue("$p", path);
        cmd.ExecuteNonQuery();
    }

    private SqliteCommand Cmd(string sql)
    {
        var c = _conn.CreateCommand();
        c.CommandText = sql;
        return c;
    }

    /// <summary>Outbox rows strictly after <paramref name="afterSeq"/>, oldest first.</summary>
    public List<(long Seq, string Op, string Payload)> ReadOutbox(long afterSeq, int limit)
    {
        var rows = new List<(long, string, string)>();

        using var cmd = Cmd("SELECT seq, op, payload FROM pg_outbox WHERE seq > $s ORDER BY seq LIMIT $l;");
        cmd.Parameters.AddWithValue("$s", afterSeq);
        cmd.Parameters.AddWithValue("$l", limit);

        using var r = cmd.ExecuteReader();
        while (r.Read()) rows.Add((r.GetInt64(0), r.GetString(1), r.GetString(2)));

        return rows;
    }

    /// <summary>The largest outbox seq, so we can report lag.</summary>
    public long MaxOutboxSeq()
    {
        using var cmd = Cmd("SELECT COALESCE(MAX(seq), 0) FROM pg_outbox;");
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    /// <summary>
    /// A stable identifier for this hydrus install, so the drain refuses to
    /// point a populated read model at a different database. Uses the oldest
    /// service key, which is created once at db genesis and never changes.
    /// </summary>
    public byte[]? SourceDbId()
    {
        using var cmd = Cmd("SELECT service_key FROM services ORDER BY service_id ASC LIMIT 1;");
        return cmd.ExecuteScalar() as byte[];
    }

    public Dictionary<long, byte[]> ResolveHashes(IEnumerable<long> hashIds)
        => ResolveBlob(hashIds, "SELECT hash_id, hash FROM external_master.hashes WHERE hash_id IN ");

    public Dictionary<long, byte[]> ResolvePhashes(IEnumerable<long> phashIds)
        => ResolveBlob(phashIds, "SELECT phash_id, phash FROM external_master.shape_perceptual_hashes WHERE phash_id IN ");

    private Dictionary<long, byte[]> ResolveBlob(IEnumerable<long> ids, string prefix)
    {
        var result = new Dictionary<long, byte[]>();

        foreach (var chunk in Chunk(ids.Distinct(), 900))
        {
            using var cmd = Cmd(prefix + "(" + string.Join(",", chunk) + ");");
            using var r = cmd.ExecuteReader();
            while (r.Read()) result[r.GetInt64(0)] = (byte[])r.GetValue(1);
        }

        return result;
    }

    /// <summary>tag_id -> (namespace, subtag), joined through hydrus's split tag tables.</summary>
    public Dictionary<long, (string Namespace, string Subtag)> ResolveTags(IEnumerable<long> tagIds)
    {
        var result = new Dictionary<long, (string, string)>();

        const string sql = @"
            SELECT t.tag_id, n.namespace, s.subtag
            FROM external_master.tags t
            JOIN external_master.namespaces n ON n.namespace_id = t.namespace_id
            JOIN external_master.subtags   s ON s.subtag_id    = t.subtag_id
            WHERE t.tag_id IN ";

        foreach (var chunk in Chunk(tagIds.Distinct(), 900))
        {
            using var cmd = Cmd(sql + "(" + string.Join(",", chunk) + ");");
            using var r = cmd.ExecuteReader();
            while (r.Read()) result[r.GetInt64(0)] = (r.GetString(1), r.GetString(2));
        }

        return result;
    }

    public record ServiceRow(long ServiceId, byte[] ServiceKey, int ServiceType, string Name);

    public List<ServiceRow> ReadServices()
    {
        var rows = new List<ServiceRow>();

        using var cmd = Cmd("SELECT service_id, service_key, service_type, name FROM services;");
        using var r = cmd.ExecuteReader();
        while (r.Read())
            rows.Add(new ServiceRow(r.GetInt64(0), (byte[])r.GetValue(1), r.GetInt32(2), r.GetString(3)));

        return rows;
    }

    private static IEnumerable<List<T>> Chunk<T>(IEnumerable<T> src, int size)
    {
        var buf = new List<T>(size);
        foreach (var x in src)
        {
            buf.Add(x);
            if (buf.Count < size) continue;
            yield return buf;
            buf = new List<T>(size);
        }
        if (buf.Count > 0) yield return buf;
    }

    public void Dispose() => _conn.Dispose();
}
