using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Shared.DTOs.Podcasts;
using MyMediaVerse.Shared.Exceptions;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Podcasts
{
    /// <summary>
    /// Reads podcast RSS feeds. Outbound fetches follow fixed rules: http/https only, a time limit
    /// covering headers and body, a 5 MB size cap enforced while streaming, and no DTD processing.
    /// Parsing uses XDocument rather than SyndicationFeed, which rejects the non-RFC-822 dates common
    /// in podcast feeds and has no typed access to the itunes: and podcast: namespaces.
    /// Every failure surfaces as a <see cref="PodcastFeedException"/> with a reason.
    /// </summary>
    public class RssPodcastFeedReader : IPodcastFeedReader
    {
        public const long MaxFeedBytes = 5 * 1024 * 1024;

        private const string ItunesNamespace = "http://www.itunes.com/dtds/podcast-1.0.dtd";
        private const string ContentNamespace = "http://purl.org/rss/1.0/modules/content/";

        // The Podcasting 2.0 namespace, plus the GitHub URL some hosts still declare for it.
        private static readonly string[] PodcastNamespaces =
        {
            "https://podcastindex.org/namespace/1.0",
            "https://github.com/Podcastindex-org/podcast-namespace/blob/main/docs/1.0.md"
        };

        private static readonly Dictionary<string, string> NamedTimeZones = new(StringComparer.OrdinalIgnoreCase)
        {
            ["GMT"] = "+00:00", ["UT"] = "+00:00", ["UTC"] = "+00:00", ["Z"] = "+00:00",
            ["EST"] = "-05:00", ["EDT"] = "-04:00", ["CST"] = "-06:00", ["CDT"] = "-05:00",
            ["MST"] = "-07:00", ["MDT"] = "-06:00", ["PST"] = "-08:00", ["PDT"] = "-07:00"
        };

        private static readonly Regex LeadingDayName = new(@"^[A-Za-z]{3,9},\s*", RegexOptions.Compiled);
        private static readonly Regex TrailingNamedZone = new(@"\s([A-Za-z]{1,3})$", RegexOptions.Compiled);
        private static readonly Regex TrailingNumericOffset = new(@"([+-])(\d{2}):?(\d{2})$", RegexOptions.Compiled);
        private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

        private readonly HttpClient _httpClient;
        private readonly ILogger<RssPodcastFeedReader> _logger;

        public RssPodcastFeedReader(HttpClient httpClient, ILogger<RssPodcastFeedReader> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<PodcastFeed> ReadAsync(string feedUrl, int maxItems = int.MaxValue, CancellationToken cancellationToken = default)
        {
            if (!UrlNormalizer.IsValid(feedUrl))
                throw new PodcastFeedException(PodcastFeedFailureReason.InvalidUrl,
                    "The feed URL must be an absolute http or https URL.");

            var uri = new Uri(feedUrl.Trim());

            // HttpClient.Timeout only covers the headers once ResponseHeadersRead is used, so the
            // same limit is applied to the whole read through a linked token.
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (_httpClient.Timeout != Timeout.InfiniteTimeSpan)
                timeout.CancelAfter(_httpClient.Timeout);

            try
            {
                using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Podcast feed returned {StatusCode} for {FeedUrl}", (int)response.StatusCode, feedUrl);
                    throw new PodcastFeedException(PodcastFeedFailureReason.HttpError,
                        $"The feed returned HTTP {(int)response.StatusCode}.", response.StatusCode);
                }

                if (response.Content.Headers.ContentLength > MaxFeedBytes)
                    throw TooLarge();

                using var buffer = await ReadCappedAsync(response.Content, timeout.Token);
                return Parse(buffer, maxItems);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning("Podcast feed timed out: {FeedUrl}", feedUrl);
                throw new PodcastFeedException(PodcastFeedFailureReason.Timeout, "The feed took too long to respond.", innerException: ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Podcast feed host unreachable: {FeedUrl}", feedUrl);
                throw new PodcastFeedException(PodcastFeedFailureReason.Unreachable, "The feed's server could not be reached.", innerException: ex);
            }
        }

        private static async Task<MemoryStream> ReadCappedAsync(HttpContent content, CancellationToken cancellationToken)
        {
            await using var stream = await content.ReadAsStreamAsync(cancellationToken);
            var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + read > MaxFeedBytes)
                {
                    await buffer.DisposeAsync();
                    throw TooLarge();
                }
                buffer.Write(chunk, 0, read);
            }
            buffer.Position = 0;
            return buffer;
        }

        private static PodcastFeedException TooLarge() =>
            new(PodcastFeedFailureReason.TooLarge, "The feed is larger than 5 MB.");

        /// <summary>
        /// Parses an RSS document. DTDs are refused outright, which also rules out external entities.
        /// </summary>
        public static PodcastFeed Parse(Stream xml, int maxItems = int.MaxValue)
        {
            XDocument document;
            try
            {
                var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, IgnoreComments = true };
                using var reader = XmlReader.Create(xml, settings);
                document = XDocument.Load(reader);
            }
            catch (XmlException ex)
            {
                throw new PodcastFeedException(PodcastFeedFailureReason.InvalidXml, "The feed is not valid XML.", innerException: ex);
            }

            var channel = document.Root is { Name.LocalName: "rss" } root
                ? root.Elements().FirstOrDefault(e => e.Name.LocalName == "channel" && e.Name.NamespaceName.Length == 0)
                : null;
            if (channel == null)
                throw new PodcastFeedException(PodcastFeedFailureReason.NotAFeed, "The URL does not point to an RSS podcast feed.");

            var items = channel.Elements("item").ToList();

            return new PodcastFeed
            {
                Series = ParseSeries(channel),
                Episodes = items.Take(Math.Max(0, maxItems)).Select(ParseEpisode).ToList(),
                TotalItemCount = items.Count
            };
        }

        private static FeedSeries ParseSeries(XElement channel)
        {
            var owner = Itunes(channel, "owner");

            return new FeedSeries
            {
                Title = Text(channel.Element("title")),
                Description = HtmlText.Strip(Text(channel.Element("description")))
                    ?? HtmlText.Strip(Text(Itunes(channel, "summary")))
                    ?? HtmlText.Strip(Text(Itunes(channel, "subtitle"))),
                Publisher = Text(Itunes(channel, "author")) ?? Text(owner == null ? null : Itunes(owner, "name")),
                ImageUrl = Attribute(Itunes(channel, "image"), "href") ?? Text(channel.Element("image")?.Element("url")),
                Link = Text(channel.Element("link")),
                Language = Text(channel.Element("language")),
                PodcastGuid = Text(channel.Elements().FirstOrDefault(e => e.Name.LocalName == "guid" && IsPodcastNamespace(e.Name))),
                Categories = channel.Elements()
                    .Where(e => IsItunes(e.Name, "category"))
                    .SelectMany(e => e.Elements().Where(sub => IsItunes(sub.Name, "category")).Prepend(e))
                    .Select(e => Attribute(e, "text"))
                    .OfType<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                Explicit = ParseExplicit(Text(Itunes(channel, "explicit")))
            };
        }

        private static FeedEpisode ParseEpisode(XElement item)
        {
            var enclosure = item.Elements("enclosure").FirstOrDefault(e => IsMediaEnclosure(e) && Attribute(e, "url") != null);

            return new FeedEpisode
            {
                Title = Text(item.Element("title")) ?? Text(Itunes(item, "title")),
                Description = HtmlText.Strip(Text(item.Element("description")))
                    ?? HtmlText.Strip(Text(item.Elements().FirstOrDefault(e => e.Name.LocalName == "encoded" && e.Name.NamespaceName == ContentNamespace)))
                    ?? HtmlText.Strip(Text(Itunes(item, "summary"))),
                EnclosureUrl = Attribute(enclosure, "url"),
                EnclosureType = Attribute(enclosure, "type"),
                PublishedAt = ParseDate(Text(item.Element("pubDate"))),
                Guid = Text(item.Element("guid")),
                DurationSeconds = ParseDuration(Text(Itunes(item, "duration"))),
                ImageUrl = Attribute(Itunes(item, "image"), "href"),
                EpisodeNumber = ParsePositiveInt(Text(Itunes(item, "episode"))),
                SeasonNumber = ParsePositiveInt(Text(Itunes(item, "season"))),
                Link = Text(item.Element("link"))
            };
        }

        // An enclosure with no type is kept: hosts that omit it almost always serve audio.
        private static bool IsMediaEnclosure(XElement enclosure)
        {
            var type = Attribute(enclosure, "type");
            return type == null
                || type.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
                || type.StartsWith("video/", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parses RFC 822 dates as feeds actually write them: wrong or missing day names, named US
        /// zones, offsets with or without a colon, single-digit days, and ISO 8601. Returns UTC, or
        /// null when nothing sensible can be read.
        /// </summary>
        public static DateTime? ParseDate(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var text = Whitespace.Replace(value.Trim(), " ");
            text = LeadingDayName.Replace(text, string.Empty);

            var namedZone = TrailingNamedZone.Match(text);
            if (namedZone.Success && NamedTimeZones.TryGetValue(namedZone.Groups[1].Value, out var offset))
                text = text[..namedZone.Index] + " " + offset;

            text = TrailingNumericOffset.Replace(text, "$1$2:$3");

            return DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var parsed)
                ? parsed.UtcDateTime
                : null;
        }

        /// <summary>Parses itunes:duration written as seconds, MM:SS or HH:MM:SS.</summary>
        public static int? ParseDuration(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var parts = value.Trim().Split(':');
            if (parts.Length > 3)
                return null;

            double total = 0;
            foreach (var part in parts)
            {
                if (!double.TryParse(part, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number) || number < 0)
                    return null;
                total = total * 60 + number;
            }

            return total > int.MaxValue ? null : (int)Math.Round(total);
        }

        private static int? ParsePositiveInt(string? value) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) && number > 0 ? number : null;

        private static bool? ParseExplicit(string? value) => value?.ToLowerInvariant() switch
        {
            "true" or "yes" or "explicit" => true,
            "false" or "no" or "clean" => false,
            _ => null
        };

        private static XElement? Itunes(XElement parent, string localName) =>
            parent.Elements().FirstOrDefault(e => IsItunes(e.Name, localName));

        // Some feeds declare the iTunes namespace with different capitalization.
        private static bool IsItunes(XName name, string localName) =>
            name.LocalName == localName && string.Equals(name.NamespaceName, ItunesNamespace, StringComparison.OrdinalIgnoreCase);

        private static bool IsPodcastNamespace(XName name) =>
            PodcastNamespaces.Any(ns => string.Equals(name.NamespaceName, ns, StringComparison.OrdinalIgnoreCase));

        private static string? Text(XElement? element)
        {
            var value = element?.Value.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static string? Attribute(XElement? element, string name)
        {
            var value = element?.Attribute(name)?.Value.Trim();
            return string.IsNullOrEmpty(value) ? null : value;
        }
    }
}
