using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ProjectLoopbreaker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTraktAndTvEpisodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TraktId",
                table: "TvShows",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TraktLastWatchedAt",
                table: "TvShows",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TraktPlays",
                table: "TvShows",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TraktRating",
                table: "TvShows",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraktSlug",
                table: "TvShows",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TraktId",
                table: "Movies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TraktLastWatchedAt",
                table: "Movies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TraktPlays",
                table: "Movies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TraktRating",
                table: "Movies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraktSlug",
                table: "Movies",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TraktTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TraktUsername = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TraktTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TvShowEpisodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShowId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonNumber = table.Column<int>(type: "integer", nullable: true),
                    EpisodeNumber = table.Column<int>(type: "integer", nullable: true),
                    AirDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationInMinutes = table.Column<int>(type: "integer", nullable: true),
                    TmdbEpisodeId = table.Column<int>(type: "integer", nullable: true),
                    TraktEpisodeId = table.Column<int>(type: "integer", nullable: true),
                    StillPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TraktPlays = table.Column<int>(type: "integer", nullable: true),
                    TraktLastWatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvShowEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TvShowEpisodes_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TvShowEpisodes_TvShows_ShowId",
                        column: x => x.ShowId,
                        principalTable: "TvShows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_AirDate",
                table: "TvShowEpisodes",
                column: "AirDate");

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_ShowId",
                table: "TvShowEpisodes",
                column: "ShowId");

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_TmdbEpisodeId",
                table: "TvShowEpisodes",
                column: "TmdbEpisodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TraktTokens");

            migrationBuilder.DropTable(
                name: "TvShowEpisodes");

            migrationBuilder.DropColumn(
                name: "TraktId",
                table: "TvShows");

            migrationBuilder.DropColumn(
                name: "TraktLastWatchedAt",
                table: "TvShows");

            migrationBuilder.DropColumn(
                name: "TraktPlays",
                table: "TvShows");

            migrationBuilder.DropColumn(
                name: "TraktRating",
                table: "TvShows");

            migrationBuilder.DropColumn(
                name: "TraktSlug",
                table: "TvShows");

            migrationBuilder.DropColumn(
                name: "TraktId",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TraktLastWatchedAt",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TraktPlays",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TraktRating",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TraktSlug",
                table: "Movies");
        }
    }
}
