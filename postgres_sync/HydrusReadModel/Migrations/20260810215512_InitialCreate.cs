using System;
using HydrusReadModel;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HydrusReadModel.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hydrus");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:hydrus.content_status", "current,deleted,pending,petitioned")
                .Annotation("Npgsql:Enum:hydrus.discrepancy_kind", "extra,mismatch,missing")
                .Annotation("Npgsql:Enum:hydrus.filetype", "animation,application,archive,audio,image,image_project,video")
                .Annotation("Npgsql:Enum:hydrus.service_type", "client_api_service,combined_deleted_file,combined_file,combined_local_file_domains,combined_tag,file_repository,hydrus_local_file_storage,ipfs,local_booru,local_file_domain,local_file_trash_domain,local_file_update_domain,local_notes,local_rating_incdec,local_rating_like,local_rating_numerical,local_tag,message_depot,null_service,rating_like_repository,rating_numerical_repository,server_admin,tag_repository,test_service")
                .Annotation("Npgsql:Enum:hydrus.sync_kind", "backfill,drain,reconcile")
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "mime",
                schema: "hydrus",
                columns: table => new
                {
                    code = table.Column<string>(type: "text", nullable: false),
                    media_type = table.Column<string>(type: "text", nullable: false),
                    extension = table.Column<string>(type: "text", nullable: false),
                    filetype = table.Column<Filetype>(type: "hydrus.filetype", nullable: false),
                    hydrus_enum = table.Column<short>(type: "smallint", nullable: false),
                    searchable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mime", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "phash",
                schema: "hydrus",
                columns: table => new
                {
                    phash_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    phash = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_phash", x => x.phash_id);
                });

            migrationBuilder.CreateTable(
                name: "service",
                schema: "hydrus",
                columns: table => new
                {
                    service_id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    service_key = table.Column<byte[]>(type: "bytea", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    service_type = table.Column<ServiceType>(type: "hydrus.service_type", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service", x => x.service_id);
                    table.CheckConstraint("ck_service_key_length", "octet_length(service_key) = 32");
                });

            migrationBuilder.CreateTable(
                name: "sync_discrepancy",
                schema: "hydrus",
                columns: table => new
                {
                    discrepancy_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    found_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    sha256 = table.Column<byte[]>(type: "bytea", nullable: true),
                    kind = table.Column<DiscrepancyKind>(type: "hydrus.discrepancy_kind", nullable: false),
                    detail = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_discrepancy", x => x.discrepancy_id);
                });

            migrationBuilder.CreateTable(
                name: "sync_run",
                schema: "hydrus",
                columns: table => new
                {
                    run_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rows_applied = table.Column<long>(type: "bigint", nullable: false),
                    kind = table.Column<SyncKind>(type: "hydrus.sync_kind", nullable: false),
                    ok = table.Column<bool>(type: "boolean", nullable: true),
                    detail = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_run", x => x.run_id);
                });

            migrationBuilder.CreateTable(
                name: "sync_state",
                schema: "hydrus",
                columns: table => new
                {
                    stream = table.Column<string>(type: "text", nullable: false),
                    last_seq = table.Column<long>(type: "bigint", nullable: false),
                    source_db_id = table.Column<byte[]>(type: "bytea", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sync_state", x => x.stream);
                });

            migrationBuilder.CreateTable(
                name: "tag",
                schema: "hydrus",
                columns: table => new
                {
                    tag_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    @namespace = table.Column<string>(name: "namespace", type: "text", nullable: false, defaultValue: ""),
                    subtag = table.Column<string>(type: "text", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: false, computedColumnSql: "CASE WHEN namespace = '' THEN subtag ELSE namespace || ':' || subtag END", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag", x => x.tag_id);
                });

            migrationBuilder.CreateTable(
                name: "url",
                schema: "hydrus",
                columns: table => new
                {
                    url_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    url = table.Column<string>(type: "text", nullable: false),
                    domain = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_url", x => x.url_id);
                });

            migrationBuilder.CreateTable(
                name: "file",
                schema: "hydrus",
                columns: table => new
                {
                    file_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sha256 = table.Column<byte[]>(type: "bytea", nullable: false),
                    size = table.Column<long>(type: "bigint", nullable: true),
                    duration_ms = table.Column<long>(type: "bigint", nullable: true),
                    num_frames = table.Column<long>(type: "bigint", nullable: true),
                    num_words = table.Column<long>(type: "bigint", nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    has_audio = table.Column<bool>(type: "boolean", nullable: true),
                    mime_code = table.Column<string>(type: "text", nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    synced_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file", x => x.file_id);
                    table.CheckConstraint("ck_file_sha256_length", "octet_length(sha256) = 32");
                    table.ForeignKey(
                        name: "fk_file_mime_mime_code",
                        column: x => x.mime_code,
                        principalSchema: "hydrus",
                        principalTable: "mime",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tag_parent",
                schema: "hydrus",
                columns: table => new
                {
                    service_id = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<ContentStatus>(type: "hydrus.content_status", nullable: false),
                    child_tag_id = table.Column<int>(type: "integer", nullable: false),
                    parent_tag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_parent", x => new { x.service_id, x.status, x.child_tag_id, x.parent_tag_id });
                    table.ForeignKey(
                        name: "fk_tag_parent_service_service_id",
                        column: x => x.service_id,
                        principalSchema: "hydrus",
                        principalTable: "service",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tag_parent_tag_child_tag_id",
                        column: x => x.child_tag_id,
                        principalSchema: "hydrus",
                        principalTable: "tag",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tag_parent_tag_parent_tag_id",
                        column: x => x.parent_tag_id,
                        principalSchema: "hydrus",
                        principalTable: "tag",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tag_sibling",
                schema: "hydrus",
                columns: table => new
                {
                    service_id = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<ContentStatus>(type: "hydrus.content_status", nullable: false),
                    bad_tag_id = table.Column<int>(type: "integer", nullable: false),
                    good_tag_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_sibling", x => new { x.service_id, x.status, x.bad_tag_id, x.good_tag_id });
                    table.ForeignKey(
                        name: "fk_tag_sibling_service_service_id",
                        column: x => x.service_id,
                        principalSchema: "hydrus",
                        principalTable: "service",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tag_sibling_tag_bad_tag_id",
                        column: x => x.bad_tag_id,
                        principalSchema: "hydrus",
                        principalTable: "tag",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_tag_sibling_tag_good_tag_id",
                        column: x => x.good_tag_id,
                        principalSchema: "hydrus",
                        principalTable: "tag",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_note",
                schema: "hydrus",
                columns: table => new
                {
                    file_id = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_note", x => new { x.file_id, x.name });
                    table.ForeignKey(
                        name: "fk_file_note_file_file_id",
                        column: x => x.file_id,
                        principalSchema: "hydrus",
                        principalTable: "file",
                        principalColumn: "file_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "file_other_hash",
                schema: "hydrus",
                columns: table => new
                {
                    file_id = table.Column<int>(type: "integer", nullable: false),
                    md5 = table.Column<byte[]>(type: "bytea", nullable: true),
                    sha1 = table.Column<byte[]>(type: "bytea", nullable: true),
                    sha512 = table.Column<byte[]>(type: "bytea", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_other_hash", x => x.file_id);
                    table.ForeignKey(
                        name: "fk_file_other_hash_file_file_id",
                        column: x => x.file_id,
                        principalSchema: "hydrus",
                        principalTable: "file",
                        principalColumn: "file_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "file_phash",
                schema: "hydrus",
                columns: table => new
                {
                    file_id = table.Column<int>(type: "integer", nullable: false),
                    phash_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_phash", x => new { x.file_id, x.phash_id });
                    table.ForeignKey(
                        name: "fk_file_phash_file_file_id",
                        column: x => x.file_id,
                        principalSchema: "hydrus",
                        principalTable: "file",
                        principalColumn: "file_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_file_phash_phash_phash_id",
                        column: x => x.phash_id,
                        principalSchema: "hydrus",
                        principalTable: "phash",
                        principalColumn: "phash_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_service_status",
                schema: "hydrus",
                columns: table => new
                {
                    file_id = table.Column<int>(type: "integer", nullable: false),
                    service_id = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<ContentStatus>(type: "hydrus.content_status", nullable: false),
                    imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    original_imported_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_service_status", x => new { x.service_id, x.status, x.file_id });
                    table.ForeignKey(
                        name: "fk_file_service_status_file_file_id",
                        column: x => x.file_id,
                        principalSchema: "hydrus",
                        principalTable: "file",
                        principalColumn: "file_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_file_service_status_service_service_id",
                        column: x => x.service_id,
                        principalSchema: "hydrus",
                        principalTable: "service",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "file_tag",
                schema: "hydrus",
                columns: table => new
                {
                    service_id = table.Column<short>(type: "smallint", nullable: false),
                    status = table.Column<ContentStatus>(type: "hydrus.content_status", nullable: false),
                    tag_id = table.Column<int>(type: "integer", nullable: false),
                    file_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_tag", x => new { x.service_id, x.status, x.tag_id, x.file_id });
                    table.ForeignKey(
                        name: "fk_file_tag_file_file_id",
                        column: x => x.file_id,
                        principalSchema: "hydrus",
                        principalTable: "file",
                        principalColumn: "file_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_file_tag_service_service_id",
                        column: x => x.service_id,
                        principalSchema: "hydrus",
                        principalTable: "service",
                        principalColumn: "service_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_file_tag_tag_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "hydrus",
                        principalTable: "tag",
                        principalColumn: "tag_id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("Npgsql:StorageParameter:fillfactor", 100);

            migrationBuilder.CreateTable(
                name: "file_url",
                schema: "hydrus",
                columns: table => new
                {
                    file_id = table.Column<int>(type: "integer", nullable: false),
                    url_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_url", x => new { x.file_id, x.url_id });
                    table.ForeignKey(
                        name: "fk_file_url_file_file_id",
                        column: x => x.file_id,
                        principalSchema: "hydrus",
                        principalTable: "file",
                        principalColumn: "file_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_file_url_url_url_id",
                        column: x => x.url_id,
                        principalSchema: "hydrus",
                        principalTable: "url",
                        principalColumn: "url_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_file_mime_code",
                schema: "hydrus",
                table: "file",
                column: "mime_code");

            migrationBuilder.CreateIndex(
                name: "ix_file_sha256",
                schema: "hydrus",
                table: "file",
                column: "sha256",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_file_note_body_trgm",
                schema: "hydrus",
                table: "file_note",
                column: "body")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_file_other_hash_md5",
                schema: "hydrus",
                table: "file_other_hash",
                column: "md5",
                filter: "md5 IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_file_other_hash_sha1",
                schema: "hydrus",
                table: "file_other_hash",
                column: "sha1",
                filter: "sha1 IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_file_phash_phash_id_file_id",
                schema: "hydrus",
                table: "file_phash",
                columns: new[] { "phash_id", "file_id" });

            migrationBuilder.CreateIndex(
                name: "ix_file_service_status_current",
                schema: "hydrus",
                table: "file_service_status",
                columns: new[] { "service_id", "imported_at", "file_id" },
                descending: new[] { false, true, false },
                filter: "status = 'current'");

            migrationBuilder.CreateIndex(
                name: "ix_file_service_status_file_id_service_id",
                schema: "hydrus",
                table: "file_service_status",
                columns: new[] { "file_id", "service_id" })
                .Annotation("Npgsql:IndexInclude", new[] { "status" });

            migrationBuilder.CreateIndex(
                name: "ix_file_tag_file_id",
                schema: "hydrus",
                table: "file_tag",
                column: "file_id");

            migrationBuilder.CreateIndex(
                name: "ix_file_tag_service_id_status_file_id_tag_id",
                schema: "hydrus",
                table: "file_tag",
                columns: new[] { "service_id", "status", "file_id", "tag_id" });

            migrationBuilder.CreateIndex(
                name: "ix_file_url_url_id_file_id",
                schema: "hydrus",
                table: "file_url",
                columns: new[] { "url_id", "file_id" });

            migrationBuilder.CreateIndex(
                name: "ix_mime_extension",
                schema: "hydrus",
                table: "mime",
                column: "extension");

            migrationBuilder.CreateIndex(
                name: "ix_mime_filetype",
                schema: "hydrus",
                table: "mime",
                column: "filetype");

            migrationBuilder.CreateIndex(
                name: "ix_mime_hydrus_enum",
                schema: "hydrus",
                table: "mime",
                column: "hydrus_enum",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mime_media_type",
                schema: "hydrus",
                table: "mime",
                column: "media_type");

            migrationBuilder.CreateIndex(
                name: "ix_phash_phash",
                schema: "hydrus",
                table: "phash",
                column: "phash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_service_key",
                schema: "hydrus",
                table: "service",
                column: "service_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_service_service_type",
                schema: "hydrus",
                table: "service",
                column: "service_type");

            migrationBuilder.CreateIndex(
                name: "ix_tag_namespace",
                schema: "hydrus",
                table: "tag",
                column: "namespace",
                filter: "namespace <> ''");

            migrationBuilder.CreateIndex(
                name: "ix_tag_namespace_subtag",
                schema: "hydrus",
                table: "tag",
                columns: new[] { "namespace", "subtag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tag_subtag_trgm",
                schema: "hydrus",
                table: "tag",
                column: "subtag")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_tag_parent_service_id_status_parent_tag_id_child_tag_id",
                schema: "hydrus",
                table: "tag_parent",
                columns: new[] { "service_id", "status", "parent_tag_id", "child_tag_id" });

            migrationBuilder.CreateIndex(
                name: "ix_url_domain",
                schema: "hydrus",
                table: "url",
                column: "domain");

            migrationBuilder.CreateIndex(
                name: "ix_url_url",
                schema: "hydrus",
                table: "url",
                column: "url",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_note",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "file_other_hash",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "file_phash",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "file_service_status",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "file_tag",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "file_url",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "sync_discrepancy",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "sync_run",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "sync_state",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "tag_parent",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "tag_sibling",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "phash",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "file",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "url",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "service",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "tag",
                schema: "hydrus");

            migrationBuilder.DropTable(
                name: "mime",
                schema: "hydrus");
        }
    }
}
