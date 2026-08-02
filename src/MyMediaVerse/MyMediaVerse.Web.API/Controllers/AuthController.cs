using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;
using MyMediaVerse.Web.API.Conventions;

namespace MyMediaVerse.Web.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;
        private readonly IAuthService _authService;
        private readonly IWebHostEnvironment _environment;

        public AuthController(
            IConfiguration configuration,
            ILogger<AuthController> logger,
            IAuthService authService,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            _logger = logger;
            _authService = authService;
            _environment = environment;
        }

        /// <summary>
        /// Login endpoint that validates credentials and returns a JWT access token
        /// Sets the refresh token as an HttpOnly cookie
        /// Not routed on the Demo host, where sign-in happens via the TOTP unlock instead
        /// </summary>
        /// <param name="model">Login credentials (username and password)</param>
        /// <returns>JWT access token if credentials are valid</returns>
        [Environments("Production", "Development", "Testing")]
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.Username) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Username and password are required" });
            }

            var validUsername = Environment.GetEnvironmentVariable("AUTH_USERNAME") ??
                               _configuration["Auth:Username"];

            var validPassword = Environment.GetEnvironmentVariable("AUTH_PASSWORD") ??
                               _configuration["Auth:Password"];

            // Well-known defaults are a convenience for local development only; on any other
            // host, unconfigured credentials mean no login is possible rather than a guessable one.
            if (_environment.IsDevelopment())
            {
                validUsername ??= "admin";
                validPassword ??= "password123";
            }

            if (string.IsNullOrEmpty(validUsername) || string.IsNullOrEmpty(validPassword))
            {
                _logger.LogError("Login rejected: AUTH_USERNAME/AUTH_PASSWORD are not configured on this host.");
                return Unauthorized(new { message = "Invalid username or password" });
            }

            if (model.Username != validUsername || model.Password != validPassword)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", model.Username);
                return Unauthorized(new { message = "Invalid username or password" });
            }

            // Generate Tokens
            try
            {
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var userId = Guid.NewGuid().ToString(); // In production, get from database
                
                // Parse token expiration settings
                var accessTokenMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");
                var refreshTokenDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");
                
                // Generate short-lived access token
                var accessToken = _authService.GenerateAccessToken(model.Username, userId, accessTokenMinutes);
                var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenMinutes);
                
                // Generate long-lived refresh token
                var refreshToken = _authService.GenerateRefreshToken();
                await _authService.SaveRefreshTokenAsync(userId, refreshToken, refreshTokenDays);
                
                // Set refresh token as HttpOnly cookie
                // Automatically use Secure=false in Development, Secure=true in Production
                // Can be overridden with JwtSettings:RequireHttps config or REQUIRE_HTTPS env var
                var requireHttps = GetRequireHttpsSetting();
                
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,     // Prevents XSS attacks from reading the token
                    Secure = requireHttps,  // Auto: false in Development, true in Production
                    SameSite = SameSiteMode.Lax, // Allows cookies on navigation, protects against CSRF
                    Expires = DateTime.UtcNow.AddDays(refreshTokenDays)
                };
                
                Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);
                
                _logger.LogInformation("Refresh token cookie set with Secure={Secure} (Environment: {Environment})", 
                    requireHttps, _environment.EnvironmentName);

                _logger.LogInformation("User {Username} logged in successfully with dual token system", model.Username);

                // Return only the access token in the response body
                return Ok(new LoginResponseDto
                {
                    Token = accessToken,
                    Username = model.Username,
                    ExpiresAt = accessTokenExpiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating authentication tokens");
                return StatusCode(500, new { message = "An error occurred while generating authentication tokens" });
            }
        }

        /// <summary>
        /// Refresh endpoint that exchanges a valid refresh token for a new access token
        /// Implements refresh token rotation for enhanced security
        /// </summary>
        /// <returns>New JWT access token</returns>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                // Read refresh token from HttpOnly cookie
                if (!Request.Cookies.TryGetValue("refresh_token", out var refreshToken) || string.IsNullOrEmpty(refreshToken))
                {
                    _logger.LogWarning("Refresh token not found in cookie");
                    return Unauthorized(new { message = "Refresh token not found" });
                }

                // Validate the refresh token
                var userId = await _authService.ValidateRefreshTokenAsync(refreshToken);
                if (userId == null)
                {
                    _logger.LogWarning("Invalid or expired refresh token");
                    
                    // Clear the invalid cookie
                    Response.Cookies.Delete("refresh_token");
                    
                    return Unauthorized(new { message = "Invalid or expired refresh token" });
                }

                var jwtSettings = _configuration.GetSection("JwtSettings");
                var accessTokenMinutes = int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15");
                var refreshTokenDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "7");

                // Generate new access token
                var newAccessToken = _authService.GenerateAccessToken(
                    userId, 
                    userId, 
                    accessTokenMinutes);
                var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(accessTokenMinutes);

                // Generate new refresh token (Rotation)
                var newRefreshToken = _authService.GenerateRefreshToken();
                
                // Revoke old refresh token and save new one
                await _authService.RevokeRefreshTokenAsync(refreshToken, newRefreshToken);
                await _authService.SaveRefreshTokenAsync(userId, newRefreshToken, refreshTokenDays);

                // Set new refresh token as HttpOnly cookie
                var requireHttps = GetRequireHttpsSetting();
                
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = requireHttps,  // Auto: false in Development, true in Production
                    SameSite = SameSiteMode.Lax, // Allows cookies on navigation, protects against CSRF
                    Expires = DateTime.UtcNow.AddDays(refreshTokenDays)
                };
                
                Response.Cookies.Append("refresh_token", newRefreshToken, cookieOptions);

                _logger.LogInformation("Access token refreshed for user {UserId}", userId);

                // Return new access token
                return Ok(new LoginResponseDto
                {
                    Token = newAccessToken,
                    Username = userId,
                    ExpiresAt = accessTokenExpiresAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing access token");
                return StatusCode(500, new { message = "An error occurred while refreshing authentication token" });
            }
        }

        /// <summary>
        /// Validates if the current access token is still valid
        /// </summary>
        /// <returns>Token validation result</returns>
        [HttpGet("validate")]
        [Authorize]
        public IActionResult ValidateToken()
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            return Ok(new { 
                valid = true, 
                username = username,
                message = "Token is valid" 
            });
        }

        /// <summary>
        /// Logout endpoint that revokes all refresh tokens and clears the cookie
        /// </summary>
        /// <returns>Success message</returns>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userId = User.FindFirst("userId")?.Value;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    // Revoke all refresh tokens for this user
                    await _authService.RevokeAllUserTokensAsync(userId);
                    _logger.LogInformation("Revoked all refresh tokens for user {UserId}", userId);
                }

                // Clear the refresh token cookie
                Response.Cookies.Delete("refresh_token");
                
                _logger.LogInformation("User {UserId} logged out successfully", userId);
                
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }
        
        /// <summary>
        /// Cleanup expired and revoked refresh tokens from the database
        /// </summary>
        /// <returns>Statistics about deleted tokens</returns>
        [HttpPost("cleanup-tokens")]
        [Authorize]
        public async Task<IActionResult> CleanupTokens()
        {
            try
            {
                _logger.LogInformation("Starting refresh tokens cleanup...");

                var result = await _authService.CleanupExpiredAndRevokedTokensAsync();

                _logger.LogInformation("Deleted {TotalDeleted} expired/revoked refresh tokens (Expired: {Expired}, Revoked: {Revoked})",
                    result.TotalDeleted, result.ExpiredCount, result.RevokedCount);

                return Ok(new
                {
                    message = "Refresh tokens cleanup completed successfully",
                    deleted = new
                    {
                        expiredTokens = result.ExpiredCount,
                        revokedTokens = result.RevokedCount,
                        totalDeleted = result.TotalDeleted
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during refresh tokens cleanup");
                return StatusCode(500, new { message = "An error occurred during token cleanup" });
            }
        }

        /// <summary>
        /// Determines whether to require HTTPS for secure cookies
        /// Priority: 1. REQUIRE_HTTPS env var, 2. JwtSettings:RequireHttps config, 3. Environment-based default
        /// </summary>
        private bool GetRequireHttpsSetting()
        {
            // Check environment variable first (highest priority)
            var envVar = Environment.GetEnvironmentVariable("REQUIRE_HTTPS");
            if (!string.IsNullOrEmpty(envVar) && bool.TryParse(envVar, out var envValue))
            {
                _logger.LogInformation("Using REQUIRE_HTTPS environment variable: {Value}", envValue);
                return envValue;
            }
            
            // Check configuration setting (second priority)
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var configValue = jwtSettings["RequireHttps"];
            if (!string.IsNullOrEmpty(configValue) && bool.TryParse(configValue, out var configBool))
            {
                _logger.LogInformation("Using JwtSettings:RequireHttps configuration: {Value}", configBool);
                return configBool;
            }
            
            // Default: true for Production/Staging, false for Development (lowest priority)
            var defaultValue = !_environment.IsDevelopment();
            _logger.LogInformation("Using environment-based default for RequireHttps: {Value} (Environment: {Environment})", 
                defaultValue, _environment.EnvironmentName);
            return defaultValue;
        }
    }
}
