// The Google Books page for a volume. Google's branding rules require every displayed
// book that uses Google Books data to link back to its Google Books page.
export const getGoogleBooksUrl = (volumeId) =>
    `https://books.google.com/books?id=${encodeURIComponent(volumeId)}`;

const UNKNOWN_AUTHOR = 'Unknown Author';

export const getGoogleBooksLink = (book) => {
    if (book?.googleVolumeId) {
        return { url: getGoogleBooksUrl(book.googleVolumeId), exact: true };
    }

    const isbn = (book?.isbn || '').replace(/[^0-9Xx]/g, '');
    if (isbn) {
        return { url: `https://books.google.com/books?vid=ISBN${isbn}`, exact: true };
    }

    const author = book?.author && book.author !== UNKNOWN_AUTHOR ? book.author : '';
    const query = [book?.title, author].filter(Boolean).join(' ');
    return {
        url: `https://www.google.com/search?tbm=bks&q=${encodeURIComponent(query)}`,
        exact: false
    };
};
