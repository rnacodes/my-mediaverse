using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.Shared.Interfaces;

namespace MyMediaVerse.Application.Services
{
    /// <summary>
    /// Monthly screenshot budget kept in the SyncState table: one row per calendar month
    /// (key "website-screenshots:yyyy-MM") whose Value is the number of renders so far. A new
    /// month simply starts a new row, so the cap resets without any scheduled job.
    /// </summary>
    public class SyncStateScreenshotQuota : IScreenshotQuota
    {
        public const string KeyPrefix = "website-screenshots:";

        private readonly IApplicationDbContext _context;
        private readonly WebsiteScreenshotOptions _options;
        private readonly Func<DateTime> _utcNow;

        public SyncStateScreenshotQuota(IApplicationDbContext context, IOptions<WebsiteScreenshotOptions> options)
            : this(context, options, () => DateTime.UtcNow)
        {
        }

        /// <summary>Test seam for the clock.</summary>
        public SyncStateScreenshotQuota(IApplicationDbContext context, IOptions<WebsiteScreenshotOptions> options, Func<DateTime> utcNow)
        {
            _context = context;
            _options = options.Value;
            _utcNow = utcNow;
        }

        public static string KeyFor(DateTime utcNow) => $"{KeyPrefix}{utcNow:yyyy-MM}";

        public async Task<int> RemainingAsync(CancellationToken cancellationToken = default)
        {
            var used = await ReadUsedAsync(cancellationToken);
            return Math.Max(0, _options.MonthlyCap - used);
        }

        public async Task<bool> TryReserveAsync(CancellationToken cancellationToken = default)
        {
            var key = KeyFor(_utcNow());
            var state = await _context.SyncStates.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
            var used = ParseCount(state?.Value);

            if (used >= _options.MonthlyCap)
                return false;

            if (state == null)
            {
                state = new SyncState { Key = key };
                _context.Add(state);
            }

            state.Value = (used + 1).ToString();
            state.UpdatedAt = _utcNow();
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        private async Task<int> ReadUsedAsync(CancellationToken cancellationToken)
        {
            var key = KeyFor(_utcNow());
            var value = await _context.SyncStates
                .Where(s => s.Key == key)
                .Select(s => s.Value)
                .FirstOrDefaultAsync(cancellationToken);
            return ParseCount(value);
        }

        private static int ParseCount(string? value) =>
            int.TryParse(value, out var count) && count > 0 ? count : 0;
    }
}
