using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Pgvector;

#nullable disable

namespace MyMediaVerse.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "FeatureFlags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureFlags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Genres",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Genres", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MediaItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    MediaType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Link = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    DateAdded = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DateCompleted = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Rating = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OwnershipStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RelatedNotes = table.Column<string>(type: "text", nullable: true),
                    Thumbnail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: true),
                    EmbeddingGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Mixlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Thumbnail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mixlists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    VaultName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SourceUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Tags = table.Column<List<string>>(type: "jsonb", nullable: false),
                    NoteDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DateImported = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    AiDescription = table.Column<string>(type: "text", nullable: true),
                    AiDescriptionGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDescriptionManual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: true),
                    EmbeddingGeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmbeddingModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Token = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Topics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Topics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FullTextContent = table.Column<string>(type: "text", nullable: true),
                    ContentStoragePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsStarred = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Publication = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PublicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReadingProgress = table.Column<int>(type: "integer", nullable: true),
                    WordCount = table.Column<int>(type: "integer", nullable: true),
                    ReadwiseDocumentId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ReaderLocation = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LastReaderSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SyncStatus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articles_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Books",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Author = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    ISBN = table.Column<string>(type: "character varying(17)", maxLength: 17, nullable: true),
                    ASIN = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Format = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PartOfSeries = table.Column<bool>(type: "boolean", nullable: false),
                    GoodreadsRating = table.Column<decimal>(type: "numeric", nullable: true),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: true),
                    YearPublished = table.Column<int>(type: "integer", nullable: true),
                    OriginalPublicationYear = table.Column<int>(type: "integer", nullable: true),
                    DateRead = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MyReview = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: true),
                    Publisher = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    GoodreadsTags = table.Column<List<string>>(type: "jsonb", nullable: false),
                    ReadwiseBookId = table.Column<int>(type: "integer", nullable: true),
                    LastReadwiseSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Books", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Books_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaperlessId = table.Column<int>(type: "integer", nullable: true),
                    OriginalFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ArchiveSerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DocumentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Correspondent = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    OcrContent = table.Column<string>(type: "text", nullable: true),
                    DocumentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PageCount = table.Column<int>(type: "integer", nullable: true),
                    FileType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: true),
                    PaperlessTagsCsv = table.Column<string>(type: "text", nullable: true),
                    CustomFieldsJson = table.Column<string>(type: "text", nullable: true),
                    LastPaperlessSync = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaperlessUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documents_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaItemGenres",
                columns: table => new
                {
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    GenreId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItemGenres", x => new { x.MediaItemId, x.GenreId });
                    table.ForeignKey(
                        name: "FK_MediaItemGenres_Genres_GenreId",
                        column: x => x.GenreId,
                        principalTable: "Genres",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaItemGenres_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Movies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Director = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cast = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ReleaseYear = table.Column<int>(type: "integer", nullable: true),
                    RuntimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    MpaaRating = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImdbId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TmdbId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TmdbRating = table.Column<double>(type: "double precision", nullable: true),
                    TmdbBackdropPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Tagline = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Homepage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OriginalLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    OriginalTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movies_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PodcastSeries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Publisher = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    IsSubscribed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    LastSyncDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalEpisodes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PodcastSeries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PodcastSeries_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TvShows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Creator = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Cast = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    FirstAirYear = table.Column<int>(type: "integer", nullable: true),
                    LastAirYear = table.Column<int>(type: "integer", nullable: true),
                    NumberOfSeasons = table.Column<int>(type: "integer", nullable: true),
                    NumberOfEpisodes = table.Column<int>(type: "integer", nullable: true),
                    ContentRating = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TmdbId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    TmdbRating = table.Column<double>(type: "double precision", nullable: true),
                    TmdbPosterPath = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Tagline = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Homepage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    OriginalLanguage = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    OriginalName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TvShows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TvShows_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Websites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RssFeedUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    LastCheckedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Domain = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Publication = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ArchiveUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArchiveStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    WaybackUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Websites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Websites_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YouTubeChannels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CustomUrl = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubscriberCount = table.Column<long>(type: "bigint", nullable: true),
                    VideoCount = table.Column<long>(type: "bigint", nullable: true),
                    ViewCount = table.Column<long>(type: "bigint", nullable: true),
                    UploadsPlaylistId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YouTubeChannels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YouTubeChannels_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MixlistMediaItems",
                columns: table => new
                {
                    MixlistId = table.Column<Guid>(type: "uuid", nullable: false),
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MixlistMediaItems", x => new { x.MixlistId, x.MediaItemId });
                    table.ForeignKey(
                        name: "FK_MixlistMediaItems_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MixlistMediaItems_Mixlists_MixlistId",
                        column: x => x.MixlistId,
                        principalTable: "Mixlists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaItemNotes",
                columns: table => new
                {
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    NoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    LinkedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LinkDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItemNotes", x => new { x.MediaItemId, x.NoteId });
                    table.ForeignKey(
                        name: "FK_MediaItemNotes_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaItemNotes_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MediaItemTopics",
                columns: table => new
                {
                    MediaItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaItemTopics", x => new { x.MediaItemId, x.TopicId });
                    table.ForeignKey(
                        name: "FK_MediaItemTopics_MediaItems_MediaItemId",
                        column: x => x.MediaItemId,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MediaItemTopics_Topics_TopicId",
                        column: x => x.TopicId,
                        principalTable: "Topics",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Highlights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReadwiseId = table.Column<int>(type: "integer", nullable: false),
                    Text = table.Column<string>(type: "character varying(8191)", maxLength: 8191, nullable: false),
                    Note = table.Column<string>(type: "character varying(8191)", maxLength: 8191, nullable: true),
                    Title = table.Column<string>(type: "character varying(511)", maxLength: 511, nullable: true),
                    Author = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SourceUrl = table.Column<string>(type: "character varying(2047)", maxLength: 2047, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(2047)", maxLength: 2047, nullable: true),
                    HighlightUrl = table.Column<string>(type: "character varying(4095)", maxLength: 4095, nullable: true),
                    Location = table.Column<int>(type: "integer", nullable: true),
                    LocationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    HighlightedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: true),
                    BookId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReadwiseBookId = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsFavorite = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Color = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Highlights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Highlights_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Highlights_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PodcastEpisodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesId = table.Column<Guid>(type: "uuid", nullable: false),
                    AudioLink = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ReleaseDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DurationInSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EpisodeNumber = table.Column<int>(type: "integer", nullable: true),
                    SeasonNumber = table.Column<int>(type: "integer", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Publisher = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PodcastEpisodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PodcastEpisodes_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PodcastEpisodes_PodcastSeries_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "PodcastSeries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Videos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ParentVideoId = table.Column<Guid>(type: "uuid", nullable: true),
                    Platform = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    LengthInSeconds = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ExternalId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Videos_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Videos_Videos_ParentVideoId",
                        column: x => x.ParentVideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Videos_YouTubeChannels_ChannelId",
                        column: x => x.ChannelId,
                        principalTable: "YouTubeChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "YouTubePlaylists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaylistExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ChannelExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LinkedYouTubeChannelId = table.Column<Guid>(type: "uuid", nullable: true),
                    VideoCount = table.Column<int>(type: "integer", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrivacyStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YouTubePlaylists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_YouTubePlaylists_MediaItems_Id",
                        column: x => x.Id,
                        principalTable: "MediaItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YouTubePlaylists_YouTubeChannels_LinkedYouTubeChannelId",
                        column: x => x.LinkedYouTubeChannelId,
                        principalTable: "YouTubeChannels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "YouTubePlaylistVideo",
                columns: table => new
                {
                    YouTubePlaylistId = table.Column<Guid>(type: "uuid", nullable: false),
                    VideoId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: true),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VideoPublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YouTubePlaylistVideo", x => new { x.YouTubePlaylistId, x.VideoId });
                    table.ForeignKey(
                        name: "FK_YouTubePlaylistVideo_Videos_VideoId",
                        column: x => x.VideoId,
                        principalTable: "Videos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YouTubePlaylistVideo_YouTubePlaylists_YouTubePlaylistId",
                        column: x => x.YouTubePlaylistId,
                        principalTable: "YouTubePlaylists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Author",
                table: "Articles",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_IsArchived",
                table: "Articles",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_IsStarred",
                table: "Articles",
                column: "IsStarred");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Publication",
                table: "Articles",
                column: "Publication");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_PublicationDate",
                table: "Articles",
                column: "PublicationDate");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ReaderLocation",
                table: "Articles",
                column: "ReaderLocation");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ReadwiseDocumentId",
                table: "Articles",
                column: "ReadwiseDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Books_ASIN",
                table: "Books",
                column: "ASIN");

            migrationBuilder.CreateIndex(
                name: "IX_Books_Author",
                table: "Books",
                column: "Author");

            migrationBuilder.CreateIndex(
                name: "IX_Books_DateRead",
                table: "Books",
                column: "DateRead");

            migrationBuilder.CreateIndex(
                name: "IX_Books_ISBN",
                table: "Books",
                column: "ISBN");

            migrationBuilder.CreateIndex(
                name: "IX_Books_OriginalPublicationYear",
                table: "Books",
                column: "OriginalPublicationYear");

            migrationBuilder.CreateIndex(
                name: "IX_Books_YearPublished",
                table: "Books",
                column: "YearPublished");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_Correspondent",
                table: "Documents",
                column: "Correspondent");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentDate",
                table: "Documents",
                column: "DocumentDate");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_DocumentType",
                table: "Documents",
                column: "DocumentType");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_FileType",
                table: "Documents",
                column: "FileType");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_IsArchived",
                table: "Documents",
                column: "IsArchived");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PaperlessId",
                table: "Documents",
                column: "PaperlessId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureFlags_Key",
                table: "FeatureFlags",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Genres_Name",
                table: "Genres",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_ArticleId",
                table: "Highlights",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_BookId",
                table: "Highlights",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_Category",
                table: "Highlights",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_HighlightedAt",
                table: "Highlights",
                column: "HighlightedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_IsFavorite",
                table: "Highlights",
                column: "IsFavorite");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_ReadwiseBookId",
                table: "Highlights",
                column: "ReadwiseBookId");

            migrationBuilder.CreateIndex(
                name: "IX_Highlights_ReadwiseId",
                table: "Highlights",
                column: "ReadwiseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MediaItemGenres_GenreId",
                table: "MediaItemGenres",
                column: "GenreId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItemNotes_LinkedAt",
                table: "MediaItemNotes",
                column: "LinkedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItemNotes_NoteId",
                table: "MediaItemNotes",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaItemTopics_TopicId",
                table: "MediaItemTopics",
                column: "TopicId");

            migrationBuilder.CreateIndex(
                name: "IX_MixlistMediaItems_MediaItemId",
                table: "MixlistMediaItems",
                column: "MediaItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_Director",
                table: "Movies",
                column: "Director");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ImdbId",
                table: "Movies",
                column: "ImdbId");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_ReleaseYear",
                table: "Movies",
                column: "ReleaseYear");

            migrationBuilder.CreateIndex(
                name: "IX_Movies_TmdbId",
                table: "Movies",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_DateImported",
                table: "Notes",
                column: "DateImported");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_IsDescriptionManual",
                table: "Notes",
                column: "IsDescriptionManual");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_LastSyncedAt",
                table: "Notes",
                column: "LastSyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_VaultName",
                table: "Notes",
                column: "VaultName");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_VaultName_Slug",
                table: "Notes",
                columns: new[] { "VaultName", "Slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PodcastEpisodes_ExternalId",
                table: "PodcastEpisodes",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastEpisodes_ReleaseDate",
                table: "PodcastEpisodes",
                column: "ReleaseDate");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastEpisodes_SeriesId",
                table: "PodcastEpisodes",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastSeries_ExternalId",
                table: "PodcastSeries",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_PodcastSeries_IsSubscribed",
                table: "PodcastSeries",
                column: "IsSubscribed");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_IsRevoked",
                table: "RefreshTokens",
                column: "IsRevoked");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Topics_Name",
                table: "Topics",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TvShows_Creator",
                table: "TvShows",
                column: "Creator");

            migrationBuilder.CreateIndex(
                name: "IX_TvShows_FirstAirYear",
                table: "TvShows",
                column: "FirstAirYear");

            migrationBuilder.CreateIndex(
                name: "IX_TvShows_LastAirYear",
                table: "TvShows",
                column: "LastAirYear");

            migrationBuilder.CreateIndex(
                name: "IX_TvShows_TmdbId",
                table: "TvShows",
                column: "TmdbId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ChannelId",
                table: "Videos",
                column: "ChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ExternalId",
                table: "Videos",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_ParentVideoId",
                table: "Videos",
                column: "ParentVideoId");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_Platform",
                table: "Videos",
                column: "Platform");

            migrationBuilder.CreateIndex(
                name: "IX_Videos_VideoType",
                table: "Videos",
                column: "VideoType");

            migrationBuilder.CreateIndex(
                name: "IX_Websites_Domain",
                table: "Websites",
                column: "Domain");

            migrationBuilder.CreateIndex(
                name: "IX_Websites_LastCheckedDate",
                table: "Websites",
                column: "LastCheckedDate");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeChannels_ChannelExternalId",
                table: "YouTubeChannels",
                column: "ChannelExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeChannels_LastSyncedAt",
                table: "YouTubeChannels",
                column: "LastSyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeChannels_PublishedAt",
                table: "YouTubeChannels",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubeChannels_SubscriberCount",
                table: "YouTubeChannels",
                column: "SubscriberCount");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubePlaylists_LastSyncedAt",
                table: "YouTubePlaylists",
                column: "LastSyncedAt");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubePlaylists_LinkedYouTubeChannelId",
                table: "YouTubePlaylists",
                column: "LinkedYouTubeChannelId");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubePlaylists_PlaylistExternalId",
                table: "YouTubePlaylists",
                column: "PlaylistExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YouTubePlaylists_PublishedAt",
                table: "YouTubePlaylists",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubePlaylistVideo_AddedAt",
                table: "YouTubePlaylistVideo",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubePlaylistVideo_Position",
                table: "YouTubePlaylistVideo",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_YouTubePlaylistVideo_VideoId",
                table: "YouTubePlaylistVideo",
                column: "VideoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "FeatureFlags");

            migrationBuilder.DropTable(
                name: "Highlights");

            migrationBuilder.DropTable(
                name: "MediaItemGenres");

            migrationBuilder.DropTable(
                name: "MediaItemNotes");

            migrationBuilder.DropTable(
                name: "MediaItemTopics");

            migrationBuilder.DropTable(
                name: "MixlistMediaItems");

            migrationBuilder.DropTable(
                name: "Movies");

            migrationBuilder.DropTable(
                name: "PodcastEpisodes");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "TvShows");

            migrationBuilder.DropTable(
                name: "Websites");

            migrationBuilder.DropTable(
                name: "YouTubePlaylistVideo");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "Books");

            migrationBuilder.DropTable(
                name: "Genres");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Topics");

            migrationBuilder.DropTable(
                name: "Mixlists");

            migrationBuilder.DropTable(
                name: "PodcastSeries");

            migrationBuilder.DropTable(
                name: "Videos");

            migrationBuilder.DropTable(
                name: "YouTubePlaylists");

            migrationBuilder.DropTable(
                name: "YouTubeChannels");

            migrationBuilder.DropTable(
                name: "MediaItems");
        }
    }
}
