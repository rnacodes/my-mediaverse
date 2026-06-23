using AwesomeAssertions;
using MyMediaVerse.Infrastructure.Models;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// Unit tests for the computed <c>EmbeddingSource</c> property on each Typesense document model.
    /// This is the text Typesense auto-embeds, so it must compose the meaningful fields, skip
    /// empty/null values, and skip empty collections.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesenseEmbeddingSourceTests
    {
        // ---------- MediaItemDocument ----------

        [Fact]
        public void MediaItem_EmbeddingSource_ComposesMeaningfulFields()
        {
            var doc = new MediaItemDocument
            {
                Id = "1",
                Title = "Dune",
                MediaType = "Book",
                Status = "Completed",
                Description = "A desert epic",
                Topics = new List<string> { "ecology", "politics" },
                Genres = new List<string> { "sci-fi" }
            };

            doc.EmbeddingSource.Should().Be("Dune\nBook\nA desert epic\necology, politics\nsci-fi");
        }

        [Fact]
        public void MediaItem_EmbeddingSource_SkipsNullDescriptionAndEmptyCollections()
        {
            var doc = new MediaItemDocument
            {
                Id = "1",
                Title = "Untitled",
                MediaType = "Article",
                Status = "Uncharted",
                Description = null,
                Topics = new List<string>(),
                Genres = new List<string>()
            };

            doc.EmbeddingSource.Should().Be("Untitled\nArticle");
        }

        // ---------- MixlistDocument ----------

        [Fact]
        public void Mixlist_EmbeddingSource_ComposesMeaningfulFields()
        {
            var doc = new MixlistDocument
            {
                Id = "1",
                Name = "Focus Mix",
                Description = "For deep work",
                Topics = new List<string> { "productivity" },
                Genres = new List<string> { "ambient" },
                MediaItemTitles = new List<string> { "Track A", "Track B" }
            };

            doc.EmbeddingSource.Should().Be("Focus Mix\nFor deep work\nproductivity\nambient\nTrack A, Track B");
        }

        [Fact]
        public void Mixlist_EmbeddingSource_SkipsEmptyCollectionsAndNullDescription()
        {
            var doc = new MixlistDocument
            {
                Id = "1",
                Name = "Empty Mix",
                Description = null
            };

            doc.EmbeddingSource.Should().Be("Empty Mix");
        }

        // ---------- ObsidianNoteDocument ----------

        [Fact]
        public void Note_EmbeddingSource_ComposesMeaningfulFields()
        {
            var doc = new ObsidianNoteDocument
            {
                Id = "1",
                Slug = "zettelkasten",
                Title = "Zettelkasten",
                Description = "Note-taking method",
                Content = "Atomic notes linked together",
                VaultName = "general",
                Tags = new List<string> { "pkm", "notes" }
            };

            doc.EmbeddingSource.Should().Be("Zettelkasten\nNote-taking method\nAtomic notes linked together\npkm, notes\ngeneral");
        }

        [Fact]
        public void Note_EmbeddingSource_SkipsNullsButKeepsRequiredVault()
        {
            var doc = new ObsidianNoteDocument
            {
                Id = "1",
                Slug = "stub",
                Title = "Stub",
                Description = null,
                Content = null,
                VaultName = "programming"
            };

            doc.EmbeddingSource.Should().Be("Stub\nprogramming");
        }

        // ---------- HighlightDocument ----------

        [Fact]
        public void Highlight_EmbeddingSource_ComposesMeaningfulFields()
        {
            var doc = new HighlightDocument
            {
                Id = "1",
                Text = "The unexamined life is not worth living",
                Note = "Socrates",
                Title = "Apology",
                Author = "Plato",
                Tags = new List<string> { "philosophy" }
            };

            doc.EmbeddingSource.Should().Be(
                "The unexamined life is not worth living\nSocrates\nApology\nPlato\nphilosophy");
        }

        [Fact]
        public void Highlight_EmbeddingSource_SkipsNullsAndEmptyTags()
        {
            var doc = new HighlightDocument
            {
                Id = "1",
                Text = "Just the text",
                Note = null,
                Title = null,
                Author = null
            };

            doc.EmbeddingSource.Should().Be("Just the text");
        }
    }
}
