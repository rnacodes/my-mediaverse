using System.ComponentModel.DataAnnotations;

namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Stores Trakt OAuth tokens for the single-user app.
    /// Only one row should exist at a time.
    /// </summary>
    public class TraktToken
    {
        public int Id { get; set; }

        [Required]
        public required string AccessToken { get; set; }

        [Required]
        public required string RefreshToken { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [StringLength(200)]
        public string? TraktUsername { get; set; }
    }
}
