using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// This is a no-op migration that updates the EF Core model snapshot to stop tracking
    /// Embedding properties on MediaItems and Notes tables. The columns remain in the database
    /// and are handled via raw SQL in AIService and VectorSearchRepository.
    /// </summary>
    public partial class RemoveEmbeddingFromEfCoreModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: Embedding columns remain in the database.
            // We're just updating the EF Core model snapshot to stop tracking them,
            // since they're managed via raw SQL for pgvector compatibility.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: Embedding columns already exist in the database.
        }
    }
}
