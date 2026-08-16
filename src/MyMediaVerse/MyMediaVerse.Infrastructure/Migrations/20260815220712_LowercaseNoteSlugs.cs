using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <summary>
    /// Data migration: note slugs are stored lowercase from now on (all write paths
    /// normalize), so lowercase the existing rows to match. The unique (VaultName, Slug)
    /// index makes any casing collision fail loudly instead of silently merging notes.
    /// </summary>
    public partial class LowercaseNoteSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""UPDATE "Notes" SET "Slug" = LOWER("Slug") WHERE "Slug" <> LOWER("Slug");""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Original casing is not recoverable; the next vault sync would restore
            // nothing here anyway since slugs are matched case-insensitively.
        }
    }
}
