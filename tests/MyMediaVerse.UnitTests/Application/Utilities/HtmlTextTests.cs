using AwesomeAssertions;
using MyMediaVerse.Application.Utilities;
using Xunit;

namespace MyMediaVerse.UnitTests.Application.Utilities
{
    public class HtmlTextTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("<p></p>")]
        [InlineData("<br/> <b> </b>")]
        public void Strip_NullEmptyOrMarkupOnly_ReturnsNull(string? html)
        {
            HtmlText.Strip(html).Should().BeNull();
        }

        [Fact]
        public void Strip_RemovesTagsAndCollapsesWhitespace()
        {
            var html = "<p>A  <b>bold</b>\n  statement<br/>here</p>";

            HtmlText.Strip(html).Should().Be("A bold statement here");
        }

        [Fact]
        public void Strip_ClosesGapBeforePunctuationLeftByInlineTags()
        {
            HtmlText.Strip("This is <i>important</i>, really<b>!</b>").Should().Be("This is important, really!");
        }

        [Fact]
        public void Strip_DecodesCommonEntities()
        {
            var html = "Tom &amp; Jerry &lt;3 &quot;cats&quot; &#39;n&#39; mice&nbsp;too";

            HtmlText.Strip(html).Should().Be("Tom & Jerry <3 \"cats\" 'n' mice too");
        }

        [Fact]
        public void Strip_DecodesNamedDecimalAndHexEntities()
        {
            var html = "caf&eacute; &#8212; na&#xEF;ve &copy;";

            HtmlText.Strip(html).Should().Be("café — naïve ©");
        }
    }
}
