using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Constants;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Imports a purchased Listen Notes dataset export, the one stored use of Listen Notes data.
    /// Like the OPML import it lands rows fast with no external calls: shows become unsubscribed
    /// stubs marked <see cref="PodcastMetadataSources.ListenNotesDataset"/>, and podcast enrichment
    /// later finds each show's feed (by feed URL, Apple id, or title + publisher) and lets the feed
    /// replace the dataset details.
    ///
    /// The file is read by header name, so any subset of the export's columns works. A header with
    /// <c>audio</c> and <c>podcast_title</c> is an episodes export; anything else is a podcasts export.
    /// Duplicates, including repeats inside the same file, are caught by the podcast service's own
    /// identity check. A row with no feed URL or id is also skipped when a show with the same title
    /// is already stored: once enrichment has swapped in the feed's publisher, the service's
    /// title + publisher match would miss it and a re-upload would add the show twice.
    /// </summary>
    public class PodcastDatasetImportService : IPodcastDatasetImportService
    {
        private const int TitleMaxLength = 500;
        private const int PublisherMaxLength = 500;
        private const int UrlMaxLength = 2000;
        private const int ExternalIdMaxLength = 200;
        private const int LanguageMaxLength = 20;

        private readonly IPodcastService _podcastService;
        private readonly ILogger<PodcastDatasetImportService> _logger;

        public PodcastDatasetImportService(
            IPodcastService podcastService,
            ILogger<PodcastDatasetImportService> logger)
        {
            _podcastService = podcastService;
            _logger = logger;
        }

        public async Task<ChangedItemsResult<PodcastDatasetImportResultDto>> ImportFromCsvAsync(Stream csvStream)
        {
            var createdIds = new List<Guid>();
            var report = await ImportCoreAsync(csvStream, createdIds);
            return new ChangedItemsResult<PodcastDatasetImportResultDto>(report, createdIds);
        }

        private async Task<PodcastDatasetImportResultDto> ImportCoreAsync(Stream csvStream, List<Guid> createdIds)
        {
            var result = new PodcastDatasetImportResultDto { StartedAt = DateTime.UtcNow };

            List<Dictionary<string, string>> rows;
            HashSet<string> header;
            try
            {
                (header, rows) = ReadRows(csvStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read podcast dataset CSV");
                return Fail(result, "The file could not be read as a CSV.");
            }

            if (!header.Contains("title"))
            {
                return Fail(result, "The file has no \"title\" column, so it is not a Listen Notes export.");
            }

            var isEpisodes = header.Contains("audio") && header.Contains("podcast_title");
            result.FileKind = isEpisodes ? PodcastDatasetImportResultDto.EpisodesFile : PodcastDatasetImportResultDto.PodcastsFile;
            result.TotalProcessed = rows.Count;
            _logger.LogInformation("Processing {Count} rows from a Listen Notes {Kind} export", rows.Count, result.FileKind);

            var knownTitles = new HashSet<string>(StringComparer.Ordinal);
            if (!isEpisodes)
            {
                foreach (var series in await _podcastService.GetAllPodcastSeriesAsync())
                {
                    knownTitles.Add(NormalizeTitle(series.Title));
                }
            }

            foreach (var row in rows)
            {
                var title = Clip(Get(row, "title"), TitleMaxLength);
                if (title == null)
                {
                    result.SkippedCount++;
                    continue;
                }

                try
                {
                    var created = isEpisodes
                        ? await ImportEpisodeRowAsync(row, title, result, createdIds)
                        : await ImportSeriesRowAsync(row, title, knownTitles, result, createdIds);

                    if (created) result.CreatedCount++;
                    else result.SkippedCount++;
                }
                catch (Exception ex)
                {
                    // One bad row never aborts the file. The reason stays generic; the detail is logged.
                    var reason = ex is ArgumentException ? "Its feed URL is not valid." : "Could not be imported.";
                    result.FailedCount++;
                    result.Failures.Add(new PodcastDatasetImportFailureDto { Title = title, Reason = reason });
                    result.Errors.Add($"{title}: {reason}");
                    _logger.LogError(ex, "Error importing podcast dataset row: {Title}", title);
                }
            }

            if (result.FailedCount > 0)
            {
                result.WarningMessage = $"{result.FailedCount} of {result.TotalProcessed} rows could not be imported.";
            }

            result.CompletedAt = DateTime.UtcNow;
            _logger.LogInformation(
                "Podcast dataset import complete: {Created} created ({Series} shows, {Episodes} episodes), {Skipped} skipped, {Failed} failed",
                result.CreatedCount, result.SeriesCreatedCount, result.EpisodesCreatedCount, result.SkippedCount, result.FailedCount);

            return result;
        }

        private async Task<bool> ImportSeriesRowAsync(
            Dictionary<string, string> row, string title, HashSet<string> knownTitles,
            PodcastDatasetImportResultDto result, List<Guid> createdIds)
        {
            var titleOnly = Get(row, "rss") == null && Get(row, "itunes_id") == null && Get(row, "listennotes_id") == null;
            if (titleOnly && knownTitles.Contains(NormalizeTitle(title)))
            {
                return false;
            }

            var dto = new CreatePodcastSeriesDto
            {
                Title = title,
                Status = Status.Uncharted,
                IsSubscribed = false,
                Publisher = Clip(Get(row, "publisher"), PublisherMaxLength),
                Description = Get(row, "description"),
                Link = Url(Get(row, "website")),
                Thumbnail = Url(Get(row, "artwork_image")) ?? Url(Get(row, "artwork_thumbnail")),
                RssFeedUrl = Url(Get(row, "rss")),
                ApplePodcastsId = Get(row, "itunes_id"),
                ExternalId = Clip(Get(row, "listennotes_id"), ExternalIdMaxLength),
                Language = Clip(Get(row, "language"), LanguageMaxLength),
                Genres = SplitGenres(Get(row, "genres"))
            };

            var creation = await _podcastService.CreatePodcastSeriesAsync(dto, PodcastMetadataSources.ListenNotesDataset);
            knownTitles.Add(NormalizeTitle(title));
            if (!creation.Created) return false;

            result.SeriesCreatedCount++;
            createdIds.Add(creation.Series.Id);
            return true;
        }

        private async Task<bool> ImportEpisodeRowAsync(
            Dictionary<string, string> row, string title, PodcastDatasetImportResultDto result, List<Guid> createdIds)
        {
            var seriesTitle = Clip(Get(row, "podcast_title"), TitleMaxLength);
            if (seriesTitle == null)
            {
                throw new InvalidOperationException("Episode row has no podcast_title.");
            }

            var publisher = Clip(Get(row, "publisher"), PublisherMaxLength);
            var series = await _podcastService.CreatePodcastSeriesAsync(new CreatePodcastSeriesDto
            {
                Title = seriesTitle,
                Status = Status.Uncharted,
                IsSubscribed = false,
                Publisher = publisher,
                Link = Url(Get(row, "website")),
                Thumbnail = Url(Get(row, "podcast_artwork_thumbnail")),
                RssFeedUrl = Url(Get(row, "rss")),
                ApplePodcastsId = Get(row, "itunes_id"),
                ExternalId = Clip(Get(row, "listennotes_podcast_id"), ExternalIdMaxLength)
            }, PodcastMetadataSources.ListenNotesDataset);

            if (series.Created)
            {
                result.SeriesCreatedCount++;
                createdIds.Add(series.Series.Id);
            }

            var episode = await _podcastService.CreatePodcastEpisodeAsync(new CreatePodcastEpisodeDto
            {
                Title = title,
                SeriesId = series.Series.Id,
                Status = Status.Uncharted,
                Description = Get(row, "description"),
                AudioLink = Url(Get(row, "audio")),
                ReleaseDate = ParseDate(Get(row, "pub_date")),
                DurationInSeconds = int.TryParse(Get(row, "audio_length_seconds"), out var seconds) && seconds > 0 ? seconds : 0,
                Link = Url(Get(row, "link")),
                Thumbnail = Url(Get(row, "artwork_thumbnail")),
                Publisher = publisher,
                ExternalId = Clip(Get(row, "listennotes_id"), ExternalIdMaxLength)
            });

            if (!episode.Created) return false;

            result.EpisodesCreatedCount++;
            createdIds.Add(episode.Episode.Id);
            return true;
        }

        private static (HashSet<string> Header, List<Dictionary<string, string>> Rows) ReadRows(Stream stream)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
                MissingFieldFound = null,
                BadDataFound = null,
                HeaderValidated = null
            };

            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);

            if (!csv.Read() || !csv.ReadHeader() || csv.HeaderRecord == null)
            {
                return (new HashSet<string>(), new List<Dictionary<string, string>>());
            }

            var columns = csv.HeaderRecord.Select(h => h.Trim().ToLowerInvariant()).ToArray();
            var rows = new List<Dictionary<string, string>>();
            while (csv.Read())
            {
                var row = new Dictionary<string, string>(StringComparer.Ordinal);
                for (var i = 0; i < columns.Length; i++)
                {
                    if (csv.TryGetField<string>(i, out var value) && value != null)
                    {
                        row[columns[i]] = value;
                    }
                }
                rows.Add(row);
            }

            return (columns.ToHashSet(StringComparer.Ordinal), rows);
        }

        /// <summary>The trimmed value, or null when the column is missing or blank (never "").</summary>
        private static string? Get(Dictionary<string, string> row, string column) =>
            row.TryGetValue(column, out var value) && !string.IsNullOrWhiteSpace(value) ? value.Trim() : null;

        private static string NormalizeTitle(string title) => title.Trim().ToLowerInvariant();

        private static string? Clip(string? value, int maxLength) =>
            value == null ? null : value.Length <= maxLength ? value : value[..maxLength];

        /// <summary>An over-long URL is dropped rather than cut, since a cut URL is a broken one.</summary>
        private static string? Url(string? value) =>
            value != null && value.Length <= UrlMaxLength ? value : null;

        private static DateTime? ParseDate(string? value) =>
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed.UtcDateTime
                : null;

        private static string[] SplitGenres(string? value) =>
            value == null
                ? Array.Empty<string>()
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(g => g.ToLowerInvariant())
                    .Distinct()
                    .ToArray();

        private static PodcastDatasetImportResultDto Fail(PodcastDatasetImportResultDto result, string message)
        {
            result.Success = false;
            result.ErrorMessage = message;
            result.CompletedAt = DateTime.UtcNow;
            return result;
        }
    }
}
