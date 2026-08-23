using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        // Entity collections for querying
        IQueryable<BaseMediaItem> MediaItems { get; }
        IQueryable<Mixlist> Mixlists { get; }
        IQueryable<PodcastSeries> PodcastSeries { get; }
        IQueryable<PodcastEpisode> PodcastEpisodes { get; }
        IQueryable<Book> Books { get; }
        IQueryable<Movie> Movies { get; }
        IQueryable<TvShow> TvShows { get; }
        IQueryable<Video> Videos { get; }
        IQueryable<YouTubeChannel> YouTubeChannels { get; }
        IQueryable<YouTubePlaylist> YouTubePlaylists { get; }
        IQueryable<Article> Articles { get; }
        IQueryable<Website> Websites { get; }
        IQueryable<Document> Documents { get; }
        IQueryable<Topic> Topics { get; }
        IQueryable<Genre> Genres { get; }
        IQueryable<Highlight> Highlights { get; }
        IQueryable<RefreshToken> RefreshTokens { get; }
        IQueryable<Note> Notes { get; }
        IQueryable<MediaItemNote> MediaItemNotes { get; }
        IQueryable<MixlistNote> MixlistNotes { get; }
        IQueryable<MediaItemRelation> MediaItemRelations { get; }
        IQueryable<TvShowEpisode> TvShowEpisodes { get; }
        IQueryable<TraktToken> TraktTokens { get; }
        IQueryable<SyncState> SyncStates { get; }

        // Basic operations
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        // Entity tracking
        void Add<TEntity>(TEntity entity) where TEntity : class;
        void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
        void Update<TEntity>(TEntity entity) where TEntity : class;
        void Remove<TEntity>(TEntity entity) where TEntity : class;
        void ClearChangeTracker();

        // Entity finding
        Task<TEntity?> FindAsync<TEntity>(params object[] keyValues) where TEntity : class;
    }
}
