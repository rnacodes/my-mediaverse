namespace MyMediaVerse.DTOs
{
    public class ReadwiseSyncResultDto
    {
        public bool Success { get; set; }
        public int BooksCreated { get; set; }
        public int BooksUpdated { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public int TotalProcessed => BooksCreated + BooksUpdated;
        public TimeSpan? Duration => CompletedAt.HasValue 
            ? CompletedAt.Value - StartedAt 
            : null;
    }
}

