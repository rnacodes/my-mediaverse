using MyMediaVerse.Infrastructure.Services.Enrichment;
using MyMediaVerse.Infrastructure.Services.Search;
using MyMediaVerse.Infrastructure.Services.Sync;
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
        var isTesting = environment.IsTesting();

        services.AddEnrichmentWorker<NoteDescriptionGenerationHostedService, NoteDescriptionGenerationOptions>(
            configuration, NoteDescriptionGenerationOptions.SectionName, isTesting, logger,
            "Note description generation");

        services.AddEnrichmentWorker<ObsidianNoteSyncHostedService, ObsidianNoteSyncOptions>(
            configuration, ObsidianNoteSyncOptions.SectionName, isTesting, logger,
            "Obsidian note sync");

        services.AddEnrichmentWorker<SearchIndexSyncHostedService, SearchIndexSyncOptions>(
            configuration, SearchIndexSyncOptions.SectionName, isTesting, logger,
            "Search index sync");

        services.AddScoped<IBookDescriptionEnrichmentService, BookDescriptionEnrichmentService>();
        services.AddEnrichmentWorker<BookDescriptionEnrichmentHostedService, BookDescriptionEnrichmentOptions>(
            configuration, BookDescriptionEnrichmentOptions.SectionName, isTesting, logger,
            "Book description enrichment");

        // Derives the MMV Rating enum from the raw GoodreadsRating stored at import time (pure local
        // op, no external API — no background worker needed; triggered via the enrichment controller).
        services.AddScoped<IBookRatingEnrichmentService, BookRatingEnrichmentService>();

        services.AddScoped<IMovieTvEnrichmentService, MovieTvEnrichmentService>();
        services.AddEnrichmentWorker<MovieTvEnrichmentHostedService, MovieTvEnrichmentOptions>(
            configuration, MovieTvEnrichmentOptions.SectionName, isTesting, logger,
            "Movie/TV TMDB enrichment");

        services.AddScoped<IPodcastEnrichmentService, PodcastEnrichmentService>();
        services.AddEnrichmentWorker<PodcastEnrichmentHostedService, PodcastEnrichmentOptions>(
            configuration, PodcastEnrichmentOptions.SectionName, isTesting, logger,
            "Podcast ListenNotes enrichment");

        return services;
    }

    // Binds <typeparamref name="TOptions"/> from configuration and registers
    // <typeparamref name="THostedService"/> unless the host is running under integration tests.
    private static IServiceCollection AddEnrichmentWorker<THostedService, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string optionsSectionName,
        bool isTesting,
        ILogger logger,
        string description)
        where THostedService : class, IHostedService
        where TOptions : class
    {
        services.Configure<TOptions>(configuration.GetSection(optionsSectionName));

        if (!isTesting)
        {
            services.AddHostedService<THostedService>();
            logger.LogInformation("{Description} background service registered.", description);
        }

        return services;
    }
}
