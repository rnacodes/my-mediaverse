using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;

namespace MyMediaVerse.UnitTests.Application.Utilities
{
    [Trait("Category", "Unit")]
    public class DateTimeNormalizerTests
    {
        [Fact]
        public void ToUtc_LabelsUnspecifiedAsUtc_WithoutShiftingTheCalendarDay()
        {
            // Date-only JSON (e.g. "2020-03-15") deserializes to Kind=Unspecified.
            var unspecified = new DateTime(2020, 3, 15, 0, 0, 0, DateTimeKind.Unspecified);

            var result = DateTimeNormalizer.ToUtc(unspecified);

            result.Kind.Should().Be(DateTimeKind.Utc);
            result.Should().Be(new DateTime(2020, 3, 15, 0, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void ToUtc_ConvertsLocalToUtc()
        {
            var local = new DateTime(2020, 3, 15, 12, 0, 0, DateTimeKind.Local);

            var result = DateTimeNormalizer.ToUtc(local);

            result.Kind.Should().Be(DateTimeKind.Utc);
            result.Should().Be(local.ToUniversalTime());
        }

        [Fact]
        public void ToUtc_LeavesUtcUnchanged()
        {
            var utc = new DateTime(2020, 3, 15, 8, 30, 0, DateTimeKind.Utc);

            var result = DateTimeNormalizer.ToUtc(utc);

            result.Kind.Should().Be(DateTimeKind.Utc);
            result.Should().Be(utc);
        }

        [Fact]
        public void ToUtc_Nullable_ReturnsNull_WhenInputIsNull()
        {
            DateTime? input = null;

            var result = DateTimeNormalizer.ToUtc(input);

            result.Should().BeNull();
        }

        [Fact]
        public void ToUtc_Nullable_NormalizesValue_WhenInputHasValue()
        {
            DateTime? input = new DateTime(2021, 5, 1, 0, 0, 0, DateTimeKind.Unspecified);

            var result = DateTimeNormalizer.ToUtc(input);

            result.Should().NotBeNull();
            result!.Value.Kind.Should().Be(DateTimeKind.Utc);
            result.Value.Should().Be(new DateTime(2021, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}
