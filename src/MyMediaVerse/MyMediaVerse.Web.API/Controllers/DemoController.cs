using Microsoft.AspNetCore.Mvc;
using OtpNet;

namespace MyMediaVerse.Web.API.Controllers
{
    /// <summary>
    /// Controller for demo site TOTP-based write access unlock functionality.
    /// Allows temporary write access (20 minutes) using Google Authenticator or similar TOTP apps.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DemoController : ControllerBase
    {
        private readonly ILogger<DemoController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        private const string CookieName = "Demo_Write_Access";
        private const int WriteAccessMinutes = 20;

        public DemoController(
            ILogger<DemoController> logger,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _configuration = configuration;
            _environment = environment;
        }

        /// <summary>
        /// Unlocks write access for the demo site using a TOTP code from Google Authenticator.
        /// Sets an HTTP-only cookie that grants 20 minutes of write access.
        /// </summary>
        /// <param name="code">6-digit TOTP code from authenticator app</param>
        /// <returns>Success message or 401 Unauthorized</returns>
        [HttpGet("unlock")]
        public IActionResult Unlock([FromQuery] string code)
        {
            // Only allow this endpoint in Demo environment
            if (!_environment.EnvironmentName.Equals("Demo", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { error = "This endpoint is only available in Demo environment" });
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return BadRequest(new { error = "TOTP code is required" });
            }

            // Get the TOTP secret from environment variable or configuration
            var totpSecret = Environment.GetEnvironmentVariable("DEMO_TOTP_SECRET")
                             ?? _configuration["DemoTotpSecret"];

            if (string.IsNullOrEmpty(totpSecret))
            {
                _logger.LogError("DEMO_TOTP_SECRET is not configured. TOTP unlock is disabled.");
                return StatusCode(503, new { error = "TOTP unlock is not configured on this server" });
            }

            try
            {
                // Decode the Base32 secret
                var secretBytes = Base32Encoding.ToBytes(totpSecret);
                var totp = new Totp(secretBytes);

                // Verify the code (with a window of ±1 step to account for time drift)
                var isValid = totp.VerifyTotp(code, out long timeStepMatched, new VerificationWindow(1, 1));

                if (isValid)
                {
                    // Set HTTP-only, Secure, SameSite=Strict cookie
                    var cookieOptions = new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddMinutes(WriteAccessMinutes),
                        Path = "/"
                    };

                    Response.Cookies.Append(CookieName, "true", cookieOptions);

                    _logger.LogInformation(
                        "Demo write access unlocked via TOTP. Access expires at {ExpiryTime}",
                        DateTimeOffset.UtcNow.AddMinutes(WriteAccessMinutes));

                    return Ok(new
                    {
                        message = "Write access unlocked successfully!",
                        expiresInMinutes = WriteAccessMinutes,
                        expiresAt = DateTimeOffset.UtcNow.AddMinutes(WriteAccessMinutes)
                    });
                }
                else
                {
                    _logger.LogWarning("Invalid TOTP code attempt for demo write access");
                    return Unauthorized(new { error = "Invalid TOTP code" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying TOTP code");
                return StatusCode(500, new { error = "Error verifying TOTP code" });
            }
        }

        /// <summary>
        /// Revokes write access by clearing the cookie.
        /// </summary>
        [HttpGet("lock")]
        [HttpPost("lock")]
        public IActionResult Lock()
        {
            if (!_environment.EnvironmentName.Equals("Demo", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { error = "This endpoint is only available in Demo environment" });
            }

            Response.Cookies.Delete(CookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/"
            });

            _logger.LogInformation("Demo write access manually revoked");

            return Ok(new { message = "Write access revoked" });
        }

        /// <summary>
        /// Checks if write access is currently active.
        /// </summary>
        [HttpGet("status")]
        public IActionResult Status()
        {
            if (!_environment.EnvironmentName.Equals("Demo", StringComparison.OrdinalIgnoreCase))
            {
                return Ok(new
                {
                    isDemoEnvironment = false,
                    writeAccessEnabled = true,
                    message = "Not in demo environment - write operations are unrestricted"
                });
            }

            var hasWriteAccess = Request.Cookies.ContainsKey(CookieName) &&
                                 Request.Cookies[CookieName] == "true";

            return Ok(new
            {
                isDemoEnvironment = true,
                writeAccessEnabled = hasWriteAccess,
                message = hasWriteAccess
                    ? "Write access is enabled via TOTP"
                    : "Write access is disabled - use /api/demo/unlock with a valid TOTP code"
            });
        }

        /// <summary>
        /// Generates a new Base32 secret for TOTP setup.
        /// This endpoint is protected and only works in Development environment.
        /// After generating, save the secret to DEMO_TOTP_SECRET environment variable.
        /// </summary>
        [HttpGet("generate-secret")]
        public IActionResult GenerateSecret()
        {
            // Only allow in Development environment for security
            if (!_environment.IsDevelopment())
            {
                return NotFound(new { error = "This endpoint is only available in Development environment" });
            }

            // Generate a random 20-byte (160-bit) secret
            var secretBytes = KeyGeneration.GenerateRandomKey(20);
            var base32Secret = Base32Encoding.ToString(secretBytes);

            // Generate the otpauth URI for easy QR code scanning
            var issuer = "MyMediaVerse-Demo";
            var accountName = "admin@mymediaverseuniverse.com";
            var otpauthUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(accountName)}?secret={base32Secret}&issuer={Uri.EscapeDataString(issuer)}";

            return Ok(new
            {
                message = "New TOTP secret generated. Save this to your DEMO_TOTP_SECRET environment variable.",
                base32Secret = base32Secret,
                otpauthUri = otpauthUri,
                instructions = new[]
                {
                    "1. Copy the base32Secret value",
                    "2. Set it as DEMO_TOTP_SECRET environment variable on your server",
                    "3. Open Google Authenticator on your phone",
                    "4. Tap '+' to add a new account",
                    "5. Choose 'Enter a setup key'",
                    "6. Enter account name (e.g., 'MyMediaVerse Demo')",
                    "7. Paste the base32Secret as the key",
                    "8. Make sure 'Time based' is selected",
                    "9. Tap 'Add' to save",
                    "",
                    "Alternative: Generate a QR code from the otpauthUri and scan it with Google Authenticator"
                }
            });
        }
    }
}
