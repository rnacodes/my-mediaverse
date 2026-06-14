namespace MyMediaVerse.Shared.Interfaces
{
    public record ThumbnailUploadResult(string PublicUrl, string Key);

    // Storage is used only for manually-uploaded images and items the provider
    // supplies no image for; provider-supplied image URLs are stored and served
    // directly without mirroring.
    public interface IThumbnailStorageService
    {
        Task<ThumbnailUploadResult?> UploadStreamAsync(Stream content, string contentType, string keyPrefix);

        Task DeleteAsync(string? publicUrl);
    }
}
