using MyMediaVerse.Infrastructure.Services;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Extensions;

public static class BackgroundServicesExtensions
{
    public static IServiceCollection AddBackgroundServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment,
        ILogger logger)
    {
        var isTesting = environment.EnvironmentName == "Testing";

        services.Configure<NoteDescriptionGenerationOptions>(
            configuration.GetSection(NoteDescriptionGenerationOptions.SectionName));
        if (!isTesting)
        {
            services.AddHostedService<NoteDescriptionGenerationHostedService>();
            logger.LogInformation("Note description generation background service registered.");
        }

        services.Configure<EmbeddingGenerationOptions>(
            configuration.GetSection(EmbeddingGenerationOptions.SectionName));
        if (!isTesting)
        {
            services.AddHostedService<EmbeddingGenerationHostedService>();
            logger.LogInformation("Embedding generation background service registered.");
        }

        services.Configure<ObsidianNoteSyncOptions>(
            configuration.GetSection(ObsidianNoteSyncOptions.SectionName));
        if (!isTesting)
        {
            services.AddHostedService<ObsidianNoteSyncHostedService>();
            logger.LogInformation("Obsidian note sync background service registered.");
        }

        services.Configure<BookDescriptionEnrichmentOptions>(
            configuration.GetSection(BookDescriptionEnrichmentOptions.SectionName));
        services.AddScoped<IBookDescriptionEnrichmentService, BookDescriptionEnrichmentService>();
        if (!isTesting)
        {
            services.AddHostedService<BookDescriptionEnrichmentHostedService>();
            logger.LogInformation("Book description enrichment background service registered.");
        }

        services.Configure<MovieTvEnrichmentOptions>(
            configuration.GetSection(MovieTvEnrichmentOptions.SectionName));
        services.AddScoped<IMovieTvEnrichmentService, MovieTvEnrichmentService>();
        if (!isTesting)
        {
            services.AddHostedService<MovieTvEnrichmentHostedService>();
            logger.LogInformation("Movie/TV TMDB enrichment background service registered.");
        }

        services.Configure<PodcastEnrichmentOptions>(
            configuration.GetSection(PodcastEnrichmentOptions.SectionName));
        services.AddScoped<IPodcastEnrichmentService, PodcastEnrichmentService>();
        if (!isTesting)
        {
            services.AddHostedService<PodcastEnrichmentHostedService>();
            logger.LogInformation("Podcast ListenNotes enrichment background service registered.");
        }

        return services;
    }
}
