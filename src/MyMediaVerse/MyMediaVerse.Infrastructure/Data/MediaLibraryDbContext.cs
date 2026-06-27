using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Application.Interfaces;
using System.Collections.Generic;

namespace MyMediaVerse.Infrastructure.Data

{
    public class MediaLibraryDbContext : DbContext, IApplicationDbContext
    {
        // Entity Framework DbSet properties
        public DbSet<BaseMediaItem> MediaItems { get; set; }
        public DbSet<Mixlist> Mixlists { get; set; }
        public DbSet<PodcastSeries> PodcastSeries { get; set; }
        public DbSet<PodcastEpisode> PodcastEpisodes { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<TvShow> TvShows { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<YouTubeChannel> YouTubeChannels { get; set; }
        public DbSet<YouTubePlaylist> YouTubePlaylists { get; set; }
        public DbSet<Article> Articles { get; set; }
        public DbSet<Website> Websites { get; set; }
        public DbSet<Document> Documents { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<Genre> Genres { get; set; }
        public DbSet<Highlight> Highlights { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Note> Notes { get; set; }
        public DbSet<MediaItemNote> MediaItemNotes { get; set; }
        public DbSet<MixlistNote> MixlistNotes { get; set; }
        public DbSet<MediaItemRelation> MediaItemRelations { get; set; }
        public DbSet<TvShowEpisode> TvShowEpisodes { get; set; }
        public DbSet<TraktToken> TraktTokens { get; set; }

        // IApplicationDbContext interface implementations
        IQueryable<BaseMediaItem> IApplicationDbContext.MediaItems => MediaItems;
        IQueryable<Mixlist> IApplicationDbContext.Mixlists => Mixlists;
        IQueryable<PodcastSeries> IApplicationDbContext.PodcastSeries => PodcastSeries;
        IQueryable<PodcastEpisode> IApplicationDbContext.PodcastEpisodes => PodcastEpisodes;
        IQueryable<Book> IApplicationDbContext.Books => Books;
        IQueryable<Movie> IApplicationDbContext.Movies => Movies;
        IQueryable<TvShow> IApplicationDbContext.TvShows => TvShows;
        IQueryable<Video> IApplicationDbContext.Videos => Videos;
        IQueryable<YouTubeChannel> IApplicationDbContext.YouTubeChannels => YouTubeChannels;
        IQueryable<YouTubePlaylist> IApplicationDbContext.YouTubePlaylists => YouTubePlaylists;
        IQueryable<Article> IApplicationDbContext.Articles => Articles;
        IQueryable<Website> IApplicationDbContext.Websites => Websites;
        IQueryable<Document> IApplicationDbContext.Documents => Documents;
        IQueryable<Topic> IApplicationDbContext.Topics => Topics;
        IQueryable<Genre> IApplicationDbContext.Genres => Genres;
        IQueryable<Highlight> IApplicationDbContext.Highlights => Highlights;
        IQueryable<RefreshToken> IApplicationDbContext.RefreshTokens => RefreshTokens;
        IQueryable<Note> IApplicationDbContext.Notes => Notes;
        IQueryable<MediaItemNote> IApplicationDbContext.MediaItemNotes => MediaItemNotes;
        IQueryable<MixlistNote> IApplicationDbContext.MixlistNotes => MixlistNotes;
        IQueryable<MediaItemRelation> IApplicationDbContext.MediaItemRelations => MediaItemRelations;
        IQueryable<TvShowEpisode> IApplicationDbContext.TvShowEpisodes => TvShowEpisodes;
        IQueryable<TraktToken> IApplicationDbContext.TraktTokens => TraktTokens;


        public MediaLibraryDbContext(DbContextOptions<MediaLibraryDbContext> options) : base(options) { }




        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure BaseMediaItem entity
            modelBuilder.Entity<BaseMediaItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                // Configure enum conversions to store as strings for better readability
                entity.Property(e => e.Rating)
                    .HasConversion<string>()
                    .HasMaxLength(50);
                    
                entity.Property(e => e.MediaType)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();
                    
                entity.Property(e => e.Status)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();
                    
                entity.Property(e => e.OwnershipStatus)
                    .HasConversion<string>()
                    .HasMaxLength(50);

                // Configure string length constraints
                entity.Property(e => e.Title)
                    .HasMaxLength(500)
                    .IsRequired();
                    
                entity.Property(e => e.Link)
                    .HasMaxLength(2000);
                    
                // Topics and Genres are now navigation properties, no column configuration needed
                    
                entity.Property(e => e.Thumbnail)
                    .HasMaxLength(2000);
                    
                // Configure required fields
                entity.Property(e => e.DateAdded)
                    .IsRequired();
            });

            // Configure Mixlist entity
            modelBuilder.Entity<Mixlist>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Name)
                    .HasMaxLength(200)
                    .IsRequired();
                    
                entity.Property(e => e.Description)
                    .HasMaxLength(1000);
                    
                entity.Property(e => e.Thumbnail)
                    .HasMaxLength(2000);
                    
                entity.Property(e => e.DateCreated)
                    .IsRequired();
            });

