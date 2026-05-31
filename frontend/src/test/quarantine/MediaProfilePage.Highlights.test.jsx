import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import MediaProfilePage from '../MediaProfilePage';
import { getMediaById } from '../../api/mediaService';
import { getAllMixlists } from '../../api/mixlistService';
import { getBookById } from '../../api/bookService';
import { getArticleById } from '../../api/articleService';
import { getHighlightsByArticle, getHighlightsByBook } from '../../api/highlightService';

// Mock sub-components
vi.mock('../MediaHeader', () => ({
    default: ({ media }) => <div data-testid="media-header">{media?.title}</div>
}));
vi.mock('../MixlistCarousel', () => ({
    default: () => <div data-testid="mixlist-carousel">Mixlists</div>
}));
vi.mock('../MediaInfoCard', () => ({
    default: () => <div data-testid="media-info-card">Info</div>
}));
vi.mock('../MediaDetailAccordion', () => ({
    default: () => <div data-testid="media-detail-accordion">Details</div>
}));
vi.mock('../HighlightsSection', () => ({
    default: ({ highlights, highlightsLoading }) => (
        <div data-testid="highlights-section">
            {highlightsLoading ? 'Loading highlights...' : `${highlights?.length || 0} highlights`}
        </div>
    )
}));
vi.mock('../TopicsGenresSection', () => ({ default: () => <div data-testid="topics-genres">TG</div> }));
vi.mock('../RelatedNotesSection', () => ({ default: () => <div data-testid="related-notes">RN</div> }));
vi.mock('../SimilarItemsSection', () => ({ default: () => <div data-testid="similar-items">SI</div> }));
vi.mock('../SavedRelatedMediaSection', () => ({ default: () => <div data-testid="saved-related">SR</div> }));

// Mock API services
vi.mock('../../api/mediaService', () => ({ getMediaById: vi.fn() }));
vi.mock('../../api/mixlistService', () => ({ getAllMixlists: vi.fn() }));
vi.mock('../../api/bookService', () => ({ getBookById: vi.fn() }));
vi.mock('../../api/podcastService', () => ({ getPodcastSeriesById: vi.fn(), getPodcastEpisodeById: vi.fn(), getEpisodesBySeriesId: vi.fn() }));
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

// Mock data
const mockBookMedia = { id: 1, title: 'Test Book', mediaType: 'Book', status: 'Completed' };
const mockArticleMedia = { id: 2, title: 'Test Article', mediaType: 'Article', status: 'Completed' };
const mockMovieMedia = { id: 3, title: 'Test Movie', mediaType: 'Movie', status: 'Completed' };
const mockVideoMedia = { id: 4, title: 'Test Video', mediaType: 'Video', status: 'Completed' };
const mockHighlights = [
    { id: 1, text: 'Important quote', note: 'My note', color: 'yellow' },
    { id: 2, text: 'Another highlight', note: '', color: 'blue' }
];

const renderWithRouter = (ui) => {
    return render(
        <BrowserRouter>
            {ui}
        </BrowserRouter>
    );
};

