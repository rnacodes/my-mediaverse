using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Domain.Enums;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.DTOs.TMDB;

namespace MyMediaVerse.UnitTests.TestData
{
    public static class TestDataFactory
    {
        public static Book CreateBook(string? title = null, string? author = null)
        {
            return new Book
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test Book",
                Author = author ?? "Test Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>(),
                Highlights = new List<Highlight>()
            };
        }

        public static Mixlist CreateMixlist(string? name = null)
        {
            return new Mixlist
            {
                Id = Guid.NewGuid(),
                Name = name ?? "Test Mixlist",
                Description = "Test mixlist description",
                DateCreated = DateTime.UtcNow,
                MediaItems = new List<BaseMediaItem>()
            };
        }

        public static Topic CreateTopic(string? name = null)
        {
            return new Topic
            {
                Id = Guid.NewGuid(),
                Name = name ?? "test topic",  // lowercase per project standards
                MediaItems = new List<BaseMediaItem>()
            };
        }

        public static Genre CreateGenre(string? name = null)
        {
            return new Genre
            {
                Id = Guid.NewGuid(),
                Name = name ?? "test genre",  // lowercase per project standards
                MediaItems = new List<BaseMediaItem>()
            };
        }

        public static Website CreateWebsite(string? title = null, string? url = null, string? domain = null)
        {
            return new Website
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test Website",
                Link = url ?? "https://example.com",
                Domain = domain ?? "example.com",
                MediaType = MediaType.Website,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                LastCheckedDate = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>()
            };
        }

        public static List<Website> CreateWebsites(int count)
        {
            var websites = new List<Website>();
            for (int i = 0; i < count; i++)
            {
                websites.Add(CreateWebsite($"Test Website {i + 1}", $"https://example{i + 1}.com", $"example{i + 1}.com"));
            }
            return websites;
        }

        public static CreateBookDto CreateBookDto(string? title = null, string? author = null)
        {
            return new CreateBookDto
            {
                Title = title ?? "Test Book",
                Author = author ?? "Test Author",
                MediaType = MediaType.Book,
                Status = Status.Uncharted,
                Format = BookFormat.Digital,
                PartOfSeries = false,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };
        }

        public static CreateMixlistDto CreateMixlistDto(string? name = null)
        {
            return new CreateMixlistDto
            {
                Name = name ?? "Test Mixlist",
                Description = "Test mixlist description"
            };
        }

        public static CreateTopicDto CreateTopicDto(string? name = null)
        {
            return new CreateTopicDto
            {
                Name = name ?? "test topic"  // lowercase per project standards
            };
        }

        public static CreateGenreDto CreateGenreDto(string? name = null)
        {
            return new CreateGenreDto
            {
                Name = name ?? "test genre"  // lowercase per project standards
            };
        }

        public static CreateWebsiteDto CreateWebsiteDto(string? title = null, string? url = null)
        {
            return new CreateWebsiteDto
            {
                Title = title ?? "Test Website",
                Url = url ?? "https://example.com",
                Description = "Test website description",
                Topics = new List<string>(),
                Genres = new List<string>()
            };
        }

        public static List<Book> CreateBooks(int count)
        {
            var books = new List<Book>();
            for (int i = 0; i < count; i++)
            {
                books.Add(CreateBook($"Test Book {i + 1}", $"Test Author {i + 1}"));
            }
            return books;
        }

        public static List<Mixlist> CreateMixlists(int count)
        {
            var mixlists = new List<Mixlist>();
            for (int i = 0; i < count; i++)
            {
                mixlists.Add(CreateMixlist($"Test Mixlist {i + 1}"));
            }
            return mixlists;
        }

        // TMDB Test Data Factory Methods
        public static TmdbMovieDto CreateTmdbMovieDto(int id = 27205, string title = "Inception")
        {
            return new TmdbMovieDto
            {
                Id = id,
                Title = title,
                Overview = "A skilled thief who commits corporate espionage by infiltrating the subconscious of his targets is offered a chance to regain his old life.",
                PosterPath = "/9gk7adHYeDvHkCSEqAvQNLV5Uge.jpg",
                BackdropPath = "/s3TBrRGB1iav7gFOCNx3H31MoES.jpg",
                ReleaseDate = "2010-07-16",
                VoteAverage = 8.4,
                Popularity = 85.123,
                OriginalLanguage = "en",
                OriginalTitle = title,
                GenreIds = new[] { 28, 878, 53 },
                Runtime = 148,
                Tagline = "Your mind is the scene of the crime.",
                Homepage = "https://www.warnerbros.com/movies/inception",
                ImdbId = "tt1375666",
                ProductionCompanies = new[]
                {
                    new TmdbProductionCompanyDto { Id = 923, Name = "Legendary Entertainment", LogoPath = "/8M99Dkt23MjQMTTWukq4m5XsEuo.png" }
                },
                ProductionCountries = new[]
                {
                    new TmdbProductionCountryDto { Iso31661 = "US", Name = "United States of America" }
                },
                SpokenLanguages = new[]
                {
                    new TmdbSpokenLanguageDto { EnglishName = "English", Iso6391 = "en", Name = "English" }
                }
            };
        }

        public static TmdbTvShowDto CreateTmdbTvShowDto(int id = 1399, string name = "Game of Thrones")
        {
            return new TmdbTvShowDto
            {
                Id = id,
                Name = name,
                Overview = "Seven noble families fight for control of the mythical land of Westeros.",
                PosterPath = "/u3bZgnGQ9T01sWNhyveQz0wH0Hl.jpg",
                BackdropPath = "/suopoADq0k8YZr4dQXcU6pToj6s.jpg",
                FirstAirDate = "2011-04-17",
                LastAirDate = "2019-05-19",
                VoteAverage = 8.4,
                Popularity = 369.594,
                OriginalLanguage = "en",
                OriginalName = name,
                GenreIds = new[] { 18, 10759, 10765 },
                NumberOfSeasons = 8,
                NumberOfEpisodes = 73,
                Homepage = "http://www.hbo.com/game-of-thrones",
                OriginCountry = new[] { "US" },
                Tagline = "Winter Is Coming",
                Networks = new[]
                {
                    new TmdbNetworkDto { Id = 49, Name = "HBO", LogoPath = "/tuomPhY2UuLiCOFvxycrJYSHZL.png", OriginCountry = "US" }
                },
                ProductionCompanies = new[]
                {
                    new TmdbProductionCompanyDto { Id = 76043, Name = "Revolution Sun Studios", LogoPath = null }
                },
                ProductionCountries = new[]
                {
                    new TmdbProductionCountryDto { Iso31661 = "US", Name = "United States of America" }
                },
                SpokenLanguages = new[]
                {
                    new TmdbSpokenLanguageDto { EnglishName = "English", Iso6391 = "en", Name = "English" }
                }
            };
        }

        public static TmdbMovieSearchResultDto CreateTmdbMovieSearchResultDto(params TmdbMovieDto[] movies)
        {
            return new TmdbMovieSearchResultDto
            {
                Page = 1,
                Results = movies,
                TotalPages = 1,
                TotalResults = movies.Length
            };
        }

        public static TmdbTvSearchResultDto CreateTmdbTvSearchResultDto(params TmdbTvShowDto[] tvShows)
        {
            return new TmdbTvSearchResultDto
            {
                Page = 1,
                Results = tvShows,
                TotalPages = 1,
                TotalResults = tvShows.Length
            };
        }

        public static Movie CreateMovie(string? title = null, int? releaseYear = null, string? tmdbId = null)
        {
            return new Movie
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test Movie",
                ReleaseYear = releaseYear ?? 2020,
                TmdbId = tmdbId ?? "12345",
                MediaType = MediaType.Movie,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>()
            };
        }

        public static TvShow CreateTvShow(string? title = null, int? firstAirYear = null, string? tmdbId = null)
        {
            return new TvShow
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test TV Show",
                FirstAirYear = firstAirYear ?? 2020,
                TmdbId = tmdbId ?? "12345",
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>()
            };
        }

        public static CreateMovieDto CreateMovieDto(string? title = null, int? releaseYear = null)
        {
            return new CreateMovieDto
            {
                Title = title ?? "Test Movie",
                ReleaseYear = releaseYear ?? 2020,
                MediaType = MediaType.Movie,
                Status = Status.Uncharted,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };
        }

        public static CreateTvShowDto CreateTvShowDto(string? title = null, int? firstAirYear = null)
        {
            return new CreateTvShowDto
            {
                Title = title ?? "Test TV Show",
                FirstAirYear = firstAirYear ?? 2020,
                MediaType = MediaType.TVShow,
                Status = Status.Uncharted,
                Topics = Array.Empty<string>(),
                Genres = Array.Empty<string>()
            };
        }

        // Document Test Data Factory Methods
        public static Document CreateDocument(
            string? title = null,
            int? paperlessId = null,
            string? documentType = null,
            string? correspondent = null)
        {
            return new Document
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test Document",
                PaperlessId = paperlessId,
                DocumentType = documentType ?? "Invoice",
                Correspondent = correspondent ?? "Test Correspondent",
                OriginalFileName = "test-document.pdf",
                FileType = "pdf",
                PageCount = 1,
                FileSizeBytes = 1024,
                IsArchived = false,
                MediaType = MediaType.Document,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                DocumentDate = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>()
            };
        }

        public static List<Document> CreateDocuments(int count)
        {
            var documents = new List<Document>();
            for (int i = 0; i < count; i++)
            {
                documents.Add(CreateDocument(
                    $"Test Document {i + 1}",
                    paperlessId: i + 1,
                    documentType: i % 2 == 0 ? "Invoice" : "Receipt",
                    correspondent: $"Correspondent {i + 1}"));
            }
            return documents;
        }

        // Article Test Data Factory Methods
        public static Article CreateArticle(string? title = null, string? author = null)
        {
            return new Article
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test Article",
                Author = author ?? "Test Author",
                MediaType = MediaType.Article,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                SyncStatus = SyncStatus.LocalOnly,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>(),
                Highlights = new List<Highlight>()
            };
        }

        public static List<Article> CreateArticles(int count)
        {
            var articles = new List<Article>();
            for (int i = 0; i < count; i++)
            {
                articles.Add(CreateArticle($"Test Article {i + 1}", $"Test Author {i + 1}"));
            }
            return articles;
        }

        // Video Test Data Factory Methods
        public static Video CreateVideo(string? title = null, string? platform = null, VideoType? videoType = null)
        {
            return new Video
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test Video",
                Platform = platform ?? "YouTube",
                VideoType = videoType ?? VideoType.Episode,
                MediaType = MediaType.Video,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>(),
                Episodes = new List<Video>(),
                PlaylistVideos = new List<YouTubePlaylistVideo>()
            };
        }

        // YouTube Channel Test Data Factory Methods
        public static YouTubeChannel CreateYouTubeChannel(string? title = null, string? channelExternalId = null)
        {
            return new YouTubeChannel
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test YouTube Channel",
                ChannelExternalId = channelExternalId ?? "UC_test123",
                MediaType = MediaType.Channel,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>(),
                Videos = new List<Video>()
            };
        }

        // YouTube Playlist Test Data Factory Methods
        public static YouTubePlaylist CreateYouTubePlaylist(string? title = null, string? playlistExternalId = null)
        {
            return new YouTubePlaylist
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test YouTube Playlist",
                PlaylistExternalId = playlistExternalId ?? "PL_test123",
                MediaType = MediaType.Playlist,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>(),
                PlaylistVideos = new List<YouTubePlaylistVideo>()
            };
        }

        // PodcastSeries (new entity) Test Data Factory Methods
        public static PodcastSeries CreateNewPodcastSeries(string? title = null, string? publisher = null)
        {
            return new PodcastSeries
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test Podcast Series",
                Publisher = publisher ?? "Test Publisher",
                MediaType = MediaType.Podcast,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                TotalEpisodes = 0,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>(),
                Episodes = new List<PodcastEpisode>()
            };
        }

        // PodcastEpisode (new entity) Test Data Factory Methods
        public static PodcastEpisode CreateNewPodcastEpisode(string? title = null, Guid? seriesId = null)
        {
            return new PodcastEpisode
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test Podcast Episode",
                SeriesId = seriesId ?? Guid.NewGuid(),
                MediaType = MediaType.Podcast,
                Status = Status.Uncharted,
                DateAdded = DateTime.UtcNow,
                Topics = new List<Topic>(),
                Genres = new List<Genre>(),
                Mixlists = new List<Mixlist>()
            };
        }

        // Note Test Data Factory Methods
        public static Note CreateNote(string? title = null, string? slug = null, string? vaultName = null)
        {
            return new Note
            {
                Id = Guid.NewGuid(),
                Title = title ?? "Test Note",
                Slug = slug ?? "test-note",
                VaultName = vaultName ?? "general",
                DateImported = DateTime.UtcNow,
                Tags = new List<string>(),
                MediaItemNotes = new List<MediaItemNote>()
            };
        }

        // Highlight Test Data Factory Methods
        public static Highlight CreateHighlight(string? text = null, int? readwiseId = null)
        {
            return new Highlight
            {
                Id = Guid.NewGuid(),
                Text = text ?? "Test highlight text",
                ReadwiseId = readwiseId ?? 1,
                CreatedAt = DateTime.UtcNow
            };
        }

        // RefreshToken Test Data Factory Methods
        public static RefreshToken CreateRefreshToken(string? userId = null, bool isActive = true)
        {
            return new RefreshToken
            {
                Token = Guid.NewGuid().ToString("N"),
                UserId = userId ?? "admin",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = isActive ? DateTime.UtcNow.AddDays(7) : DateTime.UtcNow.AddDays(-1),
                IsRevoked = false
            };
        }

        // MediaItemRelation Test Data Factory Methods
        public static MediaItemRelation CreateMediaItemRelation(
            Guid? sourceId = null,
            Guid? relatedId = null,
            RelationSource? source = null)
        {
            return new MediaItemRelation
            {
                SourceMediaItemId = sourceId ?? Guid.NewGuid(),
                RelatedMediaItemId = relatedId ?? Guid.NewGuid(),
                Source = source ?? RelationSource.ManuallyAdded,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}

