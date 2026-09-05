using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Infrastructure.Services.Sync;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Web.API.Extensions;

public static class ApplicationServicesExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPodcastMappingService, PodcastMappingService>();
        services.AddScoped<IPodcastService, PodcastService>();
        services.AddScoped<IBookService, BookService>();
        services.AddScoped<IBookMappingService, BookMappingService>();
        services.AddScoped<IMovieService, MovieService>();
        services.AddScoped<IMovieMappingService, MovieMappingService>();
        services.AddScoped<ITvShowService, TvShowService>();
        services.AddScoped<ITvShowMappingService, TvShowMappingService>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<IYouTubeService, YouTubeService>();
        services.AddScoped<IYouTubeMappingService, YouTubeMappingService>();
        services.AddScoped<IYouTubeChannelService, YouTubeChannelService>();
        services.AddScoped<IYouTubePlaylistService, YouTubePlaylistService>();
        services.AddScoped<ITmdbService, TmdbService>();
        services.AddScoped<IListenNotesService, ListenNotesService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IArticleMappingService, ArticleMappingService>();
        services.AddScoped<IArticleDeduplicationService, ArticleDeduplicationService>();
        services.AddScoped<IWebsiteService, WebsiteService>();
        services.AddScoped<IWebsiteMappingService, WebsiteMappingService>();
        services.AddScoped<IGoodreadsImportService, GoodreadsImportService>();
        services.AddScoped<IPodcastOpmlImportService, PodcastOpmlImportService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<ITopicsService, TopicsService>();
        services.AddScoped<IGenresService, GenresService>();
        services.AddScoped<IGenreMappingService, GenreMappingService>();
        services.AddScoped<IRelatedMediaService, RelatedMediaService>();
        services.AddScoped<IMixlistService, MixlistService>();

        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IDocumentMappingService, DocumentMappingService>();

        services.AddScoped<IHighlightService, HighlightService>();
        services.AddScoped<IReadwiseService, ReadwiseService>();
        services.AddScoped<IReaderService, ReaderService>();
        services.AddScoped<ISyncStateService, SyncStateService>();
        services.AddScoped<IReadwiseSyncService, ReadwiseSyncService>();

        services.AddScoped<IOpenLibraryService, OpenLibraryService>();
        services.AddScoped<IGoogleBooksService, GoogleBooksService>();

        // External-source facades (narrow business surface; delegate to source services).
        services.AddScoped<IExternalMovieService, ExternalMovieService>();
        services.AddScoped<IExternalTvShowService, ExternalTvShowService>();
        services.AddScoped<IExternalPodcastService, ExternalPodcastService>();

        services.AddScoped<INoteService, NoteService>();
        services.AddScoped<IAIService, AIService>();

        services.AddScoped<IRecommendationService, RecommendationService>();

        services.AddScoped<ITraktSyncService, TraktSyncService>();

        return services;
    }
}
