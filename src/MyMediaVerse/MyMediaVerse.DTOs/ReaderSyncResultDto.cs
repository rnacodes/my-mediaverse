namespace MyMediaVerse.DTOs
{
    public class ReaderSyncResultDto
    {
        public bool Success { get; set; }
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

