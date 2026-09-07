using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using MyMediaVerse.Application.Utilities;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestHelpers;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class GenreResolverTests : InMemoryDbTestBase
    {
        [Fact]
        public async Task GetOrCreateAsync_ReturnsTheExistingGenre_AndCreatesNothing()
        {
            Context.Genres.Add(new Genre { Name = "blog" });
            await Context.SaveChangesAsync();
            var resolver = new GenreResolver(Context);

            var genre = await resolver.GetOrCreateAsync("blog");

            genre!.Name.Should().Be("blog");
            resolver.CreatedCount.Should().Be(0);
        }

        [Fact]
        public async Task GetOrCreateAsync_CreatesOnce_ForANameSeenManyTimesBeforeSaving()
        {
            var resolver = new GenreResolver(Context);

            var first = await resolver.GetOrCreateAsync("news");
            var second = await resolver.GetOrCreateAsync("news");
            await Context.SaveChangesAsync();

            second.Should().BeSameAs(first);
            resolver.CreatedCount.Should().Be(1);
            (await Context.Genres.CountAsync(g => g.Name == "news")).Should().Be(1);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task GetOrCreateAsync_ReturnsNull_ForBlankNames(string name)
        {
            (await new GenreResolver(Context).GetOrCreateAsync(name)).Should().BeNull();
        }

        [Fact]
        public async Task GetOrCreateAsync_ReturnsNull_ForNamesTheColumnCannotHold()
        {
            (await new GenreResolver(Context).GetOrCreateAsync(new string('g', 101))).Should().BeNull();
        }
    }
}
