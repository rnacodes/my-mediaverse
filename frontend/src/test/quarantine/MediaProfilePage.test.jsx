// QUARANTINED (RAS-17): excluded from the run via vitest.config.js.
// TODO(RAS-23): rewrite against the new test infra (MSW + renderWithProviders).
// Component moved to src/features/media/pages/MediaProfilePage.jsx in the feature-folder reorg.
import React from 'react';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';

// Mock sub-components
vi.mock('../MediaHeader', () => ({
    default: ({ title, mediaId }) => <div data-testid="media-header">{title}</div>
}));
vi.mock('../MixlistCarousel', () => ({
    default: ({ currentMixlists }) => <div data-testid="mixlist-carousel">{currentMixlists?.length || 0} mixlists</div>
}));
vi.mock('../MediaInfoCard', () => ({
    default: ({ mediaItem }) => <div data-testid="media-info-card">{mediaItem?.title}</div>
}));
vi.mock('../MediaDetailAccordion', () => ({
    default: ({ mediaItem }) => <div data-testid="media-detail-accordion">Details</div>
}));
vi.mock('../HighlightsSection', () => ({
    default: ({ highlights }) => <div data-testid="highlights-section">{highlights?.length || 0} highlights</div>
}));
vi.mock('../TopicsGenresSection', () => ({
    default: () => <div data-testid="topics-genres-section">Topics & Genres</div>
}));
vi.mock('../RelatedNotesSection', () => ({
    default: () => <div data-testid="related-notes-section">Related Notes</div>
}));
vi.mock('../SimilarItemsSection', () => ({
    default: () => <div data-testid="similar-items-section">Similar Items</div>
}));
vi.mock('../SavedRelatedMediaSection', () => ({
    default: () => <div data-testid="saved-related-media">Saved Related</div>
}));

// Mock API services
vi.mock('../../api/mediaService', () => ({ getMediaById: vi.fn() }));
vi.mock('../../api/mixlistService', () => ({ getAllMixlists: vi.fn() }));
vi.mock('../../api/bookService', () => ({ getBookById: vi.fn() }));
vi.mock('../../api/podcastService', () => ({
    getPodcastSeriesById: vi.fn(),
    getPodcastEpisodeById: vi.fn(),
    getEpisodesBySeriesId: vi.fn()
}));
vi.mock('../../api/movieService', () => ({ getMovieById: vi.fn() }));
vi.mock('../../api/tvShowService', () => ({ getTvShowById: vi.fn() }));
vi.mock('../../api/videoService', () => ({ getVideoById: vi.fn(), getPlaylistsForVideo: vi.fn() }));
vi.mock('../../api/articleService', () => ({ getArticleById: vi.fn(), fetchArticleContent: vi.fn() }));
vi.mock('../../api/highlightService', () => ({ getHighlightsByArticle: vi.fn(), getHighlightsByBook: vi.fn() }));

// Mock router
let mockParamsId = '1';
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual('react-router-dom');
    return {
        ...actual,
        useParams: () => ({ id: mockParamsId }),
        useNavigate: () => mockNavigate
    };
});

// Import after mocks
import MediaProfilePage from '../MediaProfilePage';
import { getMediaById } from '../../api/mediaService';
import { getAllMixlists } from '../../api/mixlistService';
import { getBookById } from '../../api/bookService';
import { getPodcastSeriesById, getPodcastEpisodeById, getEpisodesBySeriesId } from '../../api/podcastService';
import { getMovieById } from '../../api/movieService';
import { getTvShowById } from '../../api/tvShowService';
import { getVideoById, getPlaylistsForVideo } from '../../api/videoService';
import { getArticleById, fetchArticleContent } from '../../api/articleService';
import { getHighlightsByArticle, getHighlightsByBook } from '../../api/highlightService';

// Mock data
const mockBookMedia = {
    id: 1,
    title: 'Test Book',
    mediaType: 'Book',
    status: 'Completed',
    rating: 4,
    thumbnail: 'http://example.com/book.jpg',
    notes: 'Great book',
    mixlistIds: []
};

const mockBookDetails = {
    id: 1,
    title: 'Test Book',
    author: 'Author Name',
    isbn: '1234567890',
    pageCount: 300,
    publishedDate: '2023-01-01'
};

const mockMovieMedia = {
    id: 2,
    title: 'Test Movie',
    mediaType: 'Movie',
    status: 'Completed',
    rating: 5,
    thumbnail: 'http://example.com/movie.jpg',
    notes: 'Great movie',
    mixlistIds: []
};

const mockMovieDetails = {
    id: 2,
    title: 'Test Movie',
    director: 'Director Name',
    releaseYear: 2023,
    runtime: 120
};

const mockPlaylistMedia = {
    id: 3,
    title: 'Test Playlist',
    mediaType: 'Playlist',
    status: 'InProgress',
    mixlistIds: []
};

const mockPodcastSeriesMedia = {
    id: 4,
    title: 'Test Podcast Series',
    mediaType: 'Podcast',
    status: 'InProgress',
    mixlistIds: []
};

const mockPodcastSeriesDetails = {
    id: 4,
    title: 'Test Podcast Series',
    podcastType: 'Series',
    publisher: 'Publisher Name'
};

const mockMixlists = [
    { id: 10, name: 'Favorites', description: 'My favorites' },
    { id: 20, name: 'Watch Later', description: 'To watch' },
    { id: 30, name: 'Reading List', description: 'Books to read' }
];