            // Configure Topic entity
            modelBuilder.Entity<Topic>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();
                    
                // Create unique index on topic name to prevent duplicates
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure Genre entity
            modelBuilder.Entity<Genre>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .HasMaxLength(100)
                    .IsRequired();
                    
                // Create unique index on genre name to prevent duplicates
                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure many-to-many relationship between BaseMediaItem and Topic
            modelBuilder.Entity<BaseMediaItem>()
                .HasMany(m => m.Topics)
                .WithMany(t => t.MediaItems)
                .UsingEntity<Dictionary<string, object>>(
                    "MediaItemTopics",
                    j => j
                        .HasOne<Topic>()
                        .WithMany()
                        .HasForeignKey("TopicId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<BaseMediaItem>()
                        .WithMany()
                        .HasForeignKey("MediaItemId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("MediaItemId", "TopicId");
                        j.ToTable("MediaItemTopics");
                    });

            // Configure many-to-many relationship between BaseMediaItem and Genre
            modelBuilder.Entity<BaseMediaItem>()
                .HasMany(m => m.Genres)
                .WithMany(g => g.MediaItems)
                .UsingEntity<Dictionary<string, object>>(
                    "MediaItemGenres",
                    j => j
                        .HasOne<Genre>()
                        .WithMany()
                        .HasForeignKey("GenreId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<BaseMediaItem>()
                        .WithMany()
                        .HasForeignKey("MediaItemId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("MediaItemId", "GenreId");
                        j.ToTable("MediaItemGenres");
                    });

            // Configure many-to-many relationship between Mixlist and Topic
            modelBuilder.Entity<Mixlist>()
                .HasMany(m => m.Topics)
                .WithMany(t => t.Mixlists)
                .UsingEntity<Dictionary<string, object>>(
                    "MixlistTopics",
                    j => j
                        .HasOne<Topic>()
                        .WithMany()
                        .HasForeignKey("TopicId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Mixlist>()
                        .WithMany()
                        .HasForeignKey("MixlistId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("MixlistId", "TopicId");
                        j.ToTable("MixlistTopics");
                    });

            // Configure many-to-many relationship between Mixlist and Genre
            modelBuilder.Entity<Mixlist>()
                .HasMany(m => m.Genres)
                .WithMany(g => g.Mixlists)
                .UsingEntity<Dictionary<string, object>>(
                    "MixlistGenres",
                    j => j
                        .HasOne<Genre>()
                        .WithMany()
                        .HasForeignKey("GenreId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<Mixlist>()
                        .WithMany()
                        .HasForeignKey("MixlistId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("MixlistId", "GenreId");
                        j.ToTable("MixlistGenres");
                    });

            // Configure many-to-many relationship between BaseMediaItem and Mixlist
            modelBuilder.Entity<BaseMediaItem>()
                .HasMany(m => m.Mixlists)
                .WithMany(m => m.MediaItems)
                .UsingEntity<Dictionary<string, object>>(
                    "MixlistMediaItems", // Join table name
                    j => j
                        .HasOne<Mixlist>()
                        .WithMany()
                        .HasForeignKey("MixlistId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j => j
                        .HasOne<BaseMediaItem>()
                        .WithMany()
                        .HasForeignKey("MediaItemId")
                        .OnDelete(DeleteBehavior.Cascade),
                    j =>
                    {
                        j.HasKey("MixlistId", "MediaItemId");
                        j.ToTable("MixlistMediaItems");
                    });

            // Configure Table-Per-Type (TPT) inheritance for all media entities
            modelBuilder.Entity<BaseMediaItem>().ToTable("MediaItems");
            modelBuilder.Entity<PodcastSeries>().ToTable("PodcastSeries");
            modelBuilder.Entity<PodcastEpisode>().ToTable("PodcastEpisodes");
            modelBuilder.Entity<Book>().ToTable("Books");
            modelBuilder.Entity<Movie>().ToTable("Movies");
            modelBuilder.Entity<TvShow>().ToTable("TvShows");
            modelBuilder.Entity<Video>().ToTable("Videos");
            modelBuilder.Entity<YouTubeChannel>().ToTable("YouTubeChannels");
            modelBuilder.Entity<Article>().ToTable("Articles");
            modelBuilder.Entity<Website>().ToTable("Websites");
            modelBuilder.Entity<Document>().ToTable("Documents");
            modelBuilder.Entity<TvShowEpisode>().ToTable("TvShowEpisodes");

            // Configure PodcastSeries specific properties
            modelBuilder.Entity<PodcastSeries>(entity =>
            {
                entity.Property(e => e.Publisher)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.ExternalId)
                    .HasMaxLength(200);
                    
                entity.Property(e => e.IsSubscribed)
                    .HasDefaultValue(false);
                    
                entity.Property(e => e.TotalEpisodes)
                    .HasDefaultValue(0);
                    
                // Unique index on ExternalId to make ListenNotes imports idempotent.
                // Filtered so manually-added series (no ExternalId) are unaffected.
                entity.HasIndex(e => e.ExternalId)
                    .IsUnique()
                    .HasFilter("\"ExternalId\" IS NOT NULL");

                // Create index on IsSubscribed for subscription queries
                entity.HasIndex(e => e.IsSubscribed);
            });

            // Configure PodcastEpisode specific properties
            modelBuilder.Entity<PodcastEpisode>(entity =>
            {
                entity.Property(e => e.AudioLink)
                    .HasMaxLength(2000);
                    
                entity.Property(e => e.DurationInSeconds)
                    .HasDefaultValue(0);
                    
                entity.Property(e => e.ExternalId)
                    .HasMaxLength(200);
                    
                entity.Property(e => e.Publisher)
                    .HasMaxLength(500);
                    
                // Configure relationship with PodcastSeries
                entity.HasOne(e => e.Series)
                    .WithMany(s => s.Episodes)
                    .HasForeignKey(e => e.SeriesId)
                    .OnDelete(DeleteBehavior.Cascade); // Cascade delete: when series deleted, delete episodes
                    
                // Create index on SeriesId for better query performance
                entity.HasIndex(e => e.SeriesId);

                // Unique composite index on (SeriesId, ExternalId) to make episode
                // imports idempotent within a series. Filtered so manually-added
                // episodes (no ExternalId) are unaffected (NULLs treated as distinct).
                entity.HasIndex(e => new { e.SeriesId, e.ExternalId })
                    .IsUnique()
                    .HasFilter("\"ExternalId\" IS NOT NULL");

                // Create index on ReleaseDate for chronological queries
                entity.HasIndex(e => e.ReleaseDate);
            });

            // Configure TvShowEpisode specific properties
            modelBuilder.Entity<TvShowEpisode>(entity =>
            {
                entity.Property(e => e.StillPath)
                    .HasMaxLength(2000);

                // Configure relationship with TvShow
                entity.HasOne(e => e.Show)
                    .WithMany(s => s.Episodes)
                    .HasForeignKey(e => e.ShowId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes for query performance
                entity.HasIndex(e => e.ShowId);
                entity.HasIndex(e => e.TmdbEpisodeId);
                entity.HasIndex(e => e.AirDate);
            });

            // Configure TraktToken table (not a media item, standalone table)
            modelBuilder.Entity<TraktToken>(entity =>
            {
                entity.ToTable("TraktTokens");
                entity.HasKey(e => e.Id);

                entity.Property(e => e.AccessToken)
                    .IsRequired();

                entity.Property(e => e.RefreshToken)
                    .IsRequired();

                entity.Property(e => e.TraktUsername)
                    .HasMaxLength(200);
            });

            // Configure Book specific properties
            modelBuilder.Entity<Book>(entity =>
            {
                entity.Property(e => e.Author)
                    .HasMaxLength(300)
                    .IsRequired();

                entity.Property(e => e.ISBN)
                    .HasMaxLength(17);

                entity.Property(e => e.ASIN)
                    .HasMaxLength(20);

                entity.Property(e => e.Format)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Publisher)
                    .HasMaxLength(500);

                entity.Property(e => e.MyReview)
                    .HasMaxLength(10000);

                // Store GoodreadsTags as JSON array in PostgreSQL
                entity.Property(e => e.GoodreadsTags)
                    .HasColumnType("jsonb");

                // Create index on ISBN for better query performance
                entity.HasIndex(e => e.ISBN);

                // Create index on Author for better query performance
                entity.HasIndex(e => e.Author);

                // Create index on ASIN for better query performance
                entity.HasIndex(e => e.ASIN);

                // Create indexes on new Goodreads fields
                entity.HasIndex(e => e.YearPublished);
                entity.HasIndex(e => e.OriginalPublicationYear);
                entity.HasIndex(e => e.DateRead);
            });

            // Configure Movie specific properties
            modelBuilder.Entity<Movie>(entity =>
            {
                entity.Property(e => e.Director)
                    .HasMaxLength(100);
                    
                entity.Property(e => e.Cast)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.MpaaRating)
                    .HasMaxLength(50);
                    
                entity.Property(e => e.ImdbId)
                    .HasMaxLength(20);
                    
                entity.Property(e => e.TmdbId)
                    .HasMaxLength(20);
                    
                entity.Property(e => e.TmdbBackdropPath)
                    .HasMaxLength(2000);
                    
                entity.Property(e => e.Tagline)
                    .HasMaxLength(1000);
                    
                entity.Property(e => e.Homepage)
                    .HasMaxLength(2000);
                    
                entity.Property(e => e.OriginalLanguage)
                    .HasMaxLength(10);
                    
                entity.Property(e => e.OriginalTitle)
                    .HasMaxLength(500);
                
                // Create indexes for better query performance
                entity.HasIndex(e => e.ReleaseYear);
                entity.HasIndex(e => e.ImdbId);
                entity.HasIndex(e => e.TmdbId);
                entity.HasIndex(e => e.Director);
            });

            // Configure TvShow specific properties
            modelBuilder.Entity<TvShow>(entity =>
            {
                entity.Property(e => e.Creator)
                    .HasMaxLength(100);
                    
                entity.Property(e => e.Cast)
                    .HasMaxLength(500);
                    
                entity.Property(e => e.ContentRating)
                    .HasMaxLength(50);
                    
                entity.Property(e => e.TmdbId)
                    .HasMaxLength(20);
                    
                entity.Property(e => e.TmdbPosterPath)
                    .HasMaxLength(2000);
                    
                entity.Property(e => e.Tagline)
                    .HasMaxLength(1000);
                    
                entity.Property(e => e.Homepage)
                    .HasMaxLength(2000);
                    
                entity.Property(e => e.OriginalLanguage)
                    .HasMaxLength(10);
                    
                entity.Property(e => e.OriginalName)
                    .HasMaxLength(500);
                
                // Create indexes for better query performance
                entity.HasIndex(e => e.FirstAirYear);
                entity.HasIndex(e => e.LastAirYear);
                entity.HasIndex(e => e.TmdbId);
                entity.HasIndex(e => e.Creator);
            });

            // Configure Video specific properties
            modelBuilder.Entity<Video>(entity =>
            {
                entity.Property(e => e.Platform)
                    .HasMaxLength(100)
                    .IsRequired();
                    
                entity.Property(e => e.LengthInSeconds)
                    .HasDefaultValue(0);

                entity.Property(e => e.ExternalId)
                    .HasMaxLength(200);

                // Configure relationship with YouTubeChannel
                entity.HasOne(e => e.Channel)
                    .WithMany(c => c.Videos)
                    .HasForeignKey(e => e.ChannelId)
                    .OnDelete(DeleteBehavior.SetNull); // When channel deleted, videos remain but ChannelId becomes null

                // Create indexes for better query performance
                entity.HasIndex(e => e.ExternalId);
                entity.HasIndex(e => e.Platform);
                entity.HasIndex(e => e.ChannelId);
            });

            // Configure YouTubeChannel specific properties
            modelBuilder.Entity<YouTubeChannel>(entity =>
            {
                entity.Property(e => e.ChannelExternalId)
                    .HasMaxLength(100)
                    .IsRequired();
                    
                entity.Property(e => e.CustomUrl)
                    .HasMaxLength(200);
                    
                entity.Property(e => e.UploadsPlaylistId)
                    .HasMaxLength(100);
                    
                entity.Property(e => e.Country)
                    .HasMaxLength(10);
                    
                // Create unique index on ChannelExternalId to prevent duplicate channel imports
                entity.HasIndex(e => e.ChannelExternalId)
                    .IsUnique();
                    
                // Create indexes for better query performance
                entity.HasIndex(e => e.PublishedAt);
                entity.HasIndex(e => e.LastSyncedAt);
                entity.HasIndex(e => e.SubscriberCount);
            });

            // Configure YouTubePlaylist specific properties
            modelBuilder.Entity<YouTubePlaylist>(entity =>
            {
                entity.Property(e => e.PlaylistExternalId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.PrivacyStatus)
                    .HasMaxLength(50);

                // Create unique index on PlaylistExternalId to prevent duplicate playlist imports
                entity.HasIndex(e => e.PlaylistExternalId)
                    .IsUnique();

                // Create indexes for better query performance
                entity.HasIndex(e => e.PublishedAt);
                entity.HasIndex(e => e.LastSyncedAt);
            });

            // Configure YouTubePlaylistVideo junction table
            modelBuilder.Entity<YouTubePlaylistVideo>(entity =>
            {
                // Composite primary key
                entity.HasKey(pv => new { pv.YouTubePlaylistId, pv.VideoId });
                
                // Configure relationship with YouTubePlaylist
                entity.HasOne(pv => pv.YouTubePlaylist)
                    .WithMany(p => p.PlaylistVideos)
                    .HasForeignKey(pv => pv.YouTubePlaylistId)
                    .OnDelete(DeleteBehavior.Cascade); // When playlist deleted, remove all playlist-video associations
                    
                // Configure relationship with Video
                entity.HasOne(pv => pv.Video)
                    .WithMany(v => v.PlaylistVideos)
                    .HasForeignKey(pv => pv.VideoId)
                    .OnDelete(DeleteBehavior.Cascade); // When video deleted, remove all playlist-video associations
                    
                // Create indexes for better query performance
                entity.HasIndex(pv => pv.Position);
                entity.HasIndex(pv => pv.AddedAt);
            });

            // Configure Article specific properties
            modelBuilder.Entity<Article>(entity =>
            {
                entity.Property(e => e.ContentStoragePath)
                    .HasMaxLength(500);

                entity.Property(e => e.Author)
                    .HasMaxLength(200);

                entity.Property(e => e.Publication)
                    .HasMaxLength(200);

                entity.Property(e => e.IsStarred)
                    .HasDefaultValue(false);

                entity.Property(e => e.IsArchived)
                    .HasDefaultValue(false);

                entity.Property(e => e.ReadwiseDocumentId)
                    .HasMaxLength(100);

                entity.Property(e => e.ReaderLocation)
                    .HasMaxLength(50);

                // Create indexes for better query performance
                entity.HasIndex(e => e.ReadwiseDocumentId);
                entity.HasIndex(e => e.Author);
                entity.HasIndex(e => e.Publication);
                entity.HasIndex(e => e.IsStarred);
                entity.HasIndex(e => e.IsArchived);
                entity.HasIndex(e => e.PublicationDate);
                entity.HasIndex(e => e.ReaderLocation);
            });

            // Configure Website specific properties
            modelBuilder.Entity<Website>(entity =>
            {
                entity.Property(e => e.RssFeedUrl)
                    .HasMaxLength(2000);

                entity.Property(e => e.Domain)
                    .HasMaxLength(200);

                entity.Property(e => e.Author)
                    .HasMaxLength(200);

                entity.Property(e => e.Publication)
                    .HasMaxLength(200);

                // Create indexes for better query performance
                entity.HasIndex(e => e.Domain);
                entity.HasIndex(e => e.LastCheckedDate);
            });

            // Configure Document specific properties
            modelBuilder.Entity<Document>(entity =>
            {
                entity.Property(e => e.OriginalFileName)
                    .HasMaxLength(500);

                entity.Property(e => e.ArchiveSerialNumber)
                    .HasMaxLength(100);

                entity.Property(e => e.DocumentType)
                    .HasMaxLength(200);

                entity.Property(e => e.Correspondent)
                    .HasMaxLength(200);

                entity.Property(e => e.FileType)
                    .HasMaxLength(20);

                entity.Property(e => e.PaperlessUrl)
                    .HasMaxLength(2000);

                entity.Property(e => e.IsArchived)
                    .HasDefaultValue(false);

                // Create indexes for better query performance
                entity.HasIndex(e => e.PaperlessId);
                entity.HasIndex(e => e.DocumentType);
                entity.HasIndex(e => e.Correspondent);
                entity.HasIndex(e => e.DocumentDate);
                entity.HasIndex(e => e.FileType);
                entity.HasIndex(e => e.IsArchived);
            });

            // Configure Highlight entity
            modelBuilder.Entity<Highlight>(entity =>
            {
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.ReadwiseId)
                    .IsRequired(false);
                    
                entity.Property(e => e.Text)
                    .HasMaxLength(8191)
                    .IsRequired();
                    
                entity.Property(e => e.Note)
                    .HasMaxLength(8191);
                    
                entity.Property(e => e.Title)
                    .HasMaxLength(511);
                    
                entity.Property(e => e.Author)
                    .HasMaxLength(1024);
                    
                entity.Property(e => e.Category)
                    .HasMaxLength(50);
                    
                entity.Property(e => e.SourceUrl)
                    .HasMaxLength(2047);
                    
                entity.Property(e => e.ImageUrl)
                    .HasMaxLength(2047);
                    
                entity.Property(e => e.HighlightUrl)
                    .HasMaxLength(4095);
                    
                entity.Property(e => e.LocationType)
                    .HasMaxLength(50);
                    
                entity.Property(e => e.Tags)
                    .HasMaxLength(1000);
                    
                entity.Property(e => e.SourceType)
                    .HasMaxLength(64);
                    
                entity.Property(e => e.Color)
                    .HasMaxLength(50);
                    
                entity.Property(e => e.IsFavorite)
                    .HasDefaultValue(false);
                    
                entity.Property(e => e.CreatedAt)
                    .IsRequired();
                    
                // Configure relationships
                entity.HasOne(e => e.Article)
                    .WithMany(a => a.Highlights)
                    .HasForeignKey(e => e.ArticleId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                entity.HasOne(e => e.Book)
                    .WithMany(b => b.Highlights)
                    .HasForeignKey(e => e.BookId)
                    .OnDelete(DeleteBehavior.SetNull);
                    
                // Create indexes for better query performance
                entity.HasIndex(e => e.ReadwiseId)
                    .IsUnique()
                    .HasFilter("\"ReadwiseId\" IS NOT NULL");
                entity.HasIndex(e => e.ArticleId);
                entity.HasIndex(e => e.BookId);
                entity.HasIndex(e => e.ReadwiseBookId);
                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.HighlightedAt);
                entity.HasIndex(e => e.IsFavorite);
            });

            // Configure RefreshToken entity
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Token)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.UserId)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                entity.Property(e => e.ExpiresAt)
                    .IsRequired();

