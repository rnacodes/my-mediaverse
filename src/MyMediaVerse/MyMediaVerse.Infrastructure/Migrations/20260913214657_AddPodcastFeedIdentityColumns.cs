using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPodcastFeedIdentityColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeedGuid",
                table: "PodcastSeries",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedUrlKey",
                table: "PodcastSeries",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Language",
                table: "PodcastSeries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastEnrichmentAttemptAt",
                table: "PodcastSeries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataSource",
                table: "PodcastSeries",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "manual");

            migrationBuilder.AddColumn<long>(
                name: "PodcastIndexId",
                table: "PodcastSeries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RssGuid",
                table: "PodcastEpisodes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PodcastSeries_ApplePodcastsId",
                table: "PodcastSeries",
                column: "ApplePodcastsId",
                unique: true,
                filter: "\"ApplePodcastsId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastSeries_FeedGuid",
                table: "PodcastSeries",
                column: "FeedGuid",
                unique: true,
                filter: "\"FeedGuid\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastSeries_FeedUrlKey",
                table: "PodcastSeries",
                column: "FeedUrlKey",
                unique: true,
                filter: "\"FeedUrlKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastSeries_PodcastIndexId",
                table: "PodcastSeries",
                column: "PodcastIndexId",
                unique: true,
                filter: "\"PodcastIndexId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastEpisodes_SeriesId_RssGuid",
                table: "PodcastEpisodes",
                columns: new[] { "SeriesId", "RssGuid" },
                unique: true,
                filter: "\"RssGuid\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PodcastSeries_ApplePodcastsId",
                table: "PodcastSeries");

            migrationBuilder.DropIndex(
                name: "IX_PodcastSeries_FeedGuid",
                table: "PodcastSeries");

            migrationBuilder.DropIndex(
                name: "IX_PodcastSeries_FeedUrlKey",
                table: "PodcastSeries");

            migrationBuilder.DropIndex(
                name: "IX_PodcastSeries_PodcastIndexId",
                table: "PodcastSeries");

            migrationBuilder.DropIndex(
                name: "IX_PodcastEpisodes_SeriesId_RssGuid",
                table: "PodcastEpisodes");

            migrationBuilder.DropColumn(
                name: "FeedGuid",
                table: "PodcastSeries");

            migrationBuilder.DropColumn(
                name: "FeedUrlKey",
                table: "PodcastSeries");

            migrationBuilder.DropColumn(
                name: "Language",
                table: "PodcastSeries");

            migrationBuilder.DropColumn(
                name: "LastEnrichmentAttemptAt",
                table: "PodcastSeries");

            migrationBuilder.DropColumn(
                name: "MetadataSource",
                table: "PodcastSeries");

            migrationBuilder.DropColumn(
                name: "PodcastIndexId",
                table: "PodcastSeries");

            migrationBuilder.DropColumn(
                name: "RssGuid",
                table: "PodcastEpisodes");
        }
    }
}
