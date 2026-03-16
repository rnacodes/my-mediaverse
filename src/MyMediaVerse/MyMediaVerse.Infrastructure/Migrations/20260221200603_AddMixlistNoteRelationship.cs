using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMixlistNoteRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MixlistNotes",
                columns: table => new
                {
                    MixlistId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LinkDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MixlistNotes", x => new { x.MixlistId, x.NoteId });
                    table.ForeignKey(
                        name: "FK_MixlistNotes_Mixlists_MixlistId",
                        column: x => x.MixlistId,
                        principalTable: "Mixlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MixlistNotes_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MixlistNotes_LinkedAt",
                table: "MixlistNotes",
                column: "LinkedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MixlistNotes_NoteId",
                table: "MixlistNotes",
                column: "NoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MixlistNotes");
        }
    }
}
