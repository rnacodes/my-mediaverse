using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.Infrastructure.Models;
using MyMediaVerse.Infrastructure.Services.Search;
using MyMediaVerse.UnitTests.TestHelpers;
using NSubstitute;
using Typesense;

namespace MyMediaVerse.UnitTests.Infrastructure
{
    /// <summary>
    /// A bulk reindex used to report only a failure count. These tests pin that each rejected
    /// document is logged with its id and Typesense's reason, that the document body is never
    /// logged, and that Typesense is asked to return ids so the log can name the document.
    /// </summary>
    [Trait("Category", "Unit")]
    public class TypesenseImportFailureLoggingTests : InMemoryDbTestBase
    {
        private readonly ITypesenseClient _client = Substitute.For<ITypesenseClient>();
        private readonly CapturingLogger _logger = new();
        private readonly TypesenseService _service;

        public TypesenseImportFailureLoggingTests()
        {
            var config = new ConfigurationBuilder().Build();
            _service = new TypesenseService(_client, Context, _logger, config);

            // The reindex ends with orphan reconciliation; an empty index means nothing to remove.
            _client.ExportDocuments<TypesenseService.IdProjection>(Arg.Any<string>(), Arg.Any<ExportParameters>())
                .Returns(new List<TypesenseService.IdProjection>());
        }

        private async Task<List<Note>> SeedNotesAsync(int count)
        {
            var notes = Enumerable.Range(1, count).Select(i => new Note
            {
                Slug = $"note-{i}",
                Title = $"Note {i}",
                VaultName = "general",
                Content = $"SECRET-BODY-{i}"
            }).ToList();
            Context.Notes.AddRange(notes);
            await Context.SaveChangesAsync();
            Context.ChangeTracker.Clear();
            return notes;
        }

        private void RejectEveryOtherNote(string error)
        {
            _client.ImportDocuments(
                    Arg.Any<string>(), Arg.Any<IEnumerable<ObsidianNoteDocument>>(), Arg.Any<int>(),
                    Arg.Any<ImportType>(), Arg.Any<int?>(), Arg.Any<bool?>())
                .Returns(call => call.Arg<IEnumerable<ObsidianNoteDocument>>()
                    .Select((d, i) => i % 2 == 0
                        ? new ImportResponse(true, null!, null!, d.Id)
                        : new ImportResponse(false, error, null!, d.Id))
                    .ToList());
        }

        [Fact]
        public async Task BulkReindexAllNotesAsync_LogsEachRejectedNoteWithItsIdAndReason()
        {
            var notes = await SeedNotesAsync(4);
            RejectEveryOtherNote("Error embedding document: token limit exceeded");

            var indexed = await _service.BulkReindexAllNotesAsync();

            indexed.Should().Be(2);
            var rejections = _logger.Warnings.Where(m => m.StartsWith("Typesense rejected document")).ToList();
            rejections.Should().HaveCount(2);
            rejections.Should().AllSatisfy(m =>
            {
                m.Should().Contain("token limit exceeded");
                m.Should().Contain("obsidian_notes");
                m.Should().NotContain("SECRET-BODY");
            });
            var rejectedIds = notes.OrderBy(n => n.Id).Where((_, i) => i % 2 == 1).Select(n => n.Id.ToString());
            foreach (var id in rejectedIds)
            {
                rejections.Should().Contain(m => m.Contains(id));
            }
        }

        [Fact]
        public async Task BulkReindexAllNotesAsync_AsksTypesenseToReturnDocumentIds()
        {
            await SeedNotesAsync(1);
            RejectEveryOtherNote("unused");

            await _service.BulkReindexAllNotesAsync();

            await _client.Received().ImportDocuments(
                Arg.Any<string>(), Arg.Any<IEnumerable<ObsidianNoteDocument>>(), Arg.Any<int>(),
                ImportType.Upsert, Arg.Any<int?>(), true);
        }

        [Fact]
        public async Task BulkReindexAllNotesAsync_TruncatesVeryLongErrors()
        {
            await SeedNotesAsync(2);
            RejectEveryOtherNote(new string('x', 5000));

            await _service.BulkReindexAllNotesAsync();

            var rejection = _logger.Warnings.Single(m => m.StartsWith("Typesense rejected document"));
            rejection.Length.Should().BeLessThan(700);
        }

        /// <summary>Records formatted log messages so a test can assert on what was logged.</summary>
        private sealed class CapturingLogger : ILogger<TypesenseService>
        {
            private readonly List<(LogLevel Level, string Message)> _entries = new();

            public IEnumerable<string> Warnings =>
                _entries.Where(e => e.Level == LogLevel.Warning).Select(e => e.Message);

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
                Func<TState, Exception?, string> formatter) =>
                _entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
