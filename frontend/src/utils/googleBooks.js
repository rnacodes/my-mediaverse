// The Google Books page for a volume. Google's branding rules require every displayed
// book that uses Google Books data to link back to its Google Books page.
export const getGoogleBooksUrl = (volumeId) =>
    `https://books.google.com/books?id=${encodeURIComponent(volumeId)}`;
