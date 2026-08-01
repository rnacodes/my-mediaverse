using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using MyMediaVerse.IntegrationTests.Fixtures;
using Xunit;

namespace MyMediaVerse.IntegrationTests.Api
{
    /// <summary>
    /// Convention test pinning the effective authorization classification of every controller
    /// action. Any new or changed endpoint fails this test until it is deliberately classified
    /// here, which prevents endpoints from silently shipping with the wrong access level.
    ///
    /// Classifications:
    /// - "Anonymous": carries [AllowAnonymous] (wins over [Authorize] when both are present).
    /// - "Authorized": carries [Authorize] at action or controller level.
    /// - "Unclassified": carries neither attribute, so the host's fallback policy governs it:
    ///   authentication is required everywhere, except that the Demo host also permits
    ///   anonymous GET requests.
    /// </summary>
    [Collection("Database")]
    public class AuthorizationModelTests
    {
        private const string Anonymous = "Anonymous";
        private const string Authorized = "Authorized";
        private const string Unclassified = "Unclassified";

        private readonly ApiFactory _factory;

        public AuthorizationModelTests(ApiFactory factory)
        {
            _factory = factory;
        }

        private static readonly IReadOnlyDictionary<string, string> ExpectedClassifications =
            new Dictionary<string, string>
            {
                ["AI.GenerateNoteDescription"] = Authorized,
                ["AI.GenerateNoteDescriptionsBatch"] = Authorized,
                ["AI.GetPendingNoteDescriptionsCount"] = Authorized,
                ["AI.GetStatus"] = Authorized,
                ["Article.BulkFetchContent"] = Unclassified,
                ["Article.CreateArticle"] = Unclassified,
                ["Article.DeduplicateArticles"] = Unclassified,
                ["Article.DeleteArticle"] = Unclassified,
                ["Article.FetchArticleContent"] = Unclassified,
                ["Article.FindDuplicates"] = Unclassified,
                ["Article.GetAllArticles"] = Unclassified,
                ["Article.GetArchivedArticles"] = Unclassified,
                ["Article.GetArticle"] = Unclassified,
                ["Article.GetArticleContent"] = Unclassified,
                ["Article.GetArticlesByAuthor"] = Unclassified,
                ["Article.GetStarredArticles"] = Unclassified,
                ["Article.ScrapePreview"] = Unclassified,
                ["Article.SyncFromReader"] = Unclassified,
                ["Article.UpdateArticle"] = Unclassified,
                ["Article.UpdateArticleContent"] = Unclassified,
                ["Article.UpdateArticleSyncStatus"] = Unclassified,
                ["Auth.CleanupTokens"] = Authorized,
                ["Auth.Login"] = Anonymous,
                ["Auth.Logout"] = Authorized,
                ["Auth.Refresh"] = Anonymous,
                ["Auth.ValidateToken"] = Authorized,
                ["Book.CreateBook"] = Unclassified,
                ["Book.DeleteBook"] = Unclassified,
                ["Book.GetAllBooks"] = Unclassified,
                ["Book.GetBook"] = Unclassified,
                ["Book.GetBooksByAuthor"] = Unclassified,
                ["Book.GetBookSeries"] = Unclassified,
                ["Book.ImportFromGoogleBooks"] = Unclassified,
                ["Book.ImportFromOpenLibrary"] = Unclassified,
                ["Book.SearchGoogleBooks"] = Unclassified,
                ["Book.SearchOpenLibrary"] = Unclassified,
                ["Book.UpdateBook"] = Unclassified,
                ["BookEnrichment.ConvertGoodreadsRatings"] = Authorized,
                ["BookEnrichment.EnrichSingleBook"] = Authorized,
                ["BookEnrichment.GetStatus"] = Authorized,
                ["BookEnrichment.RunEnrichment"] = Authorized,
                ["BookEnrichment.RunEnrichmentAll"] = Authorized,
                ["Demo.GenerateSecret"] = Unclassified,
                ["Demo.Lock"] = Anonymous,
                ["Demo.Status"] = Anonymous,
                ["Demo.Unlock"] = Anonymous,
                ["Dev.CleanupAllGenres"] = Authorized,
                ["Dev.CleanupAllMedia"] = Authorized,
                ["Dev.CleanupAllTopics"] = Authorized,
                ["Dev.CleanupArticles"] = Authorized,
                ["Dev.CleanupBooks"] = Authorized,
                ["Dev.CleanupChannels"] = Authorized,
                ["Dev.CleanupDocuments"] = Authorized,
                ["Dev.CleanupHighlights"] = Authorized,
                ["Dev.CleanupMixlists"] = Authorized,
                ["Dev.CleanupMovies"] = Authorized,
                ["Dev.CleanupNotes"] = Authorized,
                ["Dev.CleanupOrphanedGenres"] = Authorized,
                ["Dev.CleanupOrphanedTopics"] = Authorized,
                ["Dev.CleanupPlaylists"] = Authorized,
                ["Dev.CleanupPodcasts"] = Authorized,
                ["Dev.CleanupTvShows"] = Authorized,
                ["Dev.CleanupVideos"] = Authorized,
                ["Dev.CleanupWebsites"] = Authorized,
                ["Dev.CleanupYouTubeData"] = Authorized,
                ["Dev.DiagnoseOrphanedMedia"] = Authorized,
                ["Dev.FixOrphanedMedia"] = Authorized,
                ["Dev.ResetDatabase"] = Authorized,
                ["Dev.SeedDemoData"] = Authorized,
                ["Dev.SeedDemoNotes"] = Authorized,
                ["Dev.SeedMixlists"] = Authorized,
                ["Document.CreateDocument"] = Authorized,
                ["Document.DeleteDocument"] = Authorized,
                ["Document.GetAllDocuments"] = Authorized,
                ["Document.GetArchivedDocuments"] = Authorized,
                ["Document.GetDocument"] = Authorized,
                ["Document.GetDocumentsByCorrespondent"] = Authorized,
                ["Document.GetDocumentsByDateRange"] = Authorized,
                ["Document.GetDocumentsByType"] = Authorized,
                ["Document.GetPaperlessStatus"] = Authorized,
                ["Document.SearchDocuments"] = Authorized,
                ["Document.SyncFromPaperless"] = Authorized,
                ["Document.SyncSingleDocument"] = Authorized,
                ["Document.UpdateDocument"] = Authorized,
                ["Genres.CreateGenre"] = Unclassified,
                ["Genres.DeleteGenre"] = Unclassified,
                ["Genres.GetAllGenres"] = Unclassified,
                ["Genres.GetGenre"] = Unclassified,
                ["Genres.ImportGenresFromCsv"] = Unclassified,
                ["Genres.ImportGenresFromJson"] = Unclassified,
                ["Genres.SearchGenres"] = Unclassified,
                ["Genres.UpdateGenre"] = Unclassified,
                ["Health.CorsTest"] = Authorized,
                ["Health.Get"] = Anonymous,
                ["Health.GetDetailed"] = Authorized,
                ["Highlight.BulkCreateHighlights"] = Unclassified,
                ["Highlight.CleanHighlightText"] = Unclassified,
                ["Highlight.CreateHighlight"] = Unclassified,
                ["Highlight.DeleteHighlight"] = Unclassified,
                ["Highlight.DiagnoseLinking"] = Unclassified,
                ["Highlight.ExportHighlight"] = Unclassified,
                ["Highlight.GetAllHighlights"] = Unclassified,
                ["Highlight.GetHighlight"] = Unclassified,
                ["Highlight.GetHighlightsByArticle"] = Unclassified,
                ["Highlight.GetHighlightsByBook"] = Unclassified,
                ["Highlight.GetHighlightsByTag"] = Unclassified,
                ["Highlight.GetUnlinkedHighlights"] = Unclassified,
                ["Highlight.LinkHighlightsToMedia"] = Unclassified,
                ["Highlight.SyncHighlights"] = Unclassified,
                ["Highlight.UpdateHighlight"] = Unclassified,
                ["Highlight.ValidateConnection"] = Unclassified,
                ["ListenNotes.GetBestPodcasts"] = Unclassified,
                ["ListenNotes.GetCuratedPodcast"] = Unclassified,
                ["ListenNotes.GetCuratedPodcasts"] = Unclassified,
                ["ListenNotes.GetEpisode"] = Unclassified,
                ["ListenNotes.GetEpisodeRecommendations"] = Unclassified,
                ["ListenNotes.GetGenres"] = Unclassified,
                ["ListenNotes.GetPlaylist"] = Unclassified,
                ["ListenNotes.GetPlaylists"] = Unclassified,
                ["ListenNotes.GetPodcast"] = Unclassified,
                ["ListenNotes.GetPodcastRecommendations"] = Unclassified,
                ["ListenNotes.ImportPodcast"] = Unclassified,
                ["ListenNotes.ImportPodcastEpisode"] = Unclassified,
                ["ListenNotes.Search"] = Unclassified,
                ["Media.AddMediaItem"] = Unclassified,
                ["Media.BulkDeleteMediaItems"] = Unclassified,
                ["Media.DeleteMediaItem"] = Unclassified,
                ["Media.ExportAllMedia"] = Authorized,
                ["Media.ExportMediaItem"] = Authorized,
                ["Media.GetAllMedia"] = Unclassified,
                ["Media.GetMediaByGenre"] = Unclassified,
                ["Media.GetMediaByTopic"] = Unclassified,
                ["Media.GetMediaByType"] = Unclassified,
                ["Media.GetMediaItem"] = Unclassified,
                ["Media.SearchMedia"] = Unclassified,
                ["Media.UpdateMediaItem"] = Unclassified,
                ["Mixlist.AddMediaItemToMixlist"] = Unclassified,
                ["Mixlist.CreateMixlist"] = Unclassified,
                ["Mixlist.DeleteMixlist"] = Unclassified,
                ["Mixlist.ExportAllMixlists"] = Authorized,
                ["Mixlist.ExportMixlist"] = Authorized,
                ["Mixlist.GetAllMixlists"] = Unclassified,
                ["Mixlist.GetMixlist"] = Unclassified,
                ["Mixlist.GetNotesForMixlist"] = Unclassified,
                ["Mixlist.ImportMixlists"] = Unclassified,
                ["Mixlist.LinkNoteToMixlist"] = Unclassified,
                ["Mixlist.RemoveMediaItemFromMixlist"] = Unclassified,
                ["Mixlist.SearchMixlists"] = Unclassified,
                ["Mixlist.UnlinkNoteFromMixlist"] = Unclassified,
                ["Mixlist.UpdateMixlist"] = Unclassified,
                ["Movie.CreateMovie"] = Unclassified,
                ["Movie.DeleteMovie"] = Unclassified,
                ["Movie.GetAllMovies"] = Unclassified,
                ["Movie.GetMovie"] = Unclassified,
                ["Movie.GetMoviesByDirector"] = Unclassified,
                ["Movie.GetMoviesByYear"] = Unclassified,
                ["Movie.ImportMovieFromTmdb"] = Unclassified,
                ["Movie.SearchTmdbMovies"] = Unclassified,
                ["Movie.UpdateMovie"] = Unclassified,
                ["MovieTvEnrichment.GetStatus"] = Authorized,
                ["MovieTvEnrichment.RunAllEnrichment"] = Authorized,
                ["MovieTvEnrichment.RunMovieEnrichment"] = Authorized,
                ["MovieTvEnrichment.RunTvShowEnrichment"] = Authorized,
                ["Note.Create"] = Unclassified,
                ["Note.Delete"] = Unclassified,
                ["Note.GetAll"] = Unclassified,
                ["Note.GetById"] = Unclassified,
                ["Note.GetBySlug"] = Unclassified,
                ["Note.GetMediaForNote"] = Unclassified,
                ["Note.GetNotesForMedia"] = Unclassified,
                ["Note.GetSyncStatus"] = Unclassified,
                ["Note.LinkToMedia"] = Unclassified,
                ["Note.SyncAll"] = Unclassified,
                ["Note.SyncVault"] = Unclassified,
                ["Note.UnlinkFromMedia"] = Unclassified,
                ["Note.Update"] = Unclassified,
                ["Podcast.CreatePodcastEpisode"] = Unclassified,
                ["Podcast.CreatePodcastSeries"] = Unclassified,
                ["Podcast.DeletePodcastEpisode"] = Unclassified,
                ["Podcast.DeletePodcastSeries"] = Unclassified,
                ["Podcast.GetAllPodcastEpisodes"] = Unclassified,
                ["Podcast.GetEpisodesBySeries"] = Unclassified,
                ["Podcast.GetPodcastEpisode"] = Unclassified,
                ["Podcast.GetPodcastSeries"] = Unclassified,
                ["Podcast.GetSubscribedPodcastSeries"] = Unclassified,
                ["Podcast.ImportPodcastEpisodeFromApi"] = Unclassified,
                ["Podcast.ImportPodcastSeriesByName"] = Unclassified,
                ["Podcast.ImportPodcastSeriesFromApi"] = Unclassified,
                ["Podcast.ImportPodcastsFromOpml"] = Unclassified,
                ["Podcast.SearchPodcastSeries"] = Unclassified,
                ["Podcast.SubscribeToPodcastSeries"] = Unclassified,
                ["Podcast.SyncPodcastSeriesEpisodes"] = Unclassified,
                ["Podcast.UnsubscribeFromPodcastSeries"] = Unclassified,
                ["Podcast.UpdatePodcastEpisode"] = Unclassified,
                ["Podcast.UpdatePodcastSeries"] = Unclassified,
                ["PodcastEnrichment.GetStatus"] = Authorized,
                ["PodcastEnrichment.RunEnrichment"] = Authorized,
                ["PodcastEnrichment.RunEnrichmentAll"] = Authorized,
                ["Readwise.FetchArticleContent"] = Unclassified,
                ["Readwise.FetchByDocumentId"] = Unclassified,
                ["Readwise.GetArticlesWithDocumentIds"] = Unclassified,
                ["Readwise.GetDocumentsFromReaderApi"] = Unclassified,
                ["Readwise.ImportByLocation"] = Unclassified,
                ["Readwise.SyncAll"] = Unclassified,
                ["Readwise.TestFetchDocument"] = Unclassified,
                ["Readwise.ValidateConnection"] = Unclassified,
                ["Recommendation.GetMediaRelatedToNote"] = Unclassified,
                ["Recommendation.GetNotesRelatedToMedia"] = Unclassified,
                ["Recommendation.GetPersonalizedRecommendations"] = Authorized,
                ["Recommendation.GetSimilarMedia"] = Unclassified,
                ["Recommendation.GetSimilarNotes"] = Unclassified,
                ["Recommendation.GetStatus"] = Unclassified,
                ["Recommendation.SearchByVibe"] = Authorized,
                ["RelatedMedia.GetRelatedMedia"] = Unclassified,
                ["RelatedMedia.RemoveRelatedMedia"] = Unclassified,
                ["RelatedMedia.SaveRelatedMedia"] = Unclassified,
                ["RelatedMedia.SaveRelatedMediaBatch"] = Unclassified,
                ["Search.Health"] = Unclassified,
                ["Search.MultiSearch"] = Unclassified,
                ["Search.ReindexAll"] = Authorized,
                ["Search.ReindexAllMixlists"] = Authorized,
                ["Search.ReindexAllNotes"] = Authorized,
                ["Search.ReindexHighlight"] = Authorized,
                ["Search.ReindexHighlights"] = Authorized,
                ["Search.ReindexMediaItem"] = Authorized,
                ["Search.ReindexMixlist"] = Authorized,
                ["Search.ReindexNote"] = Authorized,
                ["Search.ResetHighlightsCollection"] = Authorized,
                ["Search.ResetMediaItemsCollection"] = Authorized,
                ["Search.ResetMixlistsCollection"] = Authorized,
                ["Search.ResetNotesCollection"] = Authorized,
                ["Search.Search"] = Unclassified,
                ["Search.SearchByType"] = Unclassified,
                ["Search.SearchByVibe"] = Authorized,
                ["Search.SearchHighlights"] = Unclassified,
                ["Search.SearchMixlists"] = Unclassified,
                ["Search.SearchNotes"] = Unclassified,
                ["Search.SearchNotesByVault"] = Unclassified,
                ["Search.SemanticSearchMedia"] = Authorized,
                ["Search.SemanticSearchNotes"] = Authorized,
                ["Tmdb.GetImageUrl"] = Unclassified,
                ["Tmdb.GetMovieDetails"] = Unclassified,
                ["Tmdb.GetMovieGenres"] = Unclassified,
                ["Tmdb.GetPopularMovies"] = Unclassified,
                ["Tmdb.GetPopularTvShows"] = Unclassified,
                ["Tmdb.GetTvGenres"] = Unclassified,
                ["Tmdb.GetTvShowDetails"] = Unclassified,
                ["Tmdb.ImportMovie"] = Unclassified,
                ["Tmdb.ImportTvShow"] = Unclassified,
                ["Tmdb.SearchMovies"] = Unclassified,
                ["Tmdb.SearchMulti"] = Unclassified,
                ["Tmdb.SearchTvShows"] = Unclassified,
                ["Topics.CreateTopic"] = Unclassified,
                ["Topics.DeleteTopic"] = Unclassified,
                ["Topics.GetAllTopics"] = Unclassified,
                ["Topics.GetTopic"] = Unclassified,
                ["Topics.ImportTopicsFromCsv"] = Unclassified,
                ["Topics.ImportTopicsFromJson"] = Unclassified,
                ["Topics.SearchTopics"] = Unclassified,
                ["Topics.UpdateTopic"] = Unclassified,
                ["Trakt.Disconnect"] = Unclassified,
                ["Trakt.GetStatus"] = Unclassified,
                ["Trakt.PollDeviceToken"] = Unclassified,
                ["Trakt.StartDeviceAuth"] = Unclassified,
                ["Trakt.SyncAll"] = Unclassified,
                ["Trakt.SyncRatings"] = Unclassified,
                ["Trakt.SyncWatched"] = Unclassified,
                ["Trakt.SyncWatchlist"] = Unclassified,
                ["TvShow.CreateTvShow"] = Unclassified,
                ["TvShow.CreateTvShowEpisode"] = Unclassified,
                ["TvShow.DeleteTvShow"] = Unclassified,
                ["TvShow.DeleteTvShowEpisode"] = Unclassified,
                ["TvShow.GetAllTvShows"] = Unclassified,
                ["TvShow.GetEpisodesByShowId"] = Unclassified,
                ["TvShow.GetTvShow"] = Unclassified,
                ["TvShow.GetTvShowEpisode"] = Unclassified,
                ["TvShow.GetTvShowsByCreator"] = Unclassified,
                ["TvShow.GetTvShowsByYear"] = Unclassified,
                ["TvShow.ImportTvShowFromTmdb"] = Unclassified,
                ["TvShow.SearchTmdbTvShows"] = Unclassified,
                ["TvShow.UpdateTvShow"] = Unclassified,
                ["Upload.CheckSpacesStatus"] = Authorized,
                ["Upload.DeleteThumbnail"] = Authorized,
                ["Upload.UploadCsv"] = Authorized,
                ["Upload.UploadGoodreadsCsv"] = Authorized,
                ["Upload.UploadThumbnail"] = Authorized,
                ["Upload.UploadThumbnailFromUrl"] = Authorized,
                ["Video.CreateVideo"] = Unclassified,
                ["Video.DeleteVideo"] = Unclassified,
                ["Video.GetAllVideos"] = Unclassified,
                ["Video.GetPlaylistsForVideo"] = Unclassified,
                ["Video.GetVideo"] = Unclassified,
                ["Video.GetVideosByChannel"] = Unclassified,
                ["Video.UpdateVideo"] = Unclassified,
                ["Website.CreateWebsite"] = Unclassified,
                ["Website.DeleteWebsite"] = Unclassified,
                ["Website.GetAllWebsites"] = Unclassified,
                ["Website.GetRssFeedItems"] = Unclassified,
                ["Website.GetWebsite"] = Unclassified,
                ["Website.GetWebsitesByDomain"] = Unclassified,
                ["Website.GetWebsitesWithRss"] = Unclassified,
                ["Website.ImportWebsite"] = Unclassified,
                ["Website.ScrapePreview"] = Unclassified,
                ["Website.UpdateWebsite"] = Unclassified,
                ["YouTube.GetAllPlaylistItems"] = Unclassified,
                ["YouTube.GetChannelByUsername"] = Unclassified,
                ["YouTube.GetChannelDetails"] = Unclassified,
                ["YouTube.GetChannelUploads"] = Unclassified,
                ["YouTube.GetPlaylistDetails"] = Unclassified,
                ["YouTube.GetPlaylistItems"] = Unclassified,
                ["YouTube.GetVideoDetails"] = Unclassified,
                ["YouTube.GetVideos"] = Unclassified,
                ["YouTube.ImportFromUrl"] = Unclassified,
                ["YouTube.ImportVideo"] = Unclassified,
                ["YouTube.Search"] = Unclassified,
                ["YouTubeChannel.CheckChannelExists"] = Unclassified,
                ["YouTubeChannel.CreateChannel"] = Unclassified,
                ["YouTubeChannel.DeleteChannel"] = Unclassified,
                ["YouTubeChannel.GetAllChannels"] = Unclassified,
                ["YouTubeChannel.GetChannel"] = Unclassified,
                ["YouTubeChannel.GetChannelByExternalId"] = Unclassified,
                ["YouTubeChannel.GetChannelVideos"] = Unclassified,
                ["YouTubeChannel.ImportChannel"] = Unclassified,
                ["YouTubeChannel.SyncChannelMetadata"] = Unclassified,
                ["YouTubeChannel.UpdateChannel"] = Unclassified,
                ["YouTubePlaylist.AddVideoToPlaylist"] = Unclassified,
                ["YouTubePlaylist.DeletePlaylist"] = Unclassified,
                ["YouTubePlaylist.GetAllPlaylists"] = Unclassified,
                ["YouTubePlaylist.GetPlaylist"] = Unclassified,
                ["YouTubePlaylist.GetPlaylistByExternalId"] = Unclassified,
                ["YouTubePlaylist.GetPlaylistVideos"] = Unclassified,
                ["YouTubePlaylist.ImportPlaylist"] = Unclassified,
                ["YouTubePlaylist.RemoveVideoFromPlaylist"] = Unclassified,
                ["YouTubePlaylist.SyncPlaylist"] = Unclassified,
            };