                entity.Property(e => e.ReplacedByToken)
                    .HasMaxLength(500);

                // Create indexes for better query performance
                entity.HasIndex(e => e.Token)
                    .IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsRevoked);
            });

            // Configure Note entity (Obsidian notes from Quartz vaults)
            modelBuilder.Entity<Note>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Slug)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(e => e.Title)
                    .HasMaxLength(500)
                    .IsRequired();

                entity.Property(e => e.VaultName)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.SourceUrl)
                    .HasMaxLength(2000);

                entity.Property(e => e.ContentHash)
                    .HasMaxLength(64);

                // Store Tags as JSON array in PostgreSQL
                entity.Property(e => e.Tags)
                    .HasColumnType("jsonb");

                entity.Property(e => e.DateImported)
                    .IsRequired();

                // Create unique composite index on VaultName + Slug
                entity.HasIndex(e => new { e.VaultName, e.Slug })
                    .IsUnique();

                // Create index on VaultName for vault filtering
                entity.HasIndex(e => e.VaultName);

                // Create index on LastSyncedAt for delta sync queries
                entity.HasIndex(e => e.LastSyncedAt);

                // Create index on DateImported for sorting
                entity.HasIndex(e => e.DateImported);

                entity.Property(e => e.IsDescriptionManual)
                    .HasDefaultValue(false);

                // Create index on IsDescriptionManual for AI processing queries
                entity.HasIndex(e => e.IsDescriptionManual);
            });

            // Configure MediaItemNote join entity
            modelBuilder.Entity<MediaItemNote>(entity =>
            {
                // Composite primary key
                entity.HasKey(e => new { e.MediaItemId, e.NoteId });

                // Configure relationship with BaseMediaItem
                entity.HasOne(e => e.MediaItem)
                    .WithMany()
                    .HasForeignKey(e => e.MediaItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure relationship with Note
                entity.HasOne(e => e.Note)
                    .WithMany(n => n.MediaItemNotes)
                    .HasForeignKey(e => e.NoteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.LinkDescription)
                    .HasMaxLength(500);

                entity.Property(e => e.LinkedAt)
                    .IsRequired();

                // Create index on LinkedAt for sorting
                entity.HasIndex(e => e.LinkedAt);
            });

            // Configure MixlistNote join entity
            modelBuilder.Entity<MixlistNote>(entity =>
            {
                // Composite primary key
                entity.HasKey(e => new { e.MixlistId, e.NoteId });

                // Configure relationship with Mixlist
                entity.HasOne(e => e.Mixlist)
                    .WithMany(m => m.MixlistNotes)
                    .HasForeignKey(e => e.MixlistId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure relationship with Note
                entity.HasOne(e => e.Note)
                    .WithMany(n => n.MixlistNotes)
                    .HasForeignKey(e => e.NoteId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.LinkDescription)
                    .HasMaxLength(500);

                entity.Property(e => e.LinkedAt)
                    .IsRequired();

                // Create index on LinkedAt for sorting
                entity.HasIndex(e => e.LinkedAt);
            });

            // Configure MediaItemRelation join entity (self-referential many-to-many for saved related items)
            modelBuilder.Entity<MediaItemRelation>(entity =>
            {
                // Composite primary key
                entity.HasKey(e => new { e.SourceMediaItemId, e.RelatedMediaItemId });

                // Configure relationship with source media item
                entity.HasOne(e => e.SourceMediaItem)
                    .WithMany(m => m.RelatedToItems)
                    .HasForeignKey(e => e.SourceMediaItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Configure relationship with related media item
                entity.HasOne(e => e.RelatedMediaItem)
                    .WithMany(m => m.RelatedFromItems)
                    .HasForeignKey(e => e.RelatedMediaItemId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(e => e.Source)
                    .HasConversion<string>()
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(e => e.Note)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .IsRequired();

                // Create indexes for better query performance
                entity.HasIndex(e => e.SourceMediaItemId);
                entity.HasIndex(e => e.RelatedMediaItemId);
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Source);
            });
        }

        // IApplicationDbContext interface method implementations
        public new void Add<TEntity>(TEntity entity) where TEntity : class
        {
            base.Add(entity);
        }

        public void AddRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            base.AddRange(entities);
        }

        public new void Update<TEntity>(TEntity entity) where TEntity : class
        {
            base.Update(entity);
        }

        public void ClearChangeTracker()
        {
            ChangeTracker.Clear();
        }

        public new void Remove<TEntity>(TEntity entity) where TEntity : class
        {
            base.Remove(entity);
        }

        public new async Task<TEntity?> FindAsync<TEntity>(params object[] keyValues) where TEntity : class
        {
            return await base.FindAsync<TEntity>(keyValues);
        }
    }
}
