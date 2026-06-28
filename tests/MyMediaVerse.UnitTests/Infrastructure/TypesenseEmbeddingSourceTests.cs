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

            // Tags are emitted in sorted order (notes < pkm) for deterministic embedding text.
            doc.EmbeddingSource.Should().Be("Zettelkasten\nNote-taking method\nAtomic notes linked together\nnotes, pkm\ngeneral");
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

        // ---------- Determinism ----------
        // The list fields (topics/genres/tags/titles) arrive in unstable order from EF navigation
        // collections and SelectMany().Distinct(). EmbeddingSource must sort them so an unchanged
        // item always yields a byte-identical string — that is what lets Typesense skip the paid
        // re-embedding call on an upsert when nothing actually changed.

        [Fact]
        public void MediaItem_EmbeddingSource_IsIndependentOfTopicAndGenreOrder()
        {
            var a = new MediaItemDocument
            {
                Id = "1", Title = "Dune", MediaType = "Book", Status = "Completed", Description = "epic",
                Topics = new List<string> { "ecology", "politics", "religion" },
                Genres = new List<string> { "sci-fi", "adventure" }
            };
            var b = new MediaItemDocument
            {
                Id = "1", Title = "Dune", MediaType = "Book", Status = "Completed", Description = "epic",
                Topics = new List<string> { "religion", "ecology", "politics" },
                Genres = new List<string> { "adventure", "sci-fi" }
            };

            a.EmbeddingSource.Should().Be(b.EmbeddingSource);
        }

        [Fact]
        public void Mixlist_EmbeddingSource_IsIndependentOfAggregatedListOrder()
        {
            var a = new MixlistDocument
            {
                Id = "1", Name = "Mix", Description = "d",
                Topics = new List<string> { "focus", "calm" },
                Genres = new List<string> { "ambient", "lofi" },
                MediaItemTitles = new List<string> { "Track B", "Track A" }
            };
            var b = new MixlistDocument
            {
                Id = "1", Name = "Mix", Description = "d",
                Topics = new List<string> { "calm", "focus" },
                Genres = new List<string> { "lofi", "ambient" },
                MediaItemTitles = new List<string> { "Track A", "Track B" }
            };

            a.EmbeddingSource.Should().Be(b.EmbeddingSource);
        }

        [Fact]
        public void Note_EmbeddingSource_IsIndependentOfTagOrder()
        {
            var a = new ObsidianNoteDocument
            {
                Id = "1", Slug = "s", Title = "T", VaultName = "general",
                Tags = new List<string> { "pkm", "notes", "zettel" }
            };
            var b = new ObsidianNoteDocument
            {
                Id = "1", Slug = "s", Title = "T", VaultName = "general",
                Tags = new List<string> { "zettel", "pkm", "notes" }
            };

            a.EmbeddingSource.Should().Be(b.EmbeddingSource);
        }

        [Fact]
        public void Highlight_EmbeddingSource_IsIndependentOfTagOrder()
        {
            var a = new HighlightDocument
            {
                Id = "1", Text = "t", Tags = new List<string> { "philosophy", "ethics" }
            };
            var b = new HighlightDocument
            {
                Id = "1", Text = "t", Tags = new List<string> { "ethics", "philosophy" }
            };

            a.EmbeddingSource.Should().Be(b.EmbeddingSource);
        }
    }
}
