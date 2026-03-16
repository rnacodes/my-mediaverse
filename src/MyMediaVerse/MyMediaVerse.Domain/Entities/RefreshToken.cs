namespace MyMediaVerse.Domain.Entities
{
    /// <summary>
    /// Represents a refresh token for JWT authentication
    /// </summary>
    public class RefreshToken
    {
        public int Id { get; set; }
        
        /// <summary>
        /// The actual refresh token string
        /// </summary>
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// User identifier (username or email) associated with this token
        /// </summary>
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// When the token was created
        /// </summary>
        public DateTime CreatedAt { get; set; }
        
        /// <summary>
        /// When the token expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }
        
        /// <summary>
        /// Whether the token has been revoked
        /// </summary>
        public bool IsRevoked { get; set; }
        
        /// <summary>
        /// When the token was revoked (if applicable)
        /// </summary>
        public DateTime? RevokedAt { get; set; }
        
        /// <summary>
        /// Token that replaced this one during rotation
        /// </summary>
        public string? ReplacedByToken { get; set; }
        
        /// <summary>
        /// Whether the token is currently active
        /// </summary>
        public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }
}

