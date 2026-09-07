using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddWebsiteIdentityAndEnrichmentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EnrichedAt",
                table: "Websites",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LastHttpStatus",
                table: "Websites",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrlKey",
                table: "Websites",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Websites_UrlKey",
                table: "Websites",
                column: "UrlKey",
                unique: true,
                filter: "\"UrlKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Websites_UrlKey",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "EnrichedAt",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "LastHttpStatus",
                table: "Websites");

            migrationBuilder.DropColumn(
                name: "UrlKey",
                table: "Websites");
        }
    }
}
