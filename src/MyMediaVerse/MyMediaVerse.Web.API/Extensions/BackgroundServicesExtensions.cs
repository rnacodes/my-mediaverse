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
        IWebHostEnvironment environment)
    {
        var isTesting = environment.IsTesting();

        services.AddEnrichmentWorker<NoteDescriptionGenerationHostedService, NoteDescriptionGenerationOptions>(
            configuration, NoteDescriptionGenerationOptions.SectionName, isTesting);

        services.AddEnrichmentWorker<ObsidianNoteSyncHostedService, ObsidianNoteSyncOptions>(
            configuration, ObsidianNoteSyncOptions.SectionName, isTesting);

        services.AddEnrichmentWorker<SearchIndexSyncHostedService, SearchIndexSyncOptions>(
            configuration, SearchIndexSyncOptions.SectionName, isTesting);

        services.AddScoped<IBookDescriptionEnrichmentService, BookDescriptionEnrichmentService>();
        services.AddEnrichmentWorker<BookDescriptionEnrichmentHostedService, BookDescriptionEnrichmentOptions>(
            configuration, BookDescriptionEnrichmentOptions.SectionName, isTesting);

        // Derives the MMV Rating enum from the raw GoodreadsRating stored at import time (pure local
        // op, no external API — no background worker needed; triggered via the enrichment controller).
        services.AddScoped<IBookRatingEnrichmentService, BookRatingEnrichmentService>();

        services.AddScoped<IMovieTvEnrichmentService, MovieTvEnrichmentService>();
        services.AddEnrichmentWorker<MovieTvEnrichmentHostedService, MovieTvEnrichmentOptions>(
            configuration, MovieTvEnrichmentOptions.SectionName, isTesting);

        services.AddScoped<IPodcastEnrichmentService, PodcastEnrichmentService>();
        services.AddEnrichmentWorker<PodcastEnrichmentHostedService, PodcastEnrichmentOptions>(
            configuration, PodcastEnrichmentOptions.SectionName, isTesting);

        return services;
    }

    // Binds <typeparamref name="TOptions"/> from configuration and registers
    // <typeparamref name="THostedService"/> unless the host is running under integration tests.
    // Registration does NOT imply the worker runs: all workers default Enabled=false and each worker's
    // ExecuteAsync logs its effective on/off state (N8N owns the ongoing cadence), so nothing is logged here.
    private static IServiceCollection AddEnrichmentWorker<THostedService, TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string optionsSectionName,
        bool isTesting)
        where THostedService : class, IHostedService
        where TOptions : class
    {
        services.Configure<TOptions>(configuration.GetSection(optionsSectionName));

        if (!isTesting)
        {
            services.AddHostedService<THostedService>();
        }

        return services;
    }
}
