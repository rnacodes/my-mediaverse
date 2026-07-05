using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Interfaces
{
    public interface IPodcastOpmlImportService
    {
        /// <summary>
        /// Import podcast subscriptions from an OPML export stream (e.g. Overcast, Apple Podcasts).
        /// Each feed becomes a lightweight <c>PodcastSeries</c> stub (title + RSS feed url + Apple
        /// Podcasts id) with no external API calls; a separate enrichment pass fills in the rest.
        /// </summary>
        /// <param name="opmlStream">The OPML file stream.</param>
        /// <returns>Import result with created/skipped/failed counts and any per-feed failures.</returns>
        Task<OpmlImportResultDto> ImportFromOpmlAsync(Stream opmlStream);
    }
}