        [Fact]
        public void EveryControllerAction_HasAnExplicitAuthorizationClassification()
        {
            var provider = _factory.Services.GetRequiredService<IActionDescriptorCollectionProvider>();

            var actual = provider.ActionDescriptors.Items
                .OfType<ControllerActionDescriptor>()
                .GroupBy(action => $"{action.ControllerName}.{action.ActionName}")
                .ToDictionary(
                    group => group.Key,
                    group => string.Join("|", group.Select(Classify).Distinct().OrderBy(c => c)));

            var missing = actual.Keys.Except(ExpectedClassifications.Keys).OrderBy(k => k).ToList();
            var stale = ExpectedClassifications.Keys.Except(actual.Keys).OrderBy(k => k).ToList();
            var mismatched = actual
                .Where(kvp => ExpectedClassifications.TryGetValue(kvp.Key, out var expected) && expected != kvp.Value)
                .OrderBy(kvp => kvp.Key)
                .ToList();

            if (missing.Count == 0 && stale.Count == 0 && mismatched.Count == 0)
            {
                return;
            }

            var message = new StringBuilder();
            message.AppendLine("The authorization classification table is out of date.");
            message.AppendLine("Review each change deliberately, then update ExpectedClassifications.");

            if (missing.Count > 0)
            {
                message.AppendLine();
                message.AppendLine($"Unlisted actions ({missing.Count}) â€” add these entries:");
                foreach (var key in missing)
                {
                    message.AppendLine($"                [\"{key}\"] = {ToConstantName(actual[key])},");
                }
            }

            if (stale.Count > 0)
            {
                message.AppendLine();
                message.AppendLine($"Stale entries ({stale.Count}) â€” these actions no longer exist:");
                foreach (var key in stale)
                {
                    message.AppendLine($"    {key}");
                }
            }

            if (mismatched.Count > 0)
            {
                message.AppendLine();
                message.AppendLine($"Mismatched classifications ({mismatched.Count}):");
                foreach (var (key, actualValue) in mismatched)
                {
                    message.AppendLine($"    {key}: expected {ExpectedClassifications[key]}, actual {actualValue}");
                }
            }

            Assert.Fail(message.ToString());
        }

        private static string Classify(ControllerActionDescriptor action)
        {
            if (action.EndpointMetadata.OfType<IAllowAnonymous>().Any())
            {
                return Anonymous;
            }

            if (action.EndpointMetadata.OfType<IAuthorizeData>().Any())
            {
                return Authorized;
            }

            return Unclassified;
        }

        private static string ToConstantName(string classification) => classification switch
        {
            Anonymous => nameof(Anonymous),
            Authorized => nameof(Authorized),
            Unclassified => nameof(Unclassified),
            _ => $"\"{classification}\""
        };
    }
}
