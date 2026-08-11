using NpgsqlTypes;

namespace HydrusReadModel;

// Closed sets, mapped to PG enums. [PgName] pins the label so renaming the C#
// member never silently changes the database contract.
//
// Declaration order matters: PG enum sort order follows label creation order,
// so `ORDER BY filetype` in SQL follows the order written here.

/// <summary>HC.CONTENT_STATUS_* (HydrusConstants.py:179-182)</summary>
public enum ContentStatus
{
    [PgName("current")]    Current,
    [PgName("pending")]    Pending,
    [PgName("deleted")]    Deleted,
    [PgName("petitioned")] Petitioned
}

/// <summary>HC.GENERAL_* general filetypes (HydrusConstants.py:851-859)</summary>
public enum Filetype
{
    [PgName("image")]         Image,
    [PgName("animation")]     Animation,
    [PgName("video")]         Video,
    [PgName("audio")]         Audio,
    [PgName("archive")]       Archive,
    [PgName("image_project")] ImageProject,
    [PgName("application")]   Application
}

/// <summary>HC service constants (HydrusConstants.py:439-462)</summary>
public enum ServiceType
{
    [PgName("tag_repository")]              TagRepository,
    [PgName("file_repository")]             FileRepository,
    [PgName("local_file_domain")]           LocalFileDomain,
    [PgName("local_tag")]                   LocalTag,
    [PgName("local_notes")]                 LocalNotes,
    [PgName("local_rating_like")]           LocalRatingLike,
    [PgName("local_rating_numerical")]      LocalRatingNumerical,
    [PgName("local_rating_incdec")]         LocalRatingIncDec,
    [PgName("local_file_trash_domain")]     LocalFileTrashDomain,
    [PgName("local_file_update_domain")]    LocalFileUpdateDomain,
    [PgName("hydrus_local_file_storage")]   HydrusLocalFileStorage,
    [PgName("combined_tag")]                CombinedTag,
    [PgName("combined_file")]               CombinedFile,
    [PgName("combined_deleted_file")]       CombinedDeletedFile,
    [PgName("combined_local_file_domains")] CombinedLocalFileDomains,
    [PgName("client_api_service")]          ClientApiService,
    [PgName("ipfs")]                        Ipfs,
    [PgName("server_admin")]                ServerAdmin,
    [PgName("test_service")]                TestService,
    [PgName("null_service")]                NullService,

    // Defunct upstream, retained so historical rows still map.
    [PgName("message_depot")]               MessageDepot,
    [PgName("local_booru")]                 LocalBooru,
    [PgName("rating_like_repository")]      RatingLikeRepository,
    [PgName("rating_numerical_repository")] RatingNumericalRepository
}

public enum SyncKind
{
    [PgName("backfill")]  Backfill,
    [PgName("drain")]     Drain,
    [PgName("reconcile")] Reconcile
}

public enum DiscrepancyKind
{
    [PgName("missing")]  Missing,
    [PgName("extra")]    Extra,
    [PgName("mismatch")] Mismatch
}
