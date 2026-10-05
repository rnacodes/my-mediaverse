namespace MyMediaVerse.DTOs
{
    /// <summary>
    /// Result of a generic CSV upload, in the sync/import reporting contract shape.
    /// </summary>
    public class CsvUploadResultDto
    {
        public const string CsvUploadOperation = "csv-upload";

        /// <summary>Fatal-only flag. Per-row failures do not flip it.</summary>
        public bool Success { get; set; } = true;

        public string Operation { get; set; } = CsvUploadOperation;

        public int TotalProcessed => CreatedCount + SkippedCount + FailedCount;

        public int CreatedCount { get; set; }

        /// <summary>Rows for an item that is already in the library, or that appeared earlier in the file.</summary>
        public int SkippedCount { get; set; }

        public int FailedCount { get; set; }

        /// <summary>One line per failed row.</summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>One line per skipped row.</summary>
        public List<string> Skipped { get; set; } = new();

        public List<CsvImportedItemDto> ImportedItems { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public string? WarningMessage { get; set; }

        public bool ReindexTriggered { get; set; }

        public DateTime StartedAt { get; set; }

        public DateTime? CompletedAt { get; set; }

        public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : null;
    }

    public class CsvImportedItemDto
    {
        public Guid Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string MediaType { get; set; } = string.Empty;
    }
}
