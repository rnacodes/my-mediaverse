using AwesomeAssertions;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.UnitTests.TestData;

namespace MyMediaVerse.UnitTests.Domain
{
    [Trait("Category", "Unit")]
    public class DocumentTests
    {
        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var document = new Document { Title = "" };

            // Assert
            document.Id.Should().NotBeEmpty();
            document.PaperlessId.Should().BeNull();
            document.OriginalFileName.Should().BeNull();
            document.ArchiveSerialNumber.Should().BeNull();
            document.DocumentType.Should().BeNull();
            document.Correspondent.Should().BeNull();
            document.OcrContent.Should().BeNull();
            document.DocumentDate.Should().BeNull();
            document.PageCount.Should().BeNull();
            document.FileType.Should().BeNull();
            document.FileSizeBytes.Should().BeNull();
            document.PaperlessTagsCsv.Should().BeNull();
            document.CustomFieldsJson.Should().BeNull();
            document.LastPaperlessSync.Should().BeNull();
            document.PaperlessUrl.Should().BeNull();
            document.IsArchived.Should().BeFalse();
            document.Topics.Should().NotBeNull().And.BeEmpty();
            document.Genres.Should().NotBeNull().And.BeEmpty();
            document.Mixlists.Should().NotBeNull().And.BeEmpty();
        }

        #endregion

        #region Property Tests

        [Fact]
        public void Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            var testDate = DateTime.UtcNow;

            // Act
            document.Title = "Tax Return 2024";
            document.PaperlessId = 42;
            document.OriginalFileName = "tax-return-2024.pdf";
            document.ArchiveSerialNumber = "ASN-001";
            document.DocumentType = "Tax Document";
            document.Correspondent = "IRS";
            document.OcrContent = "This is OCR extracted text";
            document.DocumentDate = testDate;
            document.PageCount = 15;
            document.FileType = "pdf";
            document.FileSizeBytes = 2048576;
            document.PaperlessUrl = "https://paperless.example.com/documents/42";
            document.IsArchived = true;

            // Assert
            document.Title.Should().Be("Tax Return 2024");
            document.PaperlessId.Should().Be(42);
            document.OriginalFileName.Should().Be("tax-return-2024.pdf");
            document.ArchiveSerialNumber.Should().Be("ASN-001");
            document.DocumentType.Should().Be("Tax Document");
            document.Correspondent.Should().Be("IRS");
            document.OcrContent.Should().Be("This is OCR extracted text");
            document.DocumentDate.Should().Be(testDate);
            document.PageCount.Should().Be(15);
            document.FileType.Should().Be("pdf");
            document.FileSizeBytes.Should().Be(2048576);
            document.PaperlessUrl.Should().Be("https://paperless.example.com/documents/42");
            document.IsArchived.Should().BeTrue();
        }

        #endregion

        #region GetPaperlessTags Tests

        [Fact]
        public void GetPaperlessTags_WithTags_ShouldReturnList()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.PaperlessTagsCsv = "invoice,tax,2024";

            // Act
            var tags = document.GetPaperlessTags();

            // Assert
            tags.Should().HaveCount(3);
            tags.Should().Contain(new[] { "invoice", "tax", "2024" });
        }

        [Fact]
        public void GetPaperlessTags_WithNull_ShouldReturnEmptyList()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.PaperlessTagsCsv = null;

            // Act
            var tags = document.GetPaperlessTags();

            // Assert
            tags.Should().BeEmpty();
        }

        [Fact]
        public void GetPaperlessTags_WithEmpty_ShouldReturnEmptyList()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.PaperlessTagsCsv = "";

            // Act
            var tags = document.GetPaperlessTags();

            // Assert
            tags.Should().BeEmpty();
        }

        [Fact]
        public void GetPaperlessTags_ShouldTrimWhitespace()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.PaperlessTagsCsv = " invoice , tax , 2024 ";

            // Act
            var tags = document.GetPaperlessTags();

            // Assert
            tags.Should().Contain(new[] { "invoice", "tax", "2024" });
        }

        #endregion

        #region SetPaperlessTags Tests

        [Fact]
        public void SetPaperlessTags_WithTags_ShouldSetCsv()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();

            // Act
            document.SetPaperlessTags(new[] { "invoice", "tax", "2024" });

            // Assert
            document.PaperlessTagsCsv.Should().Be("invoice,tax,2024");
        }

        [Fact]
        public void SetPaperlessTags_WithEmptyList_ShouldSetNull()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.PaperlessTagsCsv = "existing,tags";

            // Act
            document.SetPaperlessTags(Array.Empty<string>());

            // Assert
            document.PaperlessTagsCsv.Should().BeNull();
        }

        [Fact]
        public void SetPaperlessTags_ShouldTrimWhitespace()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();

            // Act
            document.SetPaperlessTags(new[] { " invoice ", " tax " });

            // Assert
            document.PaperlessTagsCsv.Should().Be("invoice,tax");
        }

        #endregion

        #region GetFormattedFileSize Tests

        [Fact]
        public void GetFormattedFileSize_WithBytes_ShouldReturnFormattedString()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.FileSizeBytes = 512;

            // Act
            var size = document.GetFormattedFileSize();

            // Assert
            size.Should().Be("512 B");
        }

        [Fact]
        public void GetFormattedFileSize_WithKilobytes_ShouldReturnFormattedString()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.FileSizeBytes = 1024;

            // Act
            var size = document.GetFormattedFileSize();

            // Assert
            size.Should().Be("1 KB");
        }

        [Fact]
        public void GetFormattedFileSize_WithMegabytes_ShouldReturnFormattedString()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.FileSizeBytes = 1048576; // 1 MB

            // Act
            var size = document.GetFormattedFileSize();

            // Assert
            size.Should().Be("1 MB");
        }

        [Fact]
        public void GetFormattedFileSize_WithNull_ShouldReturnNull()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.FileSizeBytes = null;

            // Act
            var size = document.GetFormattedFileSize();

            // Assert
            size.Should().BeNull();
        }

        [Fact]
        public void GetFormattedFileSize_WithDecimalValue_ShouldRound()
        {
            // Arrange
            var document = TestDataFactory.CreateDocument();
            document.FileSizeBytes = 1536; // 1.5 KB

            // Act
            var size = document.GetFormattedFileSize();

            // Assert
            size.Should().Be("1.5 KB");
        }

        #endregion

        #region Inheritance Tests

        [Fact]
        public void InheritsFromBaseMediaItem_ShouldHaveBaseProperties()
        {
            // Arrange & Act
            var document = TestDataFactory.CreateDocument();

            // Assert
            Assert.IsAssignableFrom<BaseMediaItem>(document);
            document.MediaType.Should().Be(MediaType.Document);
        }

        #endregion
    }
}
