using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoRefreshAndIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Videos_ExternalId",
                table: "Videos");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                table: "Videos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "YouTubeRefreshedAt",
                table: "Videos",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Videos_Platform_ExternalId",
                table: "Videos",
                columns: new[] { "Platform", "ExternalId" },
                unique: true,
                filter: "\"ExternalId\" IS NOT NULL AND \"ExternalId\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_YouTubeRefreshedAt",
                table: "Videos",
                column: "YouTubeRefreshedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Videos_Platform_ExternalId",
                table: "Videos");

            migrationBuilder.DropIndex(
                name: "IX_Videos_YouTubeRefreshedAt",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                table: "Videos");

            migrationBuilder.DropColumn(
                name: "YouTubeRefreshedAt",
                table: "Videos");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ExternalId",
                table: "Videos",
                column: "ExternalId");
        }
    }
}
