namespace MyMediaVerse.Shared.Interfaces
{
    // Domain-level abstraction over blob storage for media thumbnails.
    // Lives in Shared so both Application (PodcastMappingService, MediaService)
    // and Infrastructure (the S3-backed implementation) can reference it
    // without Application having to depend on Infrastructure.
    public interface IThumbnailStorageService
    {
        // Downloads the image at imageUrl, stores it under the given key prefix
        // (e.g. "thumbnails/imported_"), and returns the new public URL.
        // Returns the original imageUrl when storage is unavailable or the upload
        // fails -- callers get graceful degradation rather than exceptions.
        Task<string?> UploadFromUrlAsync(string? imageUrl, string keyPrefix);

        // Deletes a previously-uploaded thumbnail by its public URL.
        // No-op when storage is unavailable or the URL isn't one we manage;
        // never throws, so callers can invoke in cleanup paths without guarding.
        Task DeleteAsync(string? publicUrl);
    }
}
