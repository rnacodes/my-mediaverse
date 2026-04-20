using Amazon.S3;
using MyMediaVerse.Infrastructure.Services.Storage;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Extensions;

public static class StorageExtensions
{
    /// <summary>
    /// Registers the DigitalOcean Spaces S3 client plus the IThumbnailStorageService
    /// abstraction most consumers should use. UploadController still injects IAmazonS3?
    /// directly for its raw multipart/file endpoints.
    ///
    /// The <c>null!</c> return on IAmazonS3 is intentional: ASP.NET Core DI does not
    /// treat nullable reference type annotations as "optional service" — a consumer
    /// taking <c>IAmazonS3?</c> still requires a registration, and the factory is the
    /// only mechanism that can inject null.
    /// </summary>
    public static IServiceCollection AddS3Storage(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.Storage");

            var spacesConfig = configuration.GetSection("DigitalOceanSpaces");
            var accessKey = spacesConfig["AccessKey"];
            var secretKey = spacesConfig["SecretKey"];
            var endpoint = spacesConfig["Endpoint"];
            var region = spacesConfig["Region"];
            var bucketName = spacesConfig["BucketName"];

            if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(secretKey) ||
                string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(region) ||
                string.IsNullOrEmpty(bucketName))
            {
                logger.LogWarning("DigitalOcean Spaces configuration is incomplete. Thumbnail upload functionality will not be available until properly configured.");
                return null!;
            }

            var config = new AmazonS3Config
            {
                ServiceURL = $"https://{endpoint}",
                ForcePathStyle = false
            };

            return new AmazonS3Client(accessKey, secretKey, config);
        });

        services.AddScoped<IThumbnailStorageService, ThumbnailStorageService>();

        return services;
    }
}
