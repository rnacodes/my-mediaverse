namespace MyMediaVerse.Shared.Configuration
{
    /// <summary>
    /// Settings for the unified Readwise sync. Bound from the "ReadwiseSync" configuration section;
    /// every value has a code default so no settings file entry is required.
    /// </summary>
    public class ReadwiseSyncOptions
    {
        public const string SectionName = "ReadwiseSync";

        /// <summary>
        /// Wall-clock budget for the highlights step of one sync call, in seconds. When it runs out
        /// the step stops between export pages, saves its place, and the next call continues.
        /// Zero disables the budget.
        /// </summary>
        public int HighlightsTimeBudgetSeconds { get; set; } = 240;
    }
}
