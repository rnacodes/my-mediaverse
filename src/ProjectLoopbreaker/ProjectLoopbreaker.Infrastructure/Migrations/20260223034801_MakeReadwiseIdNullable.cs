using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectLoopbreaker.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeReadwiseIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Highlights_ReadwiseId",
                table: "Highlights");

            migrationBuilder.AlterColumn<int>(
                name: "ReadwiseId",
                table: "Highlights",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_ReadwiseId",
                table: "Highlights",
                column: "ReadwiseId",
                unique: true,
                filter: "\"ReadwiseId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Highlights_ReadwiseId",
                table: "Highlights");

            migrationBuilder.AlterColumn<int>(
                name: "ReadwiseId",
                table: "Highlights",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_ReadwiseId",
                table: "Highlights",
                column: "ReadwiseId",
                unique: true);
        }
    }
}
