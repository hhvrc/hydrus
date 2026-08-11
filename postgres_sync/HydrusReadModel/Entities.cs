namespace HydrusReadModel;

// Read model projected from hydrus's SQLite database by the drain worker.
// Nothing in this assembly writes back to hydrus.
//
// Identity surrogates (FileId/TagId/UrlId/...) are OURS, not hydrus's hash_id.
// Sha256 is the external identity and the reconciliation key.

/// <summary>
/// A mime type, with its extension and general filetype. Open set -- hydrus adds
/// mimes most releases -- so this is a table, not an enum. Seeded by the drain
/// from HydrusConstants mime_string_lookup (:1234) / mime_ext_lookup (:1425).
/// </summary>
public class Mime
{
    /// <summary>
    /// PK. Hydrus's short name ('animated gif'), NOT the media type -- media
    /// types are not unique across hydrus's filetypes. Six of them collide:
    /// static vs animated gif/webp/jxl, zip vs cbz, octet-stream, audio/mp4.
    /// Short names are unique 91/91, so they are the only safe text key.
    /// </summary>
    public required string Code { get; set; }

    public required string MediaType { get; set; }     // 'image/gif', NOT unique
    public required string Extension { get; set; }     // '.gif'
    public Filetype Filetype { get; set; }
    public short HydrusEnum { get; set; }              // HC.ANIMATION_GIF = 4
    public bool Searchable { get; set; } = true;       // HC.SEARCHABLE_MIMES

    public ICollection<File> Files { get; set; } = [];
}

/// <summary>
/// A hydrus service. ServiceKey is hydrus's 32-byte identifier and is stable
/// across reinstalls, so it is the natural key; ServiceId is ours.
/// </summary>
public class Service
{
    public short ServiceId { get; set; }
    public required byte[] ServiceKey { get; set; }    // 32 bytes
    public required string Name { get; set; }
    public ServiceType ServiceType { get; set; }
}

/// <summary>
/// One row per known file. Presence here does NOT mean the file is in your
/// collection -- see <see cref="FileServiceStatus"/>. A file hydrus has only
/// ever seen and deleted still has a row.
/// </summary>
public class File
{
    public int FileId { get; set; }
    public required byte[] Sha256 { get; set; }        // 32 bytes, external identity

    public long? Size { get; set; }
    public long? DurationMs { get; set; }              // null for still images
    public long? NumFrames { get; set; }
    public long? NumWords { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public bool? HasAudio { get; set; }
    public string? MimeCode { get; set; }

    /// <summary>hydrus main.file_modified_timestamps</summary>
    public DateTimeOffset? ModifiedAt { get; set; }

    /// <summary>
    /// hydrus main.archive_timestamps. NULL means the file is still in the
    /// hydrus inbox (main.file_inbox).
    /// </summary>
    public DateTimeOffset? ArchivedAt { get; set; }

    public DateTimeOffset SyncedAt { get; set; }

    public Mime? Mime { get; set; }
    public FileOtherHash? OtherHashes { get; set; }
    public ICollection<FileServiceStatus> ServiceStatuses { get; set; } = [];
    public ICollection<FileTag> Tags { get; set; } = [];
    public ICollection<FileUrl> Urls { get; set; } = [];
    public ICollection<FileNote> Notes { get; set; } = [];
    public ICollection<FilePhash> Phashes { get; set; } = [];
}

/// <summary>
/// Secondary hashes, only populated for files hydrus has imported locally.
/// Separate table because it is sparse and rarely joined.
/// </summary>
public class FileOtherHash
{
    public int FileId { get; set; }
    public byte[]? Md5 { get; set; }                   // 16 bytes
    public byte[]? Sha1 { get; set; }                  // 20 bytes
    public byte[]? Sha512 { get; set; }                // 64 bytes

    public File File { get; set; } = null!;
}

/// <summary>
/// Mirrors hydrus's per-service current/deleted/pending/petitioned file tables
/// (ClientDBFilesStorage.py:297-300). This is what tells you a file actually
/// exists in 'my files' right now.
/// </summary>
public class FileServiceStatus
{
    public int FileId { get; set; }
    public short ServiceId { get; set; }
    public ContentStatus Status { get; set; }

    public DateTimeOffset? ImportedAt { get; set; }          // current_files.timestamp_ms
    public DateTimeOffset? DeletedAt { get; set; }           // deleted_files.timestamp_ms
    public DateTimeOffset? OriginalImportedAt { get; set; }  // deleted_files.original_timestamp_ms

