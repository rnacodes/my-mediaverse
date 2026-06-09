using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PodcastUniqueExternalIdIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PodcastSeries_ExternalId",
                table: "PodcastSeries");

            migrationBuilder.DropIndex(
                name: "IX_PodcastEpisodes_ExternalId",
                table: "PodcastEpisodes");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastSeries_ExternalId",
                table: "PodcastSeries",
                column: "ExternalId",
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastEpisodes_SeriesId_ExternalId",
                table: "PodcastEpisodes",
                columns: new[] { "SeriesId", "ExternalId" },
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PodcastSeries_ExternalId",
                table: "PodcastSeries");

            migrationBuilder.DropIndex(
                name: "IX_PodcastEpisodes_SeriesId_ExternalId",
                table: "PodcastEpisodes");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastSeries_ExternalId",
                table: "PodcastSeries",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastEpisodes_ExternalId",
                table: "PodcastEpisodes",
                column: "ExternalId");
        }
    }
}
