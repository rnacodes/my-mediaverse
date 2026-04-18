using MyMediaVerse.Infrastructure.Services;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Extensions;

public static class BackgroundServicesExtensions
{
    public static IServiceCollection AddBackgroundServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var isTesting = environment.EnvironmentName == "Testing";

        services.Configure<NoteDescriptionGenerationOptions>(
            configuration.GetSection(NoteDescriptionGenerationOptions.SectionName));
        if (!isTesting)
        {
            services.AddHostedService<NoteDescriptionGenerationHostedService>();
            Console.WriteLine("Note description generation background service registered.");
        }

        services.Configure<EmbeddingGenerationOptions>(
            configuration.GetSection(EmbeddingGenerationOptions.SectionName));
        if (!isTesting)
        {
            services.AddHostedService<EmbeddingGenerationHostedService>();
            Console.WriteLine("Embedding generation background service registered.");
        }

        services.Configure<ObsidianNoteSyncOptions>(
            configuration.GetSection(ObsidianNoteSyncOptions.SectionName));
        if (!isTesting)
        {
            services.AddHostedService<ObsidianNoteSyncHostedService>();
            Console.WriteLine("Obsidian note sync background service registered.");
        }

        services.Configure<BookDescriptionEnrichmentOptions>(
            configuration.GetSection(BookDescriptionEnrichmentOptions.SectionName));
        services.AddScoped<IBookDescriptionEnrichmentService, BookDescriptionEnrichmentService>();
        if (!isTesting)
        {
            services.AddHostedService<BookDescriptionEnrichmentHostedService>();
            Console.WriteLine("Book description enrichment background service registered.");
        }

        services.Configure<MovieTvEnrichmentOptions>(
            configuration.GetSection(MovieTvEnrichmentOptions.SectionName));
        services.AddScoped<IMovieTvEnrichmentService, MovieTvEnrichmentService>();
        if (!isTesting)
        {
            services.AddHostedService<MovieTvEnrichmentHostedService>();
            Console.WriteLine("Movie/TV TMDB enrichment background service registered.");
        }

        services.Configure<PodcastEnrichmentOptions>(
            configuration.GetSection(PodcastEnrichmentOptions.SectionName));
        services.AddScoped<IPodcastEnrichmentService, PodcastEnrichmentService>();
        if (!isTesting)
        {
            services.AddHostedService<PodcastEnrichmentHostedService>();
            Console.WriteLine("Podcast ListenNotes enrichment background service registered.");
        }

        return services;
    }
}
