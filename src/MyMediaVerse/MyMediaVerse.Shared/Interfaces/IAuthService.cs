namespace MyMediaVerse.Shared.Interfaces
{
    /// <summary>
    /// Statistics returned by <see cref="IAuthService.CleanupExpiredAndRevokedTokensAsync"/>.
    /// </summary>
    public record TokenCleanupResult(int ExpiredCount, int RevokedCount, int TotalDeleted);

    /// <summary>
    /// Service for handling JWT authentication operations
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Generates a JWT access token for the specified user
        /// </summary>
        /// <param name="username">Username for the token</param>
        /// <param name="userId">User ID for the token</param>
        /// <param name="expirationMinutes">Token expiration time in minutes</param>
        /// <returns>JWT token string</returns>
        string GenerateAccessToken(string username, string userId, int expirationMinutes);
        
        /// <summary>
        /// Generates a unique refresh token string
        /// </summary>
        /// <returns>Refresh token string</returns>
        string GenerateRefreshToken();
        
        /// <summary>
        /// Saves a refresh token to the database
        /// </summary>
        /// <param name="userId">User ID associated with the token</param>
        /// <param name="token">The refresh token string</param>
        /// <param name="expirationDays">Token expiration time in days</param>
        /// <returns>Task</returns>
        Task SaveRefreshTokenAsync(string userId, string token, int expirationDays);
        
        /// <summary>
        /// Validates a refresh token and returns the userId if valid and active
        /// </summary>
        /// <param name="token">The refresh token to validate</param>
        /// <returns>The userId if token is valid, null otherwise</returns>
        Task<string?> ValidateRefreshTokenAsync(string token);
        
        /// <summary>
        /// Revokes a refresh token, optionally specifying what replaced it
        /// </summary>
        /// <param name="token">The token to revoke</param>
        /// <param name="replacedByToken">Optional token that replaced this one</param>
        Task RevokeRefreshTokenAsync(string token, string? replacedByToken = null);
        
        /// <summary>
        /// Revokes all refresh tokens for a specific user
        /// </summary>
        /// <param name="userId">User ID whose tokens should be revoked</param>
        Task RevokeAllUserTokensAsync(string userId);
        
        /// <summary>
        /// Removes expired refresh tokens from the database
        /// </summary>
        Task CleanupExpiredTokensAsync();

        /// <summary>
        /// Removes both expired and explicitly revoked refresh tokens, returning
        /// statistics about how many of each were deleted.
        /// </summary>
        Task<TokenCleanupResult> CleanupExpiredAndRevokedTokensAsync();
    }
}
