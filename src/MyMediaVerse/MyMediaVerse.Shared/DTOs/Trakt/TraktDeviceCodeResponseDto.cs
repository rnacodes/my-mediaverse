namespace MyMediaVerse.Shared.DTOs.Trakt
{
    /// <summary>
    /// Device-code details returned to our own clients.
    /// Kept separate from <see cref="TraktDeviceCodeDto"/>, which mirrors Trakt's
    /// snake_case wire format, so this response follows the API's camelCase convention.
    /// </summary>
    public class TraktDeviceCodeResponseDto
    {
        public string DeviceCode { get; set; } = string.Empty;

        public string UserCode { get; set; } = string.Empty;

        public string VerificationUrl { get; set; } = string.Empty;

        public int ExpiresIn { get; set; }

        public int Interval { get; set; }
    }
}
