using Microsoft.EntityFrameworkCore;

namespace HydrusReadModel;

public class HydrusDbContext : DbContext
{
    public const string Schema = "hydrus";

    public HydrusDbContext(DbContextOptions<HydrusDbContext> options) : base(options) { }

    /// <summary>
    /// Single place both the app and the design-time factory declare the enums.
    /// This is what makes migrations emit CREATE TYPE *and* map the columns to
    /// it; without it, enum columns scaffold as `integer`.
    /// </summary>
    public static void ConfigureEnums(Npgsql.EntityFrameworkCore.PostgreSQL
        .Infrastructure.NpgsqlDbContextOptionsBuilder o)
    {
        o.MapEnum<ContentStatus>("content_status", Schema);
        o.MapEnum<Filetype>("filetype", Schema);
        o.MapEnum<ServiceType>("service_type", Schema);
        o.MapEnum<SyncKind>("sync_kind", Schema);
        o.MapEnum<DiscrepancyKind>("discrepancy_kind", Schema);
    }

    /// <summary>
    /// EF auto-creates an index for every foreign key. On file_tag -- which
    /// reaches 10^8 rows at PTR scale -- that silently doubles the index
    /// footprint with redundant single-column indexes. Turn it off and declare
    /// every index deliberately.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Conventions.Remove(
            typeof(Microsoft.EntityFrameworkCore.Metadata.Conventions.ForeignKeyIndexConvention));
    }

    public DbSet<Mime> Mimes => Set<Mime>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<File> Files => Set<File>();
    public DbSet<FileOtherHash> FileOtherHashes => Set<FileOtherHash>();
    public DbSet<FileServiceStatus> FileServiceStatuses => Set<FileServiceStatus>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<FileTag> FileTags => Set<FileTag>();
    public DbSet<TagSibling> TagSiblings => Set<TagSibling>();
    public DbSet<TagParent> TagParents => Set<TagParent>();
    public DbSet<Url> Urls => Set<Url>();
    public DbSet<FileUrl> FileUrls => Set<FileUrl>();
    public DbSet<FileNote> FileNotes => Set<FileNote>();
    public DbSet<Phash> Phashes => Set<Phash>();
    public DbSet<FilePhash> FilePhashes => Set<FilePhash>();
    public DbSet<SyncState> SyncStates => Set<SyncState>();
    public DbSet<SyncRun> SyncRuns => Set<SyncRun>();
    public DbSet<SyncDiscrepancy> SyncDiscrepancies => Set<SyncDiscrepancy>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(Schema);

        // NOTE: the enums are NOT declared here. HasPostgresEnum() emits
        // CREATE TYPE in migrations but does not teach EF's type mapper, so
        // every enum column silently scaffolds as `integer`. Declare them with
        // MapEnum() on the options builder instead -- see ConfigureEnums below,
        // which both creates the type and maps the columns.

        b.HasPostgresExtension("pg_trgm");

        ConfigureMime(b);
        ConfigureService(b);
        ConfigureFile(b);
        ConfigureTags(b);
        ConfigureUrlsNotesPhashes(b);
        ConfigureSync(b);
    }

    private static void ConfigureMime(ModelBuilder b) => b.Entity<Mime>(e =>
    {
        e.ToTable("mime");
        e.HasKey(x => x.Code);
        e.HasIndex(x => x.HydrusEnum).IsUnique();   // the drain joins on this
        e.HasIndex(x => x.Filetype);
        e.HasIndex(x => x.Extension);
        e.HasIndex(x => x.MediaType);               // deliberately NOT unique
    });

    private static void ConfigureService(ModelBuilder b) => b.Entity<Service>(e =>
    {
        e.ToTable("service");
        e.HasKey(x => x.ServiceId);
        e.Property(x => x.ServiceId).UseIdentityAlwaysColumn();
        e.HasIndex(x => x.ServiceKey).IsUnique();
        e.HasIndex(x => x.ServiceType);
        e.ToTable(t => t.HasCheckConstraint(
            "ck_service_key_length", "octet_length(service_key) = 32"));
    });

    private static void ConfigureFile(ModelBuilder b)
    {
        b.Entity<File>(e =>
        {
            e.ToTable("file");
            e.HasKey(x => x.FileId);
            e.Property(x => x.FileId).UseIdentityAlwaysColumn();
            e.Property(x => x.SyncedAt).HasDefaultValueSql("now()");
            e.HasIndex(x => x.Sha256).IsUnique();
            e.HasIndex(x => x.MimeCode);
            e.ToTable(t => t.HasCheckConstraint(
                "ck_file_sha256_length", "octet_length(sha256) = 32"));

            e.HasOne(x => x.Mime).WithMany(m => m.Files)
                .HasForeignKey(x => x.MimeCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<FileOtherHash>(e =>
        {
            e.ToTable("file_other_hash");
            e.HasKey(x => x.FileId);
            e.HasOne(x => x.File).WithOne(f => f.OtherHashes)
                .HasForeignKey<FileOtherHash>(x => x.FileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Partial indexes -- EF cannot express these without HasFilter.
            e.HasIndex(x => x.Md5).HasFilter("md5 IS NOT NULL");
            e.HasIndex(x => x.Sha1).HasFilter("sha1 IS NOT NULL");
        });

        b.Entity<FileServiceStatus>(e =>
        {
            e.ToTable("file_service_status");
            e.HasKey(x => new { x.ServiceId, x.Status, x.FileId });

            e.HasOne(x => x.File).WithMany(f => f.ServiceStatuses)
                .HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Service).WithMany()
                .HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);

            // "what is in this service right now" -- the hot path.
            e.HasIndex(x => new { x.ServiceId, x.ImportedAt, x.FileId })
                .HasDatabaseName("ix_file_service_status_current")
                .HasFilter("status = 'current'")
                .IsDescending(false, true, false);

            e.HasIndex(x => new { x.FileId, x.ServiceId })
                .IncludeProperties(x => x.Status);
        });
    }

    private static void ConfigureTags(ModelBuilder b)
    {
        b.Entity<Tag>(e =>
        {
            e.ToTable("tag");
            e.HasKey(x => x.TagId);
            e.Property(x => x.TagId).UseIdentityAlwaysColumn();
            e.Property(x => x.Namespace).HasDefaultValue("");

            e.Property(x => x.TagText)
                .HasColumnName("tag")
                .HasComputedColumnSql(
                    "CASE WHEN namespace = '' THEN subtag ELSE namespace || ':' || subtag END",
                    stored: true);

            e.HasIndex(x => new { x.Namespace, x.Subtag }).IsUnique();
            e.HasIndex(x => x.Namespace).HasFilter("namespace <> ''");

            // Autocomplete: substring match on subtag, which is how hydrus searches.
            e.HasIndex(x => x.Subtag)
                .HasDatabaseName("ix_tag_subtag_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
        });

        b.Entity<FileTag>(e =>
        {
            e.ToTable("file_tag");
            e.HasKey(x => new { x.ServiceId, x.Status, x.TagId, x.FileId });

            e.HasOne(x => x.File).WithMany(f => f.Tags)
                .HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Tag).WithMany()
                .HasForeignKey(x => x.TagId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Service).WithMany()
                .HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);

            // Reverse index: "all tags on this file". Mandatory -- without it
            // every media-details lookup scans the PK.
            e.HasIndex(x => new { x.ServiceId, x.Status, x.FileId, x.TagId });

            // Explicit, because ForeignKeyIndexConvention is off. Needed so the
            // ON DELETE CASCADE from file does not seq-scan this table. No
            // equivalent for tag_id: tags are RESTRICT and the drain never
            // deletes them, so that index would be dead weight.
            e.HasIndex(x => x.FileId);

            // Append-mostly; rows are inserted/deleted, never updated in place.
            e.HasAnnotation("Npgsql:StorageParameter:fillfactor", 100);
        });

        b.Entity<TagSibling>(e =>
        {
            e.ToTable("tag_sibling");
            e.HasKey(x => new { x.ServiceId, x.Status, x.BadTagId, x.GoodTagId });
            e.HasOne(x => x.BadTag).WithMany()
                .HasForeignKey(x => x.BadTagId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.GoodTag).WithMany()
                .HasForeignKey(x => x.GoodTagId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Service).WithMany()
                .HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<TagParent>(e =>
        {
            e.ToTable("tag_parent");
            e.HasKey(x => new { x.ServiceId, x.Status, x.ChildTagId, x.ParentTagId });
            e.HasOne(x => x.ChildTag).WithMany()
                .HasForeignKey(x => x.ChildTagId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ParentTag).WithMany()
                .HasForeignKey(x => x.ParentTagId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Service).WithMany()
                .HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(x => new { x.ServiceId, x.Status, x.ParentTagId, x.ChildTagId });
        });
    }

    private static void ConfigureUrlsNotesPhashes(ModelBuilder b)
    {
        b.Entity<Url>(e =>
        {
            e.ToTable("url");
            e.HasKey(x => x.UrlId);
            e.Property(x => x.UrlId).UseIdentityAlwaysColumn();
            e.Property(x => x.Address).HasColumnName("url");
            e.HasIndex(x => x.Address).IsUnique();
            e.HasIndex(x => x.Domain);
        });

        b.Entity<FileUrl>(e =>
        {
            e.ToTable("file_url");
            e.HasKey(x => new { x.FileId, x.UrlId });
            e.HasOne(x => x.File).WithMany(f => f.Urls)
                .HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Url).WithMany()
                .HasForeignKey(x => x.UrlId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.UrlId, x.FileId });
        });

        b.Entity<FileNote>(e =>
        {
            e.ToTable("file_note");
            e.HasKey(x => new { x.FileId, x.Name });
            e.HasOne(x => x.File).WithMany(f => f.Notes)
                .HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(x => x.Body)
                .HasDatabaseName("ix_file_note_body_trgm")
                .HasMethod("gin")
                .HasOperators("gin_trgm_ops");
        });

        b.Entity<Phash>(e =>
        {
            e.ToTable("phash");
            e.HasKey(x => x.PhashId);
            e.Property(x => x.PhashId).UseIdentityAlwaysColumn();
            e.Property(x => x.Value).HasColumnName("phash");
            e.HasIndex(x => x.Value).IsUnique();
        });

        b.Entity<FilePhash>(e =>
        {
            e.ToTable("file_phash");
            e.HasKey(x => new { x.FileId, x.PhashId });
            e.HasOne(x => x.File).WithMany(f => f.Phashes)
                .HasForeignKey(x => x.FileId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Phash).WithMany()
                .HasForeignKey(x => x.PhashId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PhashId, x.FileId });
        });
    }

    private static void ConfigureSync(ModelBuilder b)
    {
        b.Entity<SyncState>(e =>
        {
            e.ToTable("sync_state");
            e.HasKey(x => x.Stream);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
        });

        b.Entity<SyncRun>(e =>
        {
            e.ToTable("sync_run");
            e.HasKey(x => x.RunId);
            e.Property(x => x.RunId).UseIdentityAlwaysColumn();
            e.Property(x => x.StartedAt).HasDefaultValueSql("now()");
            e.Property(x => x.Detail).HasColumnType("jsonb");
        });

        b.Entity<SyncDiscrepancy>(e =>
        {
            e.ToTable("sync_discrepancy");
            e.HasKey(x => x.DiscrepancyId);
            e.Property(x => x.DiscrepancyId).UseIdentityAlwaysColumn();
            e.Property(x => x.FoundAt).HasDefaultValueSql("now()");
            e.Property(x => x.Detail).HasColumnType("jsonb");
        });
    }
}

