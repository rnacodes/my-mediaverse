namespace MyMediaVerse.Shared.Configuration
{
    /// <summary>
    /// Configuration options for AI note description generation, shared by the
    /// background hosted service (scheduling) and the AI service (generation).
    /// </summary>
    public class NoteDescriptionGenerationOptions
    {
        public const string SectionName = "NoteDescriptionGeneration";

        /// <summary>
        /// Whether the background service is enabled. Default: false
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Hours between generation runs. Default: 12
        /// </summary>
        public int IntervalHours { get; set; } = 12;

        /// <summary>
        /// Initial delay in minutes before the first run. Default: 20
        /// (gives time for note sync to complete first)
        /// </summary>
        public int InitialDelayMinutes { get; set; } = 20;

        /// <summary>
        /// Number of notes to process per batch. Default: 20
        /// </summary>
        public int BatchSize { get; set; } = 20;

        /// <summary>
        /// Maximum tokens for generated descriptions. Default: 200
        /// </summary>
        public int MaxTokensPerDescription { get; set; } = 200;

        /// <summary>
        /// Delay in milliseconds between AI calls within a batch, to avoid rate limiting. Default: 1000
        /// </summary>
        public int DelayBetweenCallsMs { get; set; } = 1000;
    }
}
