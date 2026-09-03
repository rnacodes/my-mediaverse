using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookExternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GoodreadsBookId",
                table: "Books",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GoogleVolumeId",
                table: "Books",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpenLibraryKey",
                table: "Books",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReadwiseBookId",
                table: "Books",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_GoodreadsBookId",
                table: "Books",
                column: "GoodreadsBookId",
                unique: true,
                filter: "\"GoodreadsBookId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Books_GoogleVolumeId",
                table: "Books",
                column: "GoogleVolumeId",
                unique: true,
                filter: "\"GoogleVolumeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Books_OpenLibraryKey",
                table: "Books",
                column: "OpenLibraryKey",
                unique: true,
                filter: "\"OpenLibraryKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Books_ReadwiseBookId",
                table: "Books",
                column: "ReadwiseBookId",
                unique: true,
                filter: "\"ReadwiseBookId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_GoodreadsBookId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_GoogleVolumeId",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_OpenLibraryKey",
                table: "Books");

            migrationBuilder.DropIndex(
                name: "IX_Books_ReadwiseBookId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "GoodreadsBookId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "GoogleVolumeId",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "OpenLibraryKey",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "ReadwiseBookId",
                table: "Books");
        }
    }
}
