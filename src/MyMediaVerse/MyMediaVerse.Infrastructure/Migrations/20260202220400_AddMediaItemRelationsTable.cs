using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaItemRelationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaItemRelations",
                columns: table => new
                {
                    SourceMediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    RelatedMediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SimilarityScore = table.Column<double>(type: "double precision", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItemRelations", x => new { x.SourceMediaItemId, x.RelatedMediaItemId });
                    table.ForeignKey(
                        name: "FK_MediaItemRelations_MediaItems_RelatedMediaItemId",
                        column: x => x.RelatedMediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaItemRelations_MediaItems_SourceMediaItemId",
                        column: x => x.SourceMediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MediaItemRelations_CreatedAt",
                table: "MediaItemRelations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItemRelations_RelatedMediaItemId",
                table: "MediaItemRelations",
                column: "RelatedMediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItemRelations_Source",
                table: "MediaItemRelations",
                column: "Source");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItemRelations_SourceMediaItemId",
                table: "MediaItemRelations",
                column: "SourceMediaItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MediaItemRelations");
        }
    }
}
