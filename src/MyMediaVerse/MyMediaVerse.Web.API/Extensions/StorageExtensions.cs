using Amazon.S3;

namespace MyMediaVerse.Web.API.Extensions;

public static class StorageExtensions
{
    /// <summary>
    /// Registers the DigitalOcean Spaces S3 client. When config is incomplete, returns null so
    /// consumers (e.g. UploadController) can handle the disabled state gracefully.
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

        return services;
    }
}
