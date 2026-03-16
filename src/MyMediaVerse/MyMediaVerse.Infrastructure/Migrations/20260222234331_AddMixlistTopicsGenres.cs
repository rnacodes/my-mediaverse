using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMixlistTopicsGenres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MixlistGenres",
                columns: table => new
                {
                    MixlistId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenreId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MixlistGenres", x => new { x.MixlistId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_MixlistGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MixlistGenres_Mixlists_MixlistId",
                        column: x => x.MixlistId,
                        principalTable: "Mixlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MixlistTopics",
                columns: table => new
                {
                    MixlistId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MixlistTopics", x => new { x.MixlistId, x.TopicId });
                    table.ForeignKey(
                        name: "FK_MixlistTopics_Mixlists_MixlistId",
                        column: x => x.MixlistId,
                        principalTable: "Mixlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MixlistTopics_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MixlistGenres_GenreId",
                table: "MixlistGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_MixlistTopics_TopicId",
                table: "MixlistTopics",
                column: "TopicId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MixlistGenres");

            migrationBuilder.DropTable(
                name: "MixlistTopics");
        }
    }
}