// ============================================================================
// DI setup -- both halves are required.
//
//   var dsb = new NpgsqlDataSourceBuilder(connString);
//   dsb.MapEnum<ContentStatus>("hydrus.content_status");
//   dsb.MapEnum<Filetype>("hydrus.filetype");
//   dsb.MapEnum<ServiceType>("hydrus.service_type");
//   dsb.MapEnum<SyncKind>("hydrus.sync_kind");
//   dsb.MapEnum<DiscrepancyKind>("hydrus.discrepancy_kind");
//   var dataSource = dsb.Build();
//
//   services.AddDbContext<HydrusDbContext>(o => o
//       .UseNpgsql(dataSource)
//       .UseSnakeCaseNamingConvention());
//
// HasPostgresEnum alone makes migrations emit CREATE TYPE but does NOT teach
// the driver to read the values -- you get a runtime "unknown type" error.
//
// The same dataSource works for Dapper, and the enum mappings carry over. If a
// query ever needs to drop to Dapper, take an NpgsqlDataSource from DI rather
// than reusing the DbContext connection, so you are not fighting the change
// tracker over a connection it thinks it owns.
//
// MIGRATION GOTCHA: `ALTER TYPE ... ADD VALUE` cannot run inside a transaction,
// and EF wraps migrations in one. Adding a label later needs:
//     migrationBuilder.Sql("ALTER TYPE hydrus.service_type ADD VALUE 'x';",
//                          suppressTransaction: true);
// Keep such additions in their own migration -- that migration is non-atomic,
// so a later failing step will leave the new label behind.
//
// TOOLING: dotnet-ef 10.0.x cannot load a net11.0 project without
//     DOTNET_ROLL_FORWARD=Major dotnet ef ...
// until a net11-targeted tool ships.
//
// ---------------------------------------------------------------------------
// Operational notes
//
//  * PARTITIONING: if you sync the PTR, file_tag reaches 10^8-10^9 rows.
//    Convert it to PARTITION BY LIST (service_id) BEFORE the initial backfill.
//    EF cannot express native partitioning -- it needs a migrationBuilder.Sql
//    replacing the generated CREATE TABLE. Repartitioning a populated table of
//    that size is an outage, not a migration.
//
//  * BACKFILL ORDER: mime -> service -> file -> tag/url/phash -> mapping
//    tables. Create secondary indexes AFTER the bulk COPY; building them up
//    front roughly triples load time. Use Npgsql's binary COPY (NpgsqlBinary
//    Importer), not EF -- SaveChanges on 10^8 rows is not a viable path.
//
//  * DAPPER ESCAPE HATCH: the enum mappings live on NpgsqlDataSource, so
//    Dapper queries through the same data source get them for free. Inject
//    NpgsqlDataSource rather than borrowing DbContext.Database.GetDbConnection(),
//    so you are not fighting the change tracker over a connection it owns.
//
//  * Ratings (main.local_ratings, main.local_incdec_ratings) and viewing stats
//    (main.file_viewing_stats) are deliberately not modelled yet.
// ============================================================================
