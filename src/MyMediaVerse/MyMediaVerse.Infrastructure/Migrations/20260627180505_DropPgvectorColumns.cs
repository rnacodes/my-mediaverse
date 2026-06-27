using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DropPgvectorColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmbeddingGeneratedAt",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "EmbeddingGeneratedAt",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "MediaItems");

            // The pgvector Embedding columns were not tracked by the EF model (they were ignored and
            // managed via raw SQL), so EF cannot scaffold their removal automatically. Drop them
            // explicitly before removing the extension they depend on.
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "MediaItems");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingGeneratedAt",
                table: "Notes",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "Notes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingGeneratedAt",
                table: "MediaItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "MediaItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Embedding",
                table: "Notes",
                type: "vector(1024)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Embedding",
                table: "MediaItems",
                type: "vector(1024)",
                nullable: true);
        }
    }
}
