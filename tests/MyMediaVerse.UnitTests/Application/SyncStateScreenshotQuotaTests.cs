using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MyMediaVerse.Application.Services;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Shared.Configuration;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class SyncStateScreenshotQuotaTests : InMemoryDbTestBase
    {
        private static readonly DateTime March = new(2026, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime April = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);

        private SyncStateScreenshotQuota CreateQuota(int cap, DateTime now) =>
            new(Context, Options.Create(new WebsiteScreenshotOptions { MonthlyCap = cap }), () => now);

        [Fact]
        public async Task RemainingAsync_IsTheFullCap_ForAFreshMonth()
        {
            (await CreateQuota(900, March).RemainingAsync()).Should().Be(900);
        }

        [Fact]
        public async Task TryReserveAsync_CreatesTheMonthRow_AndCountsUp()
        {
            var quota = CreateQuota(900, March);

            (await quota.TryReserveAsync()).Should().BeTrue();
            (await quota.TryReserveAsync()).Should().BeTrue();

            var row = await Context.SyncStates.SingleAsync();
            row.Key.Should().Be("website-screenshots:2026-03");
            row.Value.Should().Be("2");
            (await quota.RemainingAsync()).Should().Be(898);
        }

        [Fact]
        public async Task TryReserveAsync_RefusesAtTheCap_WithoutTouchingTheRow()
        {
            Context.SyncStates.Add(new SyncState { Key = "website-screenshots:2026-03", Value = "3" });
            await Context.SaveChangesAsync();
            var quota = CreateQuota(3, March);

            (await quota.RemainingAsync()).Should().Be(0);
            (await quota.TryReserveAsync()).Should().BeFalse();
            (await Context.SyncStates.SingleAsync()).Value.Should().Be("3");
        }

        [Fact]
        public async Task ANewMonth_StartsFromZero()
        {
            Context.SyncStates.Add(new SyncState { Key = "website-screenshots:2026-03", Value = "900" });
            await Context.SaveChangesAsync();

            var april = CreateQuota(900, April);

            (await april.RemainingAsync()).Should().Be(900);
            (await april.TryReserveAsync()).Should().BeTrue();
            (await Context.SyncStates.CountAsync()).Should().Be(2);
        }

        [Fact]
        public async Task AnUnparseableValue_CountsAsZero()
        {
            Context.SyncStates.Add(new SyncState { Key = "website-screenshots:2026-03", Value = "not a number" });
            await Context.SaveChangesAsync();

            (await CreateQuota(10, March).RemainingAsync()).Should().Be(10);
        }
    }
}
