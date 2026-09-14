using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Utilities
{
    /// <summary>
    /// In-memory identity lookup over one series' episodes, for comparing a whole feed against the
    /// library with a single query. Matches in the same order as <see cref="PodcastEpisodeDuplicateFinder"/>:
    /// feed guid, then enclosure URL, then title plus release day.
    /// </summary>
    public sealed class PodcastEpisodeIdentityIndex
    {
        /// <summary>The identity columns of one stored (or just-created) episode.</summary>
        public sealed record Entry(Guid Id, string? RssGuid, string? AudioLink, string Title, DateTime? ReleaseDate);

        private readonly Dictionary<string, Entry> _byGuid = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Entry> _byAudioKey = new(StringComparer.Ordinal);
        private readonly Dictionary<string, Entry> _byTitleAndDay = new(StringComparer.Ordinal);

        private PodcastEpisodeIdentityIndex(IEnumerable<Entry> entries)
        {
            foreach (var entry in entries)
                Add(entry);
        }

        /// <summary>Release date of the newest episode with one, or null when none has a date.</summary>
        public DateTime? NewestReleaseDate { get; private set; }

        public static async Task<PodcastEpisodeIdentityIndex> BuildAsync(
            IQueryable<PodcastEpisode> episodes, Guid seriesId, CancellationToken cancellationToken = default)
        {
            var entries = await episodes
                .Where(e => e.SeriesId == seriesId)
                .Select(e => new Entry(e.Id, e.RssGuid, e.AudioLink, e.Title, e.ReleaseDate))
                .ToListAsync(cancellationToken);
            return new PodcastEpisodeIdentityIndex(entries);
        }

        public static PodcastEpisodeIdentityIndex From(IEnumerable<Entry> entries) => new(entries);

        /// <summary>Finds the stored episode matching the given identity values, or null.</summary>
        public Entry? Find(string? rssGuid, string? audioLink, string? title, DateTime? releaseDate)
        {
            if (!string.IsNullOrWhiteSpace(rssGuid) && _byGuid.TryGetValue(rssGuid.Trim(), out var byGuid))
                return byGuid;

            var audioKey = UrlNormalizer.GetComparisonKey(audioLink);
            if (!string.IsNullOrEmpty(audioKey) && _byAudioKey.TryGetValue(audioKey, out var byAudio))
                return byAudio;

            var titleKey = TitleAndDayKey(title, releaseDate);
            if (titleKey != null && _byTitleAndDay.TryGetValue(titleKey, out var byTitle))
                return byTitle;

            return null;
        }

        /// <summary>True when some episode other than <paramref name="exceptId"/> already owns the guid.</summary>
        public bool GuidOwnedByOther(string rssGuid, Guid exceptId) =>
            _byGuid.TryGetValue(rssGuid.Trim(), out var owner) && owner.Id != exceptId;

        /// <summary>Registers an episode (or a newly assigned guid) so later lookups in the same run see it.</summary>
        public void Add(Entry entry)
        {
            if (!string.IsNullOrWhiteSpace(entry.RssGuid))
                _byGuid.TryAdd(entry.RssGuid.Trim(), entry);

            var audioKey = UrlNormalizer.GetComparisonKey(entry.AudioLink);
            if (!string.IsNullOrEmpty(audioKey))
                _byAudioKey.TryAdd(audioKey, entry);

            var titleKey = TitleAndDayKey(entry.Title, entry.ReleaseDate);
            if (titleKey != null)
                _byTitleAndDay.TryAdd(titleKey, entry);

            if (entry.ReleaseDate.HasValue)
            {
                var date = DateTimeNormalizer.ToUtc(entry.ReleaseDate.Value);
                if (NewestReleaseDate == null || date > NewestReleaseDate)
                    NewestReleaseDate = date;
            }
        }

        /// <summary>Records a guid newly filled onto a stored episode.</summary>
        public void AssignGuid(Entry entry, string rssGuid) =>
            _byGuid.TryAdd(rssGuid.Trim(), entry with { RssGuid = rssGuid.Trim() });

        // Titles such as "Trailer" repeat, so a title only identifies an episode together with its day.
        private static string? TitleAndDayKey(string? title, DateTime? releaseDate) =>
            string.IsNullOrWhiteSpace(title) || !releaseDate.HasValue
                ? null
                : $"{title.Trim().ToLowerInvariant()}|{DateTimeNormalizer.ToUtc(releaseDate.Value).Date:yyyy-MM-dd}";
    }
}
