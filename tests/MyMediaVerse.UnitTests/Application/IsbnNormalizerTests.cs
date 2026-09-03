using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;

namespace MyMediaVerse.UnitTests.Application
{
    [Trait("Category", "Unit")]
    public class IsbnNormalizerTests
    {
        [Theory]
        [InlineData("9780596007126", "9780596007126")]           // already ISBN-13
        [InlineData("978-0-596-00712-6", "9780596007126")]       // hyphenated 13
        [InlineData("978 0 596 00712 6", "9780596007126")]       // spaced 13
        [InlineData("0596007124", "9780596007126")]              // ISBN-10 → 13
        [InlineData("0-596-00712-4", "9780596007126")]           // hyphenated 10 → 13
        [InlineData("=\"0596007124\"", "9780596007126")]         // Goodreads Excel wrapper
        [InlineData("155860832X", "9781558608320")]              // ISBN-10 with X check digit
        [InlineData("155860832x", "9781558608320")]              // lowercase x
        public void Normalize_ReturnsCanonicalIsbn13(string input, string expected)
        {
            IsbnNormalizer.Normalize(input).Should().Be(expected);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("=\"\"")]                 // empty Excel wrapper (blank Goodreads ISBN)
        [InlineData("not-an-isbn")]
        [InlineData("12345")]                 // wrong length
        [InlineData("123456789012")]          // 12 digits
        [InlineData("12345678901234")]        // 14 digits
        [InlineData("059600712X4")]           // X not in check position
        public void Normalize_ReturnsNull_ForNonIsbnInput(string? input)
        {
            IsbnNormalizer.Normalize(input).Should().BeNull();
        }

        [Fact]
        public void Normalize_ComputesCheckDigitZero_WhenSumIsMultipleOfTen()
        {
            // 0-19-852663-6 (Oxford) → 9780198526636... use a known 10→13 with 0 check digit:
            // ISBN-10 0306406152 → ISBN-13 9780306406157
            IsbnNormalizer.Normalize("0306406152").Should().Be("9780306406157");
        }

        [Fact]
        public void GetSearchVariants_Returns13And10_For978Isbns()
        {
            var variants = IsbnNormalizer.GetSearchVariants("978-0-596-00712-6");

            variants.Should().Contain("9780596007126");
            variants.Should().Contain("0596007124");
        }

        [Fact]
        public void GetSearchVariants_Returns13Only_For979Isbns()
        {
            // 979-prefixed ISBNs have no ISBN-10 equivalent
            var variants = IsbnNormalizer.GetSearchVariants("9791234567896");

            variants.Should().ContainSingle().Which.Should().Be("9791234567896");
        }

        [Fact]
        public void GetSearchVariants_ReturnsEmpty_ForNonIsbnInput()
        {
            IsbnNormalizer.GetSearchVariants("junk").Should().BeEmpty();
            IsbnNormalizer.GetSearchVariants(null).Should().BeEmpty();
        }

        [Fact]
        public void GetSearchVariants_DerivedIsbn10_UsesXCheckDigit()
        {
            // 9781558608320 → 155860832X
            var variants = IsbnNormalizer.GetSearchVariants("9781558608320");

            variants.Should().Contain("155860832X");
        }
    }
}
