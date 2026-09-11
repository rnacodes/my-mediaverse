using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;

namespace MyMediaVerse.UnitTests.Application.Utilities
{
    [Trait("Category", "Unit")]
    public class UrlNormalizerTests
    {
        [Theory]
        [InlineData("HTTPS://Example.com/Article/", "https://example.com/Article")]
        [InlineData("https://example.com/article#section-2", "https://example.com/article")]
        [InlineData("https://example.com/article?utm_source=news&utm_medium=email", "https://example.com/article")]
        [InlineData("https://www.example.com/article", "https://example.com/article")]
        [InlineData("https://example.com/tech/?page=2", "https://example.com/tech?page=2")]
        public void Normalize_AppliesExpectedTransformations(string input, string expected)
        {
            UrlNormalizer.Normalize(input).Should().Be(expected);
        }

        [Fact]
        public void Normalize_KeepsPathAndQueryCase_BecauseManySitesAreCaseSensitiveThere()
        {
            UrlNormalizer.Normalize("https://www.youtube.com/watch?v=dQw4w9WgXcQ")
                .Should().Be("https://youtube.com/watch?v=dQw4w9WgXcQ");
            UrlNormalizer.Normalize("https://en.wikipedia.org/wiki/Bookmark_(digital)")
                .Should().Be("https://en.wikipedia.org/wiki/Bookmark_(digital)");
        }

        [Fact]
        public void GetComparisonKey_IsCaseInsensitive_AndIgnoresATrailingSlashBeforeTheQuery()
        {
            UrlNormalizer.GetComparisonKey("https://Example.com/Article?Id=42")
                .Should().Be(UrlNormalizer.GetComparisonKey("http://www.example.com/article/?id=42"))
                .And.Be("example.com/article?id=42");
        }

        [Fact]
        public void Normalize_PreservesScheme()
        {
            UrlNormalizer.Normalize("http://example.com/article").Should().Be("http://example.com/article");
            UrlNormalizer.Normalize("https://example.com/article").Should().Be("https://example.com/article");
        }

        [Fact]
        public void Normalize_PreservesMeaningfulQueryParameters()
        {
            UrlNormalizer.Normalize("https://example.com/article?id=42&utm_source=news")
                .Should().Be("https://example.com/article?id=42");
        }

        [Fact]
        public void Normalize_NullOrWhitespace_ReturnsEmptyString()
        {
            UrlNormalizer.Normalize(null).Should().BeEmpty();
            UrlNormalizer.Normalize("   ").Should().BeEmpty();
        }

        [Theory]
        [InlineData("http://example.com/article")]
        [InlineData("https://example.com/article")]
        [InlineData("https://www.example.com/article/")]
        public void GetComparisonKey_SchemeAndWwwVariants_ProduceSameKey(string url)
        {
            UrlNormalizer.GetComparisonKey(url).Should().Be("example.com/article");
        }

        [Fact]
        public void GetComparisonKey_DifferentPaths_ProduceDifferentKeys()
        {
            UrlNormalizer.GetComparisonKey("https://example.com/article-one")
                .Should().NotBe(UrlNormalizer.GetComparisonKey("https://example.com/article-two"));
        }

        [Theory]
        [InlineData("http://example.com/article", "https://example.com/article", true)]
        [InlineData("https://www.example.com/article", "https://example.com/article/", true)]
        [InlineData("https://example.com/article?utm_source=a", "https://example.com/article", true)]
        [InlineData("https://example.com/one", "https://example.com/two", false)]
        [InlineData(null, null, true)]
        [InlineData("https://example.com/article", null, false)]
        public void AreEquivalent_ComparesSchemeInsensitively(string? url1, string? url2, bool expected)
        {
            UrlNormalizer.AreEquivalent(url1, url2).Should().Be(expected);
        }
    }
}
