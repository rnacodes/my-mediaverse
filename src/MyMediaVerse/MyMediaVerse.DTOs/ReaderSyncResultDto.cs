namespace MyMediaVerse.DTOs
{
    public class ReaderSyncResultDto
    {
        public bool Success { get; set; }

        // Stable identifier of the operation for sync-state records and notifications.
        public string Operation { get; set; } = "reader-sync";
        public int CreatedCount { get; set; }
        public int UpdatedCount { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public int TotalProcessed => CreatedCount + UpdatedCount;
        public TimeSpan? Duration => CompletedAt.HasValue 
            ? CompletedAt.Value - StartedAt 
            : null;
    }
}

