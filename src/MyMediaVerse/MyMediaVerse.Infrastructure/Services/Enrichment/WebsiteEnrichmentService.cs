using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.DTOs.WebsiteScraper;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Infrastructure.Services.Enrichment
{
    /// <summary>
    /// Fills in website stubs after the fact. For each pending website: backfill its identity
    /// columns, scrape the page, copy every missing field from the scrape, render a screenshot when
    /// the page still has no image and the monthly budget allows, look up a Wayback snapshot, then
    /// stamp the row as enriched. Every write is fill-only; a title is replaced only when it is a
    /// placeholder (the domain or "Untitled Website"). Each website is saved on its own, so a run
    /// that is interrupted keeps what it finished.
    /// </summary>
    public class WebsiteEnrichmentService : IWebsiteEnrichmentService
    {
        private const string UntitledTitle = "Untitled Website";
        private const string ThumIoHost = "image.thum.io/";
        private const string ScreenshotFolder = "/screenshots/";

        private readonly IApplicationDbContext _context;
        private readonly IWebsiteScraperService _scraper;
        private readonly IWebsiteScreenshotService _screenshots;
        private readonly IScreenshotQuota _quota;
        private readonly IWaybackMachineClient _wayback;
        private readonly ILinkChecker _linkChecker;
        private readonly IThumbnailStorageService _thumbnailStorage;
        private readonly ISyncStateService _syncState;
        private readonly WebsiteEnrichmentOptions _options;
        private readonly ILogger<WebsiteEnrichmentService> _logger;

        public WebsiteEnrichmentService(
            IApplicationDbContext context,
            IWebsiteScraperService scraper,
            IWebsiteScreenshotService screenshots,
            IScreenshotQuota quota,
            IWaybackMachineClient wayback,
            ILinkChecker linkChecker,
            IThumbnailStorageService thumbnailStorage,
            ISyncStateService syncState,
            IOptions<WebsiteEnrichmentOptions> options,
            ILogger<WebsiteEnrichmentService> logger)
        {
            _context = context;
            _scraper = scraper;
            _screenshots = screenshots;
            _quota = quota;
            _wayback = wayback;
            _linkChecker = linkChecker;
            _thumbnailStorage = thumbnailStorage;
            _syncState = syncState;
            _options = options.Value;
            _logger = logger;
        }

        public Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default) =>
            _context.Websites.CountAsync(w => w.EnrichedAt == null, cancellationToken);

        #region Enrichment run

        public async Task<WebsiteEnrichmentResult> EnrichPendingAsync(int limit, CancellationToken cancellationToken = default)
        {
            var result = new WebsiteEnrichmentResult { StartedAt = DateTime.UtcNow };
            limit = ClampLimit(limit);

            try
            {
                var candidates = await _context.Websites
                    .Include(w => w.Topics)
                    .Include(w => w.Genres)
                    .Where(w => w.EnrichedAt == null)
                    .OrderBy(w => w.DateAdded)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Website enrichment: {Count} pending websites in this run", candidates.Count);

                // Once the budget is gone, stop asking so the rest of the run costs no quota reads.
                var screenshotsAllowed = true;
                var leftWithoutThumbnail = 0;
                var wayback = new WaybackGate();

                // A page is bounded by time as well as by count: dead hosts each cost a full connect
                // timeout, and the caller's request must finish. Whatever is not reached stays pending.
                var timeBudget = _options.RunTimeBudgetSeconds > 0 ? TimeSpan.FromSeconds(_options.RunTimeBudgetSeconds) : (TimeSpan?)null;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                foreach (var website in candidates)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        break;
                    }

                    if (timeBudget.HasValue && stopwatch.Elapsed >= timeBudget.Value)
                    {
                        result.TimeBudgetReached = true;
                        _logger.LogInformation(
                            "Website enrichment: time budget of {Budget}s spent after {Processed} of {Count}; the rest stay pending",
                            _options.RunTimeBudgetSeconds, result.TotalProcessed, candidates.Count);
                        break;
                    }

                    result.TotalProcessed++;

                    try
                    {
                        var outcome = await EnrichOneAsync(website, screenshotsAllowed, wayback, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        if (outcome.QuotaReached)
                        {
                            screenshotsAllowed = false;
                            result.QuotaReached = true;
                        }

                        if (outcome.ScreenshotRendered) result.ScreenshotsRendered++;
                        if (outcome.WantedScreenshot && !outcome.ScreenshotRendered) leftWithoutThumbnail++;

                        if (outcome.Unreachable) result.SkippedCount++;
                        else if (outcome.FilledFields.Count > 0) result.EnrichedCount++;
                        else result.UnchangedCount++;
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to enrich '{website.Title}' ({website.Link}): {ex.Message}");
                        _logger.LogWarning(ex, "Failed to enrich website {Id} ({Link})", website.Id, website.Link);
                    }
                }

                result.PendingCount = await GetPendingCountAsync(cancellationToken);
                result.WaybackPaused = !wayback.Allowed;
                result.WarningMessage = BuildWarning(result, leftWithoutThumbnail, wayback);
                result.CompletedAt = DateTime.UtcNow;

                if (!result.WasCancelled)
                {
                    await _syncState.MarkSyncSucceededAsync(WebsiteEnrichmentResult.EnrichmentOperation, result.StartedAt);
                }

                _logger.LogInformation(
                    "Website enrichment complete. Enriched: {Enriched}, Unchanged: {Unchanged}, Skipped: {Skipped}, Failed: {Failed}, Screenshots: {Screenshots}, Pending: {Pending}",
                    result.EnrichedCount, result.UnchangedCount, result.SkippedCount, result.FailedCount, result.ScreenshotsRendered, result.PendingCount);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                result.WasCancelled = true;
                result.CompletedAt = DateTime.UtcNow;
                result.PendingCount = await TryGetPendingCountAsync();
                _logger.LogInformation("Website enrichment was canceled");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Enrichment run failed: {ex.Message}";
                result.CompletedAt = DateTime.UtcNow;
                result.PendingCount = await TryGetPendingCountAsync();
                _logger.LogError(ex, "Website enrichment run failed");
            }

            return result;
        }

        /// <summary>
        /// The pending count for a run that ended early. Callers loop on this number, so a canceled
        /// or failed run must not report zero left when the queue is untouched; if even the count
        /// fails, zero is the honest fallback because nothing more can be learned.
        /// </summary>
        private async Task<int> TryGetPendingCountAsync()
        {
            try
            {
                return await GetPendingCountAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not read the pending website count after an interrupted run");
                return 0;
            }
        }

        public async Task<SingleWebsiteEnrichmentResult> EnrichByIdAsync(Guid id, bool force, CancellationToken cancellationToken = default)
        {
            var result = new SingleWebsiteEnrichmentResult();

            try
            {
                var website = await _context.Websites
                    .Include(w => w.Topics)
                    .Include(w => w.Genres)
                    .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

                if (website == null)
                {
                    result.NotFound = true;
                    result.Success = false;
                    result.ErrorMessage = "Website not found";
                    return result;
                }

                result.Title = website.Title;
                result.LastHttpStatus = website.LastHttpStatus;

                if (website.EnrichedAt != null && !force)
                {
                    result.AlreadyEnriched = true;
                    result.WarningMessage = "The website has already been enriched; pass force=true to run the fill again.";
                    return result;
                }

                var outcome = await EnrichOneAsync(website, screenshotsAllowed: true, wayback: null, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                result.Title = website.Title;
                result.FilledFields = outcome.FilledFields;
                result.ScreenshotRendered = outcome.ScreenshotRendered;
                result.QuotaReached = outcome.QuotaReached;
                result.Unreachable = outcome.Unreachable;
                result.LastHttpStatus = website.LastHttpStatus;

                if (outcome.Unreachable)
                    result.WarningMessage = outcome.FilledFields.Contains("waybackUrl")
                        ? $"The page could not be fetched (status {website.LastHttpStatus}); an archived copy was linked instead."
                        : $"The page could not be fetched (status {website.LastHttpStatus}); nothing was filled.";
                else if (outcome.QuotaReached)
                    result.WarningMessage = "The monthly screenshot quota has been reached; the website was enriched without a screenshot.";
                else if (outcome.WantedScreenshot && !outcome.ScreenshotRendered)
                    result.WarningMessage = "No usable screenshot could be rendered for this page.";

                _logger.LogInformation("Enriched website {Id}: filled {Fields}", id, string.Join(", ", result.FilledFields));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Failed to enrich website: {ex.Message}";
                _logger.LogError(ex, "Failed to enrich website {Id}", id);
            }

            return result;
        }

        /// <summary>
        /// Per-run Wayback breaker. The archive throttles sustained callers, and every throttled
        /// lookup costs the full timeout for nothing, so after a streak of slow answers the run
        /// stops asking and counts what it left without an archive link.
        /// </summary>
        private sealed class WaybackGate
        {
            public bool Allowed { get; set; } = true;
            public int SlowStreak { get; set; }
            public int SkippedCount { get; set; }
        }

        private sealed class EnrichOutcome
        {
            public List<string> FilledFields { get; } = new();
            public bool Unreachable { get; set; }
            public bool WantedScreenshot { get; set; }
            public bool ScreenshotRendered { get; set; }
            public bool QuotaReached { get; set; }
        }

        /// <summary>
        /// Runs the fill for one tracked website. Does not save; callers decide when to flush.
        /// </summary>
        private async Task<EnrichOutcome> EnrichOneAsync(Website website, bool screenshotsAllowed, WaybackGate? wayback, CancellationToken cancellationToken)
        {
            var outcome = new EnrichOutcome();
            var now = DateTime.UtcNow;

            if (WebsiteDuplicateFinder.FillIdentity(website))
            {
                outcome.FilledFields.Add("urlKey");
            }

            ScrapedWebsiteDataDto? scraped;
            try
            {
                scraped = await _scraper.ScrapeWebsiteAsync(website.Link ?? string.Empty);
            }
            catch (Exception ex) when (ex is HttpRequestException or ArgumentException
                || (ex is OperationCanceledException && !cancellationToken.IsCancellationRequested))
            {
                // The page is gone, the stored URL is unusable, or the host hung until the scraper's
                // own timeout fired (that surfaces as a cancellation that nobody requested). Record
                // what we learned and mark the row enriched so the run does not retry it forever; a
                // later link check or a forced re-run can revisit it.
                website.LastHttpStatus = await ResolveFailureStatusAsync(ex, website.Link, cancellationToken);
                website.LastCheckedDate = now;
                website.EnrichedAt = now;
                outcome.Unreachable = true;
                _logger.LogInformation("Website {Id} ({Link}) could not be fetched: status {Status}", website.Id, website.Link, website.LastHttpStatus);

                // A page that is gone is exactly the one whose archived copy matters, and the
                // profile offers that copy only when a snapshot URL is stored.
                await TryFillWaybackAsync(website, outcome, wayback, cancellationToken);
                return outcome;
            }

            if (scraped != null)
            {
                if (IsPlaceholderTitle(website) && !string.IsNullOrWhiteSpace(scraped.Title))
                {
                    website.Title = scraped.Title.Trim();
                    outcome.FilledFields.Add("title");
                }

                var incoming = new Website
                {
                    Title = website.Title,
                    Description = scraped.Description,
                    Thumbnail = scraped.ImageUrl,
                    RssFeedUrl = scraped.RssFeedUrl,
                    Author = scraped.Author,
                    Publication = scraped.Publication
                };

                // One merge rule for every website-creating path: reuse it and record what moved.
                var before = Snapshot(website);
                WebsiteDuplicateFinder.AbsorbMetadata(website, incoming);
                outcome.FilledFields.AddRange(Diff(before, website));
            }

            if (string.IsNullOrWhiteSpace(website.Thumbnail) && !string.IsNullOrWhiteSpace(website.Link))
            {
                outcome.WantedScreenshot = true;

                if (!screenshotsAllowed || await _quota.RemainingAsync(cancellationToken) <= 0)
                {
                    outcome.QuotaReached = true;
                }
                else
                {
                    var thumbnail = await _screenshots.CaptureScreenshotAsync(website.Link, cancellationToken);
                    if (!string.IsNullOrEmpty(thumbnail))
                    {
                        website.Thumbnail = thumbnail;
                        outcome.ScreenshotRendered = true;
                        outcome.FilledFields.Add("thumbnail");
                    }
                }
            }

            await TryFillWaybackAsync(website, outcome, wayback, cancellationToken);

            website.LastCheckedDate = now;
            website.LastHttpStatus = 200;
            website.EnrichedAt = now;
            return outcome;
        }

        /// <summary>
        /// Fill-only Wayback lookup, for live and dead pages alike. The client returns null on any
        /// failure, so a slow or refused lookup costs time but never the row. Inside a run the gate
        /// watches for the archive throttling us: a lookup that used most of its timeout counts as
        /// slow, a streak of them pauses lookups for the rest of the run, and any quick answer
        /// (even "no captures") resets the streak. A single-item enrich passes no gate.
        /// </summary>
        private async Task TryFillWaybackAsync(Website website, EnrichOutcome outcome, WaybackGate? wayback, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(website.WaybackUrl) || string.IsNullOrWhiteSpace(website.Link))
                return;

            if (wayback is { Allowed: false })
            {
                wayback.SkippedCount++;
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var snapshot = await _wayback.FindLatestSnapshotAsync(website.Link, cancellationToken);
            stopwatch.Stop();

            if (!string.IsNullOrWhiteSpace(snapshot))
            {
                website.WaybackUrl = snapshot;
                outcome.FilledFields.Add("waybackUrl");
            }

            if (wayback != null && _options.WaybackPauseAfterSlowLookups > 0)
            {
                var slowAfter = TimeSpan.FromSeconds(Math.Max(1, _options.WaybackTimeoutSeconds) * 0.9);
                if (stopwatch.Elapsed >= slowAfter)
                {
                    wayback.SlowStreak++;
                    if (wayback.SlowStreak >= _options.WaybackPauseAfterSlowLookups)
                    {
                        wayback.Allowed = false;
                        _logger.LogWarning(
                            "Website enrichment: Wayback lookups paused for the rest of this run after {Streak} slow responses (>= {Seconds:0.#}s each)",
                            wayback.SlowStreak, slowAfter.TotalSeconds);
                    }
                }
                else
                {
                    wayback.SlowStreak = 0;
                }
            }

            if (_options.WaybackDelayMs > 0)
            {
                await Task.Delay(_options.WaybackDelayMs, cancellationToken);
            }
        }

        /// <summary>
        /// The status behind a failed scrape. The scraper wraps its HTTP errors, so the code is
        /// looked for down the exception chain; when none is recorded (the wrapper dropped it, or
        /// the host never answered), or when it is a redirect the scraper's client would not follow
        /// (an https-to-http hop, for one), the link checker probes the URL so the recorded status
        /// is where the link actually ends up rather than "unreachable" or a bare 3xx.
        /// </summary>
        private async Task<int> ResolveFailureStatusAsync(Exception exception, string? link, CancellationToken cancellationToken)
        {
            if (exception is ArgumentException)
                return 0;

            if (exception is OperationCanceledException)
                return 0;

            for (var current = exception; current != null; current = current.InnerException)
            {
                if (current is HttpRequestException { StatusCode: { } status } && (int)status is < 300 or >= 400)
                    return (int)status;
            }

            return string.IsNullOrWhiteSpace(link) ? 0 : await _linkChecker.CheckAsync(link, cancellationToken);
        }

        private static bool IsPlaceholderTitle(Website website)
        {
            var title = website.Title?.Trim();
            if (string.IsNullOrEmpty(title))
                return true;

            if (string.Equals(title, UntitledTitle, StringComparison.OrdinalIgnoreCase))
                return true;

            var domain = website.Domain ?? UrlNormalizer.ExtractDomain(website.Link);
            return !string.IsNullOrEmpty(domain) && string.Equals(title, domain, StringComparison.OrdinalIgnoreCase);
        }

        private static (string? Description, string? Thumbnail, string? RssFeedUrl, string? Author, string? Publication) Snapshot(Website w) =>
            (w.Description, w.Thumbnail, w.RssFeedUrl, w.Author, w.Publication);

        private static IEnumerable<string> Diff(
            (string? Description, string? Thumbnail, string? RssFeedUrl, string? Author, string? Publication) before,
            Website after)
        {
            if (before.Description != after.Description) yield return "description";
            if (before.Thumbnail != after.Thumbnail) yield return "thumbnail";
            if (before.RssFeedUrl != after.RssFeedUrl) yield return "rssFeedUrl";
            if (before.Author != after.Author) yield return "author";
            if (before.Publication != after.Publication) yield return "publication";
        }

        private static string? BuildWarning(WebsiteEnrichmentResult result, int leftWithoutThumbnail, WaybackGate wayback)
        {
            var parts = new List<string>();

            if (!wayback.Allowed)
            {
                parts.Add($"Wayback lookups paused after {wayback.SlowStreak} slow responses; {wayback.SkippedCount} website(s) were left without an archive link. Rerun their enrich with force=true once the archive answers again.");
            }

            if (result.QuotaReached)
            {
                parts.Add($"Screenshot quota reached; {leftWithoutThumbnail} website(s) were left without a thumbnail. Rerun next month or switch the screenshot provider.");
            }

            if (result.FailedCount > 0)
            {
                parts.Add($"{result.FailedCount} of {result.TotalProcessed} websites failed and will be retried next run.");
            }

            if (result.SkippedCount > 0)
            {
                parts.Add($"{result.SkippedCount} website(s) could not be fetched; their status was recorded.");
            }

            return parts.Count == 0 ? null : string.Join(" ", parts);
        }

        #endregion

        #region Link check

        public async Task<WebsiteLinkCheckResult> CheckLinksAsync(int limit, int olderThanDays, CancellationToken cancellationToken = default)
        {
            var result = new WebsiteLinkCheckResult { StartedAt = DateTime.UtcNow };
            limit = ClampLimit(limit);
            var cutoff = DateTime.UtcNow.AddDays(-Math.Max(0, olderThanDays));

            try
            {
                // Never-checked rows first (a save stamps LastCheckedDate, so the status column is
                // the reliable "never checked" signal), then the stalest checks.
                var candidates = await _context.Websites
                    .Where(w => w.LastHttpStatus == null || w.LastCheckedDate == null || w.LastCheckedDate < cutoff)
                    .OrderBy(w => w.LastHttpStatus == null ? 0 : 1)
                    .ThenBy(w => w.LastCheckedDate ?? DateTime.MinValue)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                result.TotalProcessed = candidates.Count;
                _logger.LogInformation("Website link check: {Count} links in this run", candidates.Count);

                foreach (var website in candidates)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        break;
                    }

                    try
                    {
                        var previous = website.LastHttpStatus;
                        var status = await _linkChecker.CheckAsync(website.Link ?? string.Empty, cancellationToken);

                        website.LastHttpStatus = status;
                        website.LastCheckedDate = DateTime.UtcNow;
                        await _context.SaveChangesAsync(cancellationToken);

                        var wasBroken = previous.HasValue && IsBroken(previous.Value);
                        var isBroken = IsBroken(status);

                        if (isBroken) result.BrokenCount++;
                        if (previous != status) result.ChangedCount++;

                        if (isBroken && !wasBroken)
                        {
                            result.NewlyBroken.Add(new WebsiteLinkStatus
                            {
                                Id = website.Id,
                                Title = website.Title,
                                Link = website.Link,
                                Status = status,
                                PreviousStatus = previous,
                                WaybackUrl = website.WaybackUrl
                            });
                        }
                        else if (!isBroken && wasBroken)
                        {
                            result.RecoveredCount++;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to check '{website.Title}' ({website.Link}): {ex.Message}");
                        _logger.LogWarning(ex, "Failed to check link for website {Id} ({Link})", website.Id, website.Link);
                    }
                }

                if (result.FailedCount > 0)
                {
                    result.WarningMessage = $"{result.FailedCount} of {result.TotalProcessed} link checks failed.";
                }

                result.CompletedAt = DateTime.UtcNow;

                if (!result.WasCancelled)
                {
                    await _syncState.MarkSyncSucceededAsync(WebsiteLinkCheckResult.LinkCheckOperation, result.StartedAt);
                }

                _logger.LogInformation(
                    "Website link check complete. Checked: {Checked}, Broken: {Broken}, Newly broken: {NewlyBroken}, Recovered: {Recovered}, Failed: {Failed}",
                    result.TotalProcessed, result.BrokenCount, result.NewlyBroken.Count, result.RecoveredCount, result.FailedCount);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                result.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Website link check was canceled");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Link check run failed: {ex.Message}";
                result.CompletedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Website link check run failed");
            }

            return result;
        }

        private static bool IsBroken(int status) => status == 0 || status >= 400;

        #endregion

        #region Thumbnail repair

        public async Task<WebsiteEnrichmentResult> RepairThumbnailsAsync(int limit, CancellationToken cancellationToken = default)
        {
            var result = new WebsiteEnrichmentResult
            {
                StartedAt = DateTime.UtcNow,
                Operation = WebsiteEnrichmentResult.ThumbnailRepairOperation
            };
            limit = ClampLimit(limit);

            try
            {
                // Thumbnails that point at the render provider itself (an impression per view and a
                // dead link once the cache expires) or at an animated placeholder we stored.
                var candidates = await _context.Websites
                    .Where(w => w.Thumbnail != null &&
                                (w.Thumbnail.Contains(ThumIoHost) ||
                                 (w.Thumbnail.Contains(ScreenshotFolder) && w.Thumbnail.EndsWith(".gif"))))
                    .OrderBy(w => w.DateAdded)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                result.TotalProcessed = candidates.Count;
                _logger.LogInformation("Website thumbnail repair: {Count} thumbnails in this run", candidates.Count);

                foreach (var website in candidates)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        result.WasCancelled = true;
                        break;
                    }

                    if (await _quota.RemainingAsync(cancellationToken) <= 0)
                    {
                        // Leave the rest untouched so they are still found by the next run.
                        result.QuotaReached = true;
                        break;
                    }

                    try
                    {
                        var previous = website.Thumbnail;
                        var rendered = await _screenshots.CaptureScreenshotAsync(website.Link ?? string.Empty, cancellationToken);

                        if (!string.IsNullOrEmpty(rendered))
                        {
                            website.Thumbnail = rendered;
                            result.EnrichedCount++;
                            result.ScreenshotsRendered++;
                        }
                        else
                        {
                            // A dead provider link or an animated placeholder is worse than no image.
                            website.Thumbnail = null;
                            result.SkippedCount++;
                        }

                        await _context.SaveChangesAsync(cancellationToken);
                        // Storage ignores URLs outside our bucket, so a provider URL is simply dropped.
                        await _thumbnailStorage.DeleteAsync(previous);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result.FailedCount++;
                        result.Errors.Add($"Failed to repair thumbnail for '{website.Title}' ({website.Link}): {ex.Message}");
                        _logger.LogWarning(ex, "Failed to repair thumbnail for website {Id}", website.Id);
                    }
                }

                var parts = new List<string>();
                if (result.QuotaReached)
                    parts.Add($"Screenshot quota reached; {result.TotalProcessed - result.EnrichedCount - result.SkippedCount - result.FailedCount} thumbnail(s) were left for a later run.");
                if (result.SkippedCount > 0)
                    parts.Add($"{result.SkippedCount} thumbnail(s) could not be re-rendered and were cleared.");
                if (result.FailedCount > 0)
                    parts.Add($"{result.FailedCount} of {result.TotalProcessed} repairs failed.");
                result.WarningMessage = parts.Count == 0 ? null : string.Join(" ", parts);

                result.CompletedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Website thumbnail repair complete. Re-rendered: {Rendered}, Cleared: {Cleared}, Failed: {Failed}, QuotaReached: {Quota}",
                    result.EnrichedCount, result.SkippedCount, result.FailedCount, result.QuotaReached);
            }
            catch (OperationCanceledException)
            {
                result.WasCancelled = true;
                result.CompletedAt = DateTime.UtcNow;
                _logger.LogInformation("Website thumbnail repair was canceled");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Thumbnail repair run failed: {ex.Message}";
                result.CompletedAt = DateTime.UtcNow;
                _logger.LogError(ex, "Website thumbnail repair run failed");
            }

            return result;
        }

        #endregion

        private int ClampLimit(int limit) => Math.Clamp(limit, 1, Math.Max(1, _options.MaxLimit));
    }
}