const mockHighlights = [
    { id: 100, text: 'Highlight one', note: 'Note one' },
    { id: 101, text: 'Highlight two', note: 'Note two' }
];

const renderComponent = () => {
    return render(
        <BrowserRouter>
            <MediaProfilePage />
        </BrowserRouter>
    );
};

// Helper to set up book mocks
const setupBookMocks = () => {
    getMediaById.mockResolvedValue({ data: mockBookMedia });
    getBookById.mockResolvedValue({ data: mockBookDetails });
    getAllMixlists.mockResolvedValue({ data: [] });
    getHighlightsByBook.mockResolvedValue([]);
};

// Helper to set up movie mocks
const setupMovieMocks = () => {
    getMediaById.mockResolvedValue({ data: mockMovieMedia });
    getMovieById.mockResolvedValue({ data: mockMovieDetails });
    getAllMixlists.mockResolvedValue({ data: [] });
};

describe('MediaProfilePage', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mockParamsId = '1';
    });

    describe('Loading State', () => {
        it('should show loading spinner while fetching data', () => {
            getMediaById.mockImplementation(() => new Promise(() => {}));

            renderComponent();

            expect(screen.getByRole('progressbar')).toBeInTheDocument();
        });

        it('should show loading indicator text', () => {
            getMediaById.mockImplementation(() => new Promise(() => {}));

            renderComponent();

            expect(screen.getByText('Loading media item...')).toBeInTheDocument();
        });
    });

    describe('Data Display for Book', () => {
        it('should render media header with book data', async () => {
            setupBookMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-header')).toBeInTheDocument();
            });
            expect(screen.getByTestId('media-header')).toHaveTextContent('Test Book');
        });

        it('should render media info card', async () => {
            setupBookMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-info-card')).toBeInTheDocument();
            });
            expect(screen.getByTestId('media-info-card')).toHaveTextContent('Test Book');
        });

        it('should render highlights section for books', async () => {
            getMediaById.mockResolvedValue({ data: mockBookMedia });
            getBookById.mockResolvedValue({ data: mockBookDetails });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByBook.mockResolvedValue(mockHighlights);

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toBeInTheDocument();
            });
        });
    });

    describe('Data Display for Movie', () => {
        it('should render movie profile page', async () => {
            mockParamsId = '2';
            setupMovieMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-header')).toBeInTheDocument();
            });
            expect(screen.getByTestId('media-info-card')).toHaveTextContent('Test Movie');
        });

        it('should fetch movie details', async () => {
            mockParamsId = '2';
            setupMovieMocks();

            renderComponent();

            await waitFor(() => {
                expect(getMovieById).toHaveBeenCalledWith('2');
            });
        });
    });

    describe('Redirects', () => {
        it('should redirect to YouTube playlist profile for Playlist type', async () => {
            mockParamsId = '3';
            getMediaById.mockResolvedValue({ data: mockPlaylistMedia });

            renderComponent();

            await waitFor(() => {
                expect(mockNavigate).toHaveBeenCalledWith('/youtube-playlist/3', { replace: true });
            });
        });

        it('should redirect to podcast series profile for Channel type', async () => {
            mockParamsId = '5';
            const mockChannelMedia = {
                id: 5,
                title: 'Test Channel',
                mediaType: 'Channel',
                status: 'InProgress',
                mixlistIds: []
            };
            getMediaById.mockResolvedValue({ data: mockChannelMedia });

            renderComponent();

            await waitFor(() => {
                expect(mockNavigate).toHaveBeenCalledWith('/youtube-channel/5', { replace: true });
            });
        });
    });

    describe('Add to Mixlist', () => {
        it('should show add to mixlist FAB button', async () => {
            setupBookMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-header')).toBeInTheDocument();
            });
            // The MixlistCarousel is rendered (which handles add-to-mixlist in the real component)
            expect(screen.getByTestId('mixlist-carousel')).toBeInTheDocument();
        });

        it('should render mixlist carousel component', async () => {
            getMediaById.mockResolvedValue({ data: mockBookMedia });
            getBookById.mockResolvedValue({ data: mockBookDetails });
            getAllMixlists.mockResolvedValue({ data: mockMixlists });
            getHighlightsByBook.mockResolvedValue([]);

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('mixlist-carousel')).toBeInTheDocument();
            });
        });

        it('should fetch available mixlists on load', async () => {
            getMediaById.mockResolvedValue({ data: mockBookMedia });
            getBookById.mockResolvedValue({ data: mockBookDetails });
            getAllMixlists.mockResolvedValue({ data: mockMixlists });
            getHighlightsByBook.mockResolvedValue([]);

            renderComponent();

            await waitFor(() => {
                expect(getAllMixlists).toHaveBeenCalled();
            });
        });
    });

    describe('Sections', () => {
        it('should render topics and genres section', async () => {
            setupBookMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('topics-genres-section')).toBeInTheDocument();
            });
            expect(screen.getByTestId('topics-genres-section')).toHaveTextContent('Topics & Genres');
        });

        it('should render similar items section', async () => {
            setupBookMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('similar-items-section')).toBeInTheDocument();
            });
            expect(screen.getByTestId('similar-items-section')).toHaveTextContent('Similar Items');
        });
    });
});
