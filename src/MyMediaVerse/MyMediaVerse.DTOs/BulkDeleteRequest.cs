namespace MyMediaVerse.DTOs
{
    public class BulkDeleteRequest
    {
        public List<Guid> Ids { get; set; } = new List<Guid>();
    }
}
