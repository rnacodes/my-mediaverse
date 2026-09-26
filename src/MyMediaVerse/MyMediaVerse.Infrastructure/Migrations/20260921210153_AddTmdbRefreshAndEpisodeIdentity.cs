using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTmdbRefreshAndEpisodeIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TvShows_TmdbId",
                table: "TvShows");

            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies");

            migrationBuilder.AddColumn<DateTime>(
                name: "TmdbRefreshedAt",
                table: "TvShows",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TmdbRefreshedAt",
                table: "Movies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TvShows_TmdbId",
                table: "TvShows",
                column: "TmdbId",
                unique: true,
                filter: "\"TmdbId\" IS NOT NULL AND \"TmdbId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_TvShows_TmdbRefreshedAt",
                table: "TvShows",
                column: "TmdbRefreshedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TvShowEpisodes_ShowId_SeasonNumber_EpisodeNumber",
                table: "TvShowEpisodes",
                columns: new[] { "ShowId", "SeasonNumber", "EpisodeNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                column: "TmdbId",
                unique: true,
                filter: "\"TmdbId\" IS NOT NULL AND \"TmdbId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbRefreshedAt",
                table: "Movies",
                column: "TmdbRefreshedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TvShows_TmdbId",
                table: "TvShows");

            migrationBuilder.DropIndex(
                name: "IX_TvShows_TmdbRefreshedAt",
                table: "TvShows");

            migrationBuilder.DropIndex(
                name: "IX_TvShowEpisodes_ShowId_SeasonNumber_EpisodeNumber",
                table: "TvShowEpisodes");

            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies");

            migrationBuilder.DropIndex(
                name: "IX_Movies_TmdbRefreshedAt",
                table: "Movies");

            migrationBuilder.DropColumn(
                name: "TmdbRefreshedAt",
                table: "TvShows");

            migrationBuilder.DropColumn(
                name: "TmdbRefreshedAt",
                table: "Movies");

            migrationBuilder.CreateIndex(
                name: "IX_TvShows_TmdbId",
                table: "TvShows",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                column: "TmdbId");
        }
    }
}