describe('MediaProfilePage - Highlights', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mockParamsId = '1';
    });

    describe('Book Highlights', () => {
        it('should fetch highlights for book media type', async () => {
            getMediaById.mockResolvedValue({ data: mockBookMedia });
            getBookById.mockResolvedValue({ data: { ...mockBookMedia, author: 'Author' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByBook.mockResolvedValue(mockHighlights);

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(getHighlightsByBook).toHaveBeenCalledWith(mockBookMedia.id);
            });
        });

        it('should pass highlights to HighlightsSection', async () => {
            getMediaById.mockResolvedValue({ data: mockBookMedia });
            getBookById.mockResolvedValue({ data: { ...mockBookMedia, author: 'Author' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByBook.mockResolvedValue(mockHighlights);

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toHaveTextContent('2 highlights');
            });
        });

        it('should show highlight count for books', async () => {
            const threeHighlights = [
                { id: 1, text: 'Quote one', note: '', color: 'yellow' },
                { id: 2, text: 'Quote two', note: '', color: 'blue' },
                { id: 3, text: 'Quote three', note: '', color: 'green' }
            ];
            getMediaById.mockResolvedValue({ data: mockBookMedia });
            getBookById.mockResolvedValue({ data: { ...mockBookMedia, author: 'Author' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByBook.mockResolvedValue(threeHighlights);

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toHaveTextContent('3 highlights');
            });
        });

        it('should handle empty highlights for books', async () => {
            getMediaById.mockResolvedValue({ data: mockBookMedia });
            getBookById.mockResolvedValue({ data: { ...mockBookMedia, author: 'Author' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByBook.mockResolvedValue([]);

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toHaveTextContent('0 highlights');
            });
        });
    });

    describe('Article Highlights', () => {
        it('should fetch highlights for article media type', async () => {
            mockParamsId = '2';
            getMediaById.mockResolvedValue({ data: mockArticleMedia });
            getArticleById.mockResolvedValue({ data: { ...mockArticleMedia, author: 'Journalist' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByArticle.mockResolvedValue(mockHighlights);

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(getHighlightsByArticle).toHaveBeenCalledWith(mockArticleMedia.id);
            });
        });

        it('should pass highlights to HighlightsSection for articles', async () => {
            mockParamsId = '2';
            getMediaById.mockResolvedValue({ data: mockArticleMedia });
            getArticleById.mockResolvedValue({ data: { ...mockArticleMedia, author: 'Journalist' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByArticle.mockResolvedValue(mockHighlights);

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toHaveTextContent('2 highlights');
            });
        });

        it('should show highlight count for articles', async () => {
            mockParamsId = '2';
            const singleHighlight = [
                { id: 1, text: 'Article passage', note: 'Interesting', color: 'yellow' }
            ];
            getMediaById.mockResolvedValue({ data: mockArticleMedia });
            getArticleById.mockResolvedValue({ data: { ...mockArticleMedia, author: 'Journalist' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByArticle.mockResolvedValue(singleHighlight);

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toHaveTextContent('1 highlights');
            });
        });

        it('should handle empty highlights for articles', async () => {
            mockParamsId = '2';
            getMediaById.mockResolvedValue({ data: mockArticleMedia });
            getArticleById.mockResolvedValue({ data: { ...mockArticleMedia, author: 'Journalist' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByArticle.mockResolvedValue([]);

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toHaveTextContent('0 highlights');
            });
        });
    });

    describe('Non-highlight Media Types', () => {
        it('should not fetch highlights for movie type', async () => {
            mockParamsId = '3';
            getMediaById.mockResolvedValue({ data: mockMovieMedia });
            const { getMovieById } = await import('../../api/movieService');
            getMovieById.mockResolvedValue({ data: { ...mockMovieMedia, director: 'Director' } });
            getAllMixlists.mockResolvedValue({ data: [] });

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toBeInTheDocument();
            });

            expect(getHighlightsByBook).not.toHaveBeenCalled();
            expect(getHighlightsByArticle).not.toHaveBeenCalled();
        });

        it('should not fetch highlights for video type', async () => {
            mockParamsId = '4';
            getMediaById.mockResolvedValue({ data: mockVideoMedia });
            const { getVideoById, getPlaylistsForVideo } = await import('../../api/videoService');
            getVideoById.mockResolvedValue({ data: { ...mockVideoMedia, platform: 'YouTube' } });
            getPlaylistsForVideo.mockResolvedValue([]);
            getAllMixlists.mockResolvedValue({ data: [] });

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toBeInTheDocument();
            });

            expect(getHighlightsByBook).not.toHaveBeenCalled();
            expect(getHighlightsByArticle).not.toHaveBeenCalled();
        });
    });

    describe('Error Handling', () => {
        it('should handle highlight fetch errors gracefully', async () => {
            getMediaById.mockResolvedValue({ data: mockBookMedia });
            getBookById.mockResolvedValue({ data: { ...mockBookMedia, author: 'Author' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByBook.mockRejectedValue(new Error('Failed to fetch highlights'));

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toHaveTextContent('0 highlights');
            });
        });

        it('should still render page when highlights fail', async () => {
            mockParamsId = '2';
            getMediaById.mockResolvedValue({ data: mockArticleMedia });
            getArticleById.mockResolvedValue({ data: { ...mockArticleMedia, author: 'Journalist' } });
            getAllMixlists.mockResolvedValue({ data: [] });
            getHighlightsByArticle.mockRejectedValue(new Error('Network error'));

            renderWithRouter(<MediaProfilePage />);

            await waitFor(() => {
                expect(screen.getByTestId('highlights-section')).toBeInTheDocument();
            });

            expect(screen.getByTestId('media-info-card')).toBeInTheDocument();
            expect(screen.getByTestId('media-detail-accordion')).toBeInTheDocument();
            expect(screen.getByTestId('mixlist-carousel')).toBeInTheDocument();
        });
    });
});
