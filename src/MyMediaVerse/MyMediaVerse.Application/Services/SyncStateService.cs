using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;

namespace MyMediaVerse.Application.Services
{
    public class SyncStateService : ISyncStateService
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<SyncStateService> _logger;

        public SyncStateService(IApplicationDbContext context, ILogger<SyncStateService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DateTime?> GetLastSuccessfulSyncAsync(string key)
        {
            var state = await _context.SyncStates
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Key == key);

            return state?.LastSuccessfulSyncAt;
        }

        public async Task<IncrementalSyncWindow> GetIncrementalWindowAsync(
            string key, DateTime now, TimeSpan defaultLookBack, TimeSpan overlap)
        {
            var cursor = await GetLastSuccessfulSyncAsync(key);

            if (cursor.HasValue)
            {
                var since = cursor.Value - overlap;
                _logger.LogInformation(
                    "Incremental window for '{Key}' from cursor {Cursor:u} (overlap {Overlap}): since {Since:u}",
                    key, cursor.Value, overlap, since);
                return new IncrementalSyncWindow(since, SyncWindowSource.Cursor);
            }

            var fallback = now - defaultLookBack;
            _logger.LogInformation(
                "No sync cursor for '{Key}'; using default look-back of {LookBack}: since {Since:u}",
                key, defaultLookBack, fallback);
            return new IncrementalSyncWindow(fallback, SyncWindowSource.Default);
        }

        public async Task MarkSyncSucceededAsync(string key, DateTime runStartedAt)
        {
            var state = await _context.SyncStates.FirstOrDefaultAsync(s => s.Key == key);

            if (state == null)
            {
                state = new SyncState
                {
                    Key = key,
                    LastSuccessfulSyncAt = runStartedAt,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Add(state);
            }
            else
            {
                if (state.LastSuccessfulSyncAt.HasValue && state.LastSuccessfulSyncAt.Value > runStartedAt)
                {
                    _logger.LogWarning(
                        "Not moving sync cursor for '{Key}' backwards from {Existing:u} to {Requested:u}",
                        key, state.LastSuccessfulSyncAt.Value, runStartedAt);
                    return;
                }

                state.LastSuccessfulSyncAt = runStartedAt;
                state.UpdatedAt = DateTime.UtcNow;
                _context.Update(state);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Sync cursor for '{Key}' set to {Cursor:u}", key, runStartedAt);
        }
    }
}
