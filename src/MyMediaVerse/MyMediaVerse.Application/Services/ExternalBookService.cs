using MyMediaVerse.Application.Interfaces;
using MyMediaVerse.Domain.Entities;
using MyMediaVerse.DTOs;

namespace MyMediaVerse.Application.Services
{
    public class ExternalBookService : IExternalBookService
    {
        private readonly IGoogleBooksService _googleBooks;
        private readonly IBookMappingService _mapping;

        public ExternalBookService(IGoogleBooksService googleBooks, IBookMappingService mapping)
        {
            _googleBooks = googleBooks;
            _mapping = mapping;
        }

        public async Task<IEnumerable<BookSearchResultDto>> SearchAsync(SearchBooksDto query)
        {
            if (string.IsNullOrWhiteSpace(query.Query))
            {
                return Array.Empty<BookSearchResultDto>();
            }

            var result = query.SearchType switch
            {
                BookSearchType.Title => await _googleBooks.SearchBooksByTitleAsync(query.Query, query.Offset, query.Limit),
                BookSearchType.Author => await _googleBooks.SearchBooksByAuthorAsync(query.Query, query.Offset, query.Limit),
                BookSearchType.ISBN => await _googleBooks.SearchBooksByISBNAsync(query.Query),
                _ => await _googleBooks.SearchBooksAsync(query.Query, query.Offset, query.Limit),
            };

            if (result?.Items == null)
            {
                return Array.Empty<BookSearchResultDto>();
            }

            return await Task.WhenAll(result.Items.Select(_mapping.MapGoogleBooksToSearchResultDtoAsync));
        }

        public Task<Book> ImportAsync(ImportBookFromGoogleBooksDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.VolumeId))
            {
                return _googleBooks.ImportBookFromVolumeIdAsync(dto.VolumeId);
            }

            if (!string.IsNullOrWhiteSpace(dto.Isbn))
            {
                return _googleBooks.ImportBookFromISBNAsync(dto.Isbn);
            }

            if (!string.IsNullOrWhiteSpace(dto.Title))
            {
                return _googleBooks.ImportBookFromTitleAndAuthorAsync(dto.Title, dto.Author);
            }

            throw new ArgumentException("At least one of VolumeId, Isbn, or Title must be provided.", nameof(dto));
        }
    }
}
