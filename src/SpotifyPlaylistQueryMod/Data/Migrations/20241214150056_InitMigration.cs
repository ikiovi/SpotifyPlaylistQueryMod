using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SpotifyPlaylistQueryMod.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "source_playlists",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", unicode: false, nullable: false),
                    snapshot_id = table.Column<string>(type: "text", unicode: false, nullable: false),
                    owner_id = table.Column<string>(type: "text", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false),
                    is_processing = table.Column<bool>(type: "boolean", nullable: false),
                    next_check = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_source_playlists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "target_playlists",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", unicode: false, nullable: false),
                    owner_id = table.Column<string>(type: "text", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_target_playlists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", unicode: false, nullable: false),
                    privileges = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    refresh_token = table.Column<string>(type: "text", nullable: false),
                    next_refresh = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_collaboration_enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    track_id = table.Column<string>(type: "text", nullable: false),
                    added_by = table.Column<string>(type: "text", nullable: true),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    source_playlist_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracks", x => x.id);
                    table.ForeignKey(
                        name: "fk_tracks_source_playlists_source_playlist_id",
                        column: x => x.source_playlist_id,
                        principalTable: "source_playlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "queries",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    source_id = table.Column<string>(type: "text", nullable: false),
                    target_id = table.Column<string>(type: "text", nullable: true),
                    query = table.Column<string>(type: "text", nullable: false),
                    is_paused = table.Column<bool>(type: "boolean", nullable: false),
                    is_superseded = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queries", x => x.id);
                    table.ForeignKey(
                        name: "fk_queries_source_playlists_source_id",
                        column: x => x.source_id,
                        principalTable: "source_playlists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_queries_target_playlists_target_id",
                        column: x => x.target_id,
                        principalTable: "target_playlists",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_queries_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "queries_state",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    last_run_snapshot_id = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    input_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_queries_state", x => x.id);
                    table.ForeignKey(
                        name: "fk_queries_state_queries_id",
                        column: x => x.id,
                        principalTable: "queries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_queries_source_id",
                table: "queries",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "ix_queries_target_id",
                table: "queries",
                column: "target_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_queries_user_id",
                table: "queries",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracks_source_playlist_id",
                table: "tracks",
                column: "source_playlist_id");

            migrationBuilder.CreateIndex(
                name: "ix_unique_superadmin",
                table: "users",
                column: "privileges",
                unique: true,
                filter: "Privileges = -42");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "queries_state");

            migrationBuilder.DropTable(
                name: "tracks");

            migrationBuilder.DropTable(
                name: "queries");

            migrationBuilder.DropTable(
                name: "source_playlists");

            migrationBuilder.DropTable(
                name: "target_playlists");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
