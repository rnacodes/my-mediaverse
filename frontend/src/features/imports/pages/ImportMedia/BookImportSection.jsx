import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
    TextField, Button, Box, Typography,
    Select, MenuItem, InputLabel, FormControl,
    Card, CardContent, CircularProgress, Alert, Chip,
    Accordion, AccordionSummary, AccordionDetails
} from '@mui/material';
import { Search, Download, MenuBook, ExpandMore, OpenInNew } from '@mui/icons-material';
import {
    searchBooksFromGoogleBooks, importBookFromGoogleBooks,
    searchBooksFromOpenLibrary, importBookFromOpenLibrary
} from '@/api/bookService';
import WhiteOutlineButton from '@/shared/WhiteOutlineButton';

const BOOK_SOURCES = {
    googlebooks: {
        label: 'Google Books',
        homeUrl: 'https://books.google.com',
        getDetailsUrl: (key) => `https://books.google.com/books?id=${key}`
    },
    openlibrary: {
        label: 'Open Library',
        homeUrl: 'https://openlibrary.org',
        // Open Library keys are paths like "/works/OL45883W"
        getDetailsUrl: (key) => `https://openlibrary.org${key}`
    }
};

function BookImportSection({ expanded, onAccordionChange }) {
    const navigate = useNavigate();

    const [bookSource, setBookSource] = useState('googlebooks');
    const [bookImportMethod, setBookImportMethod] = useState('search');
    const [bookSearchQuery, setBookSearchQuery] = useState('');
    const [bookSearchType, setBookSearchType] = useState('General');
    const [bookIsbn, setBookIsbn] = useState('');
    const [bookTitle, setBookTitle] = useState('');
    const [bookAuthor, setBookAuthor] = useState('');
    const [bookSearchResults, setBookSearchResults] = useState([]);
    const [bookIsLoading, setBookIsLoading] = useState(false);
    const [bookError, setBookError] = useState('');
    const [bookSuccess, setBookSuccess] = useState('');
    const [displayedCount, setDisplayedCount] = useState(10);
    const [hasSearched, setHasSearched] = useState(false);

    const source = BOOK_SOURCES[bookSource];
    const searchBooks = bookSource === 'openlibrary' ? searchBooksFromOpenLibrary : searchBooksFromGoogleBooks;
    const importBook = bookSource === 'openlibrary' ? importBookFromOpenLibrary : importBookFromGoogleBooks;

    const handleBookSourceChange = (e) => {
        setBookSource(e.target.value);
        setBookSearchResults([]);
        setBookError('');
        setBookSuccess('');
        setHasSearched(false);
        setDisplayedCount(10);
    };

    const handleBookSearch = async () => {
        if (!bookSearchQuery.trim()) {
            setBookError('Please enter a search term');
            return;
        }

        setBookIsLoading(true);
        setBookError('');
        setBookSuccess('');
        setBookSearchResults([]);
        setDisplayedCount(10);
        setHasSearched(true);

        try {
            const searchParams = {
                query: bookSearchQuery,
                searchType: bookSearchType,
                limit: 50
            };

            const results = await searchBooks(searchParams);
            setBookSearchResults(results || []);
            setBookIsLoading(false);

        } catch (err) {
            console.error('Book search error:', err);
            setBookError('Failed to search books. Please try again.');
            setBookIsLoading(false);
        }
    };

    const handleLoadMore = () => {
        setDisplayedCount(prev => prev + 10);
    };

    const displayedResults = bookSearchResults.slice(0, displayedCount);

    const handleBookImportByIsbn = async () => {
        if (!bookIsbn.trim()) {
            setBookError('Please enter an ISBN');
            return;
        }

        setBookIsLoading(true);
        setBookError('');
        setBookSuccess('');

        try {
            console.log('Importing book by ISBN:', bookIsbn);

            const result = await importBook({ isbn: bookIsbn });

            console.log('Book import response:', result);

            setBookSuccess(`Book imported successfully!`);
            setBookIsLoading(false);
            setBookIsbn('');

            const mediaId = result.id || result.Id;
            if (mediaId) {
                setTimeout(() => {
                    navigate(`/media/${mediaId}`);
                }, 1500);
            }

        } catch (err) {
            console.error('Import by ISBN error:', err);
            console.error('Error response:', err.response?.data);

            let errorMessage = 'Failed to import book. ';
            if (err.response?.data?.error) {
                errorMessage += err.response.data.error;
            } else if (err.response?.data?.details) {
                errorMessage += err.response.data.details;
            } else if (err.message) {
                errorMessage += err.message;
            } else {
                errorMessage += 'Please check the ISBN and try again.';
            }

            setBookError(errorMessage);
            setBookIsLoading(false);
        }
    };

    const handleBookImportByTitleAuthor = async () => {
        if (!bookTitle.trim()) {
            setBookError('Please enter a book title');
            return;
        }

        setBookIsLoading(true);
        setBookError('');
        setBookSuccess('');

        try {
            const importData = {
                title: bookTitle,
                ...(bookAuthor.trim() && { author: bookAuthor })
            };

            console.log('Importing book by title/author:', importData);

            const result = await importBook(importData);

            console.log('Book import response:', result);

            setBookSuccess(`Book imported successfully!`);
            setBookIsLoading(false);
            setBookTitle('');
            setBookAuthor('');

            const mediaId = result.id || result.Id;
            if (mediaId) {
                setTimeout(() => {
                    navigate(`/media/${mediaId}`);
                }, 1500);
            }

        } catch (err) {
            console.error('Import by title/author error:', err);
            console.error('Error response:', err.response?.data);

            let errorMessage = 'Failed to import book. ';
            if (err.response?.data?.error) {
                errorMessage += err.response.data.error;
            } else if (err.response?.data?.details) {
                errorMessage += err.response.data.details;
            } else if (err.message) {
                errorMessage += err.message;
            } else {
                errorMessage += 'Please check the title and try again.';
            }

            setBookError(errorMessage);
            setBookIsLoading(false);
        }
    };

    const handleImportBook = async (book) => {
        setBookIsLoading(true);
        setBookError('');
        setBookSuccess('');

        try {
            console.log('Attempting to import book:', {
                key: book.key,
                title: book.title,
                authors: book.authors
            });

            const importData = bookSource === 'openlibrary'
                ? { openLibraryKey: book.key, title: book.title, author: book.authors?.[0] }
                : { volumeId: book.key, title: book.title, author: book.authors?.[0] };

            console.log('Import data being sent:', importData);

            const result = await importBook(importData);

            console.log('Book import response:', result);

            setBookSuccess(`"${book.title}" imported successfully!`);
            setBookIsLoading(false);

            const mediaId = result.id || result.Id;
            if (mediaId) {
                setTimeout(() => {
                    navigate(`/media/${mediaId}`);
                }, 1500);
            }

        } catch (err) {
            console.error('Import book error:', err);
            console.error('Error response:', err.response?.data);
            console.error('Error status:', err.response?.status);

            let errorMessage = 'Failed to import book. ';
            if (err.response?.data?.error) {
                errorMessage += err.response.data.error;
            } else if (err.response?.data?.details) {
                errorMessage += err.response.data.details;
            } else if (err.response?.data) {
                errorMessage += JSON.stringify(err.response.data);
            } else if (err.message) {
                errorMessage += err.message;
            } else {
                errorMessage += 'Please try again.';
            }

            setBookError(errorMessage);
            setBookIsLoading(false);
        }
    };

    return (
        <Accordion
            expanded={expanded === 'books'}
            onChange={onAccordionChange('books')}
            sx={{ mb: 2 }}
        >
            <AccordionSummary
                expandIcon={<ExpandMore />}
                aria-controls="books-content"
                id="books-header"
            >
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, flex: 1 }}>
                    <MenuBook />
                    <Typography variant="h6">
                        Books
                    </Typography>
                    <Box sx={{ ml: 'auto', display: 'flex', alignItems: 'center', gap: 1 }}>
                        <Typography variant="body2" color="text.secondary">
                            Powered by
                        </Typography>
                        <Button
                            variant="text"
                            size="small"
                            href={source.homeUrl}
                            target="_blank"
                            rel="noopener noreferrer"
                            endIcon={<OpenInNew fontSize="small" />}
                            sx={{
                                minWidth: 'auto',
                                textTransform: 'none',
                                color: '#ffffff',
                                '&:hover': { backgroundColor: 'transparent', textDecoration: 'underline' }
                            }}
                            onClick={(e) => e.stopPropagation()}
                        >
                            {source.label}
                        </Button>
                    </Box>
                </Box>
            </AccordionSummary>
            <AccordionDetails>
                <Box sx={{ padding: 2 }}>
                    <FormControl fullWidth margin="normal">
                        <InputLabel id="book-source-label">Source</InputLabel>
                        <Select
                            labelId="book-source-label"
                            value={bookSource}
                            label="Source"
                            onChange={handleBookSourceChange}
                        >
                            <MenuItem value="googlebooks">Google Books</MenuItem>
                            <MenuItem value="openlibrary">Open Library</MenuItem>
                        </Select>
                    </FormControl>

                    <FormControl fullWidth margin="normal">
                        <InputLabel>Import Method</InputLabel>
                        <Select
                            value={bookImportMethod}
                            label="Import Method"
                            onChange={(e) => setBookImportMethod(e.target.value)}
                        >
                            <MenuItem value="search">Search and Select</MenuItem>
                            <MenuItem value="isbn">By ISBN</MenuItem>
                            <MenuItem value="title">By Title/Author</MenuItem>
                        </Select>
                    </FormControl>

                    {bookImportMethod === 'search' && (
                        <Box>
                            <FormControl fullWidth margin="normal">
                                <InputLabel>Search Type</InputLabel>
                                <Select
                                    value={bookSearchType}
                                    label="Search Type"
                                    onChange={(e) => setBookSearchType(e.target.value)}
                                >
                                    <MenuItem value="General">General Search</MenuItem>
                                    <MenuItem value="Title">Title</MenuItem>
                                    <MenuItem value="Author">Author</MenuItem>
                                    <MenuItem value="ISBN">ISBN</MenuItem>
                                </Select>
                            </FormControl>

                            <Box sx={{ display: 'flex', gap: 2, mb: 2 }}>
                                <TextField
                                    label="Search Books"
                                    value={bookSearchQuery}
                                    onChange={(e) => setBookSearchQuery(e.target.value)}
                                    variant="outlined"
                                    fullWidth
                                    onKeyPress={(e) => e.key === 'Enter' && handleBookSearch()}
                                    InputLabelProps={{
                                        sx: { color: 'white' }
                                    }}
                                />
                                <Button
                                    variant="contained"
                                    onClick={handleBookSearch}
                                    disabled={bookIsLoading}
                                    startIcon={<Search />}
                                >
                                    Search
                                </Button>
                            </Box>

                            {hasSearched && !bookIsLoading && bookSearchResults.length === 0 && !bookError && (
                                <Alert severity="info" sx={{ mt: 2 }}>
                                    No results found. Try a different search term.
                                </Alert>
                            )}

                            {displayedResults.length > 0 && (
                                <Box sx={{ mt: 2 }}>
                                    <Typography variant="h6" gutterBottom>
                                        Search Results ({bookSearchResults.length})
                                    </Typography>
                                    {displayedResults.map((book, index) => (
                                        <Card key={book.key || index} sx={{ mb: 2 }}>
                                            <CardContent>
                                                <Box sx={{ display: 'flex', gap: 2 }}>
                                                    {book.coverUrl && (
                                                        <img
                                                            src={book.coverUrl}
                                                            alt={book.title}
                                                            style={{
                                                                width: 80,
                                                                height: 120,
                                                                objectFit: 'cover',
                                                                borderRadius: 4
                                                            }}
                                                            onError={(e) => {
                                                                e.target.style.display = 'none';
                                                            }}
                                                        />
                                                    )}
                                                    <Box sx={{ flex: 1 }}>
                                                        <Typography variant="h6" gutterBottom>
                                                            {book.title || 'Unknown Title'}
                                                        </Typography>
                                                        <Typography variant="body2" color="text.secondary" gutterBottom>
                                                            by {book.authors?.join(', ') || 'Unknown Author'}
                                                        </Typography>
                                                        {book.firstPublishYear && (
                                                            <Typography variant="body2" color="text.secondary" gutterBottom>
                                                                First published: {book.firstPublishYear}
                                                            </Typography>
                                                        )}
                                                        {book.subjects && book.subjects.length > 0 && (
                                                            <Box sx={{ mb: 1 }}>
                                                                {book.subjects.slice(0, 3).map((subject) => (
                                                                    <Chip
                                                                        key={`subj-${subject}`}
                                                                        label={subject}
                                                                        size="small"
                                                                        sx={{ mr: 0.5, mb: 0.5 }}
                                                                    />
                                                                ))}
                                                            </Box>
                                                        )}
                                                        {book.editionCount && (
                                                            <Chip
                                                                label={`${book.editionCount} editions`}
                                                                size="small"
                                                                sx={{ mb: 1 }}
                                                            />
                                                        )}
                                                        <Box sx={{ display: 'flex', gap: 1 }}>
                                                            <WhiteOutlineButton
                                                                size="small"
                                                                href={source.getDetailsUrl(book.key)}
                                                                target="_blank"
                                                                rel="noopener noreferrer"
                                                                endIcon={<OpenInNew fontSize="small" />}
                                                            >
                                                                View Details
                                                            </WhiteOutlineButton>
                                                            <Button
                                                                variant="contained"
                                                                size="small"
                                                                onClick={() => handleImportBook(book)}
                                                                disabled={bookIsLoading}
                                                                startIcon={<Download />}
                                                            >
                                                                Import
                                                            </Button>
                                                        </Box>
                                                    </Box>
                                                </Box>
                                            </CardContent>
                                        </Card>
                                    ))}
                                    {displayedResults.length < bookSearchResults.length && (
                                        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mt: 2 }}>
                                            <Typography variant="body2" color="text.secondary">
                                                Showing {displayedResults.length} of {bookSearchResults.length} results
                                            </Typography>
                                            <Button
                                                variant="contained"
                                                size="small"
                                                onClick={handleLoadMore}
                                            >
                                                Load 10 More
                                            </Button>
                                        </Box>
                                    )}
                                </Box>
                            )}
                        </Box>
                    )}

                    {bookImportMethod === 'isbn' && (
                        <Box sx={{ display: 'flex', gap: 2, mt: 2 }}>
                            <TextField
                                label="ISBN"
                                value={bookIsbn}
                                onChange={(e) => setBookIsbn(e.target.value)}
                                variant="outlined"
                                fullWidth
                                placeholder="978-0-123456-78-9"
                                onKeyPress={(e) => e.key === 'Enter' && handleBookImportByIsbn()}
                                InputLabelProps={{
                                    sx: { color: 'white' }
                                }}
                            />
                            <Button
                                variant="contained"
                                onClick={handleBookImportByIsbn}
                                disabled={bookIsLoading}
                                startIcon={<Download />}
                            >
                                Import
                            </Button>
                        </Box>
                    )}

                    {bookImportMethod === 'title' && (
                        <Box sx={{ mt: 2 }}>
                            <TextField
                                label="Book Title"
                                value={bookTitle}
                                onChange={(e) => setBookTitle(e.target.value)}
                                variant="outlined"
                                fullWidth
                                margin="normal"
                                InputLabelProps={{
                                    sx: { color: 'white' }
                                }}
                            />
                            <TextField
                                label="Author (Optional)"
                                value={bookAuthor}
                                onChange={(e) => setBookAuthor(e.target.value)}
                                variant="outlined"
                                fullWidth
                                margin="normal"
                                InputLabelProps={{
                                    sx: { color: 'white' }
                                }}
                            />
                            <Button
                                variant="contained"
                                onClick={handleBookImportByTitleAuthor}
                                disabled={bookIsLoading}
                                startIcon={<Download />}
                                sx={{ mt: 1 }}
                            >
                                Import
                            </Button>
                        </Box>
                    )}

                    {bookIsLoading && (
                        <Box sx={{ display: 'flex', justifyContent: 'center', mt: 2 }}>
                            <CircularProgress />
                        </Box>
                    )}

                    {bookError && (
                        <Alert severity="error" sx={{ mt: 2 }}>
                            {bookError}
                        </Alert>
                    )}

                    {bookSuccess && (
                        <Alert severity="success" sx={{ mt: 2 }}>
                            {bookSuccess}
                        </Alert>
                    )}

                    <Alert severity="info" sx={{ mt: 3 }} icon={<MenuBook />}>
                        <Typography variant="body2">
                            <strong>Book Descriptions:</strong> A background service runs every 48 hours to automatically
                            fetch descriptions from Google Books for books with an ISBN. Descriptions will be added
                            gradually after import.
                        </Typography>
                    </Alert>
                </Box>
            </AccordionDetails>
        </Accordion>
    );
}

export default BookImportSection;