    public File File { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

/// <summary>
/// Namespace and subtag stored as text with a generated display column. Hydrus
/// normalises these into separate id tables; we do not, because the tag table is
/// small relative to the mappings table and inline text makes queries simpler.
/// </summary>
public class Tag
{
    public int TagId { get; set; }
    public string Namespace { get; set; } = "";        // 'character', '' for unnamespaced
    public required string Subtag { get; set; }

    /// <summary>Stored generated column: 'namespace:subtag', or just subtag. Read-only.</summary>
    public string TagText { get; private set; } = null!;
}

/// <summary>
/// THE big table. Hydrus STORAGE mappings -- siblings and parents are NOT
/// applied. Apply <see cref="TagSibling"/> / <see cref="TagParent"/> at read
/// time; replicating display mappings would mean one sibling edit cascading
/// into millions of row updates.
///
/// Status is part of the key: a mapping can be simultaneously 'deleted' and
/// 'pending' (deleted, then re-pended for upload).
/// </summary>
public class FileTag
{
    public short ServiceId { get; set; }
    public ContentStatus Status { get; set; }
    public int TagId { get; set; }
    public int FileId { get; set; }

    public File File { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

/// <summary>bad_tag -> good_tag replacement rule.</summary>
public class TagSibling
{
    public short ServiceId { get; set; }
    public ContentStatus Status { get; set; }
    public int BadTagId { get; set; }
    public int GoodTagId { get; set; }

    public Tag BadTag { get; set; } = null!;
    public Tag GoodTag { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

/// <summary>child_tag implies parent_tag.</summary>
public class TagParent
{
    public short ServiceId { get; set; }
    public ContentStatus Status { get; set; }
    public int ChildTagId { get; set; }
    public int ParentTagId { get; set; }

    public Tag ChildTag { get; set; } = null!;
    public Tag ParentTag { get; set; } = null!;
    public Service Service { get; set; } = null!;
}

public class Url
{
    public int UrlId { get; set; }
    public required string Address { get; set; }
    public required string Domain { get; set; }
}

public class FileUrl
{
    public int FileId { get; set; }
    public int UrlId { get; set; }

    public File File { get; set; } = null!;
    public Url Url { get; set; } = null!;
}

/// <summary>Hydrus keys notes by (hash_id, name); one body per name per file.</summary>
public class FileNote
{
    public int FileId { get; set; }
    public required string Name { get; set; }
    public required string Body { get; set; }

    public File File { get; set; } = null!;
}

/// <summary>
/// Flattened from hydrus shape_perceptual_hashes. 8 bytes at the default
/// phash_dimension of 8, but variable by design -- ClientImagePerceptualHashes.py:221
/// packs dimension^2 bits.
/// </summary>
public class Phash
{
    public int PhashId { get; set; }
    public required byte[] Value { get; set; }
}

/// <summary>Many-to-many: hydrus generates one phash per sampled frame.</summary>
public class FilePhash
{
    public int FileId { get; set; }
    public int PhashId { get; set; }

    public File File { get; set; } = null!;
    public Phash Phash { get; set; } = null!;
}

// -- sync bookkeeping -------------------------------------------------------

/// <summary>
/// The drain is at-least-once: LastSeq advances only after the batch commits,
/// so a crash replays the tail. Every upsert must therefore be idempotent.
/// </summary>
public class SyncState
{
    public required string Stream { get; set; }        // 'outbox'
    public long LastSeq { get; set; }                  // last hydrus outbox rowid applied
    public byte[]? SourceDbId { get; set; }            // guards against a different hydrus
    public DateTimeOffset UpdatedAt { get; set; }
}

public class SyncRun
{
    public long RunId { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public long RowsApplied { get; set; }
    public SyncKind Kind { get; set; }
    public bool? Ok { get; set; }
    public string? Detail { get; set; }                // jsonb
}

/// <summary>
/// Drift found by the periodic reconciliation pass. Should stay empty; if it
/// does not, the CDC hooks are missing a write path.
/// </summary>
public class SyncDiscrepancy
{
    public long DiscrepancyId { get; set; }
    public DateTimeOffset FoundAt { get; set; }
    public byte[]? Sha256 { get; set; }
    public DiscrepancyKind Kind { get; set; }
    public string? Detail { get; set; }                // jsonb
}
