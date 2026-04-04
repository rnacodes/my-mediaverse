import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import TvShowProfile from '../TvShowProfile';
import { getTvShowById, getEpisodesByShowId, deleteTvShow } from '../../api/tvShowService';
import { getAllMixlists } from '../../api/mixlistService';

// Mock sub-components
vi.mock('../MediaInfoCard', () => ({
    default: ({ mediaItem }) => <div data-testid="media-info-card">{mediaItem?.title}</div>
}));
vi.mock('../MediaDetailAccordion', () => ({
    default: () => <div data-testid="media-detail-accordion">Details</div>
}));
vi.mock('../MixlistCarousel', () => ({
    default: () => <div data-testid="mixlist-carousel">Mixlists</div>
}));
vi.mock('../TopicsGenresSection', () => ({
    default: () => <div data-testid="topics-genres">Topics</div>
}));
vi.mock('../RelatedNotesSection', () => ({
    default: () => <div data-testid="related-notes">Notes</div>
}));
vi.mock('../SavedRelatedMediaSection', () => ({
    default: () => <div data-testid="saved-related-media">Related</div>
}));
vi.mock('../SimilarItemsSection', () => ({
    default: () => <div data-testid="similar-items">Similar</div>
}));

// Mock API services
vi.mock('../../api/tvShowService', () => ({
    getTvShowById: vi.fn(),
    getEpisodesByShowId: vi.fn(),
    deleteTvShow: vi.fn()
}));
vi.mock('../../api/mixlistService', () => ({ getAllMixlists: vi.fn() }));

// Mock router
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual('react-router-dom');
    return {
        ...actual,
        useParams: () => ({ id: '1' }),
        useNavigate: () => mockNavigate
    };
});

// Mock data
const mockShow = {
    id: 1,
    title: 'Breaking Bad',
    mediaType: 'TVShow',
    status: 'Completed',
    thumbnail: 'http://example.com/bb.jpg',
    link: 'https://example.com/show'
};

const mockEpisodes = [
    { id: 10, title: 'Pilot', mediaType: 'TVShow', seasonNumber: 1, episodeNumber: 1, status: 'Completed', episodeIdentifier: 'S01E01' },
    { id: 11, title: 'Cat\'s in the Bag', mediaType: 'TVShow', seasonNumber: 1, episodeNumber: 2, status: 'Completed', episodeIdentifier: 'S01E02' },
    { id: 12, title: 'Grilled', mediaType: 'TVShow', seasonNumber: 2, episodeNumber: 1, status: 'InProgress', episodeIdentifier: 'S02E01' }
];

const renderComponent = () => {
    return render(
        <BrowserRouter>
            <TvShowProfile />
        </BrowserRouter>
    );
};

const setupSuccessMocks = () => {
    getTvShowById.mockResolvedValue({ data: mockShow });
    getEpisodesByShowId.mockResolvedValue({ data: mockEpisodes });
    getAllMixlists.mockResolvedValue({ data: [] });
};

const waitForDataLoad = async () => {
    await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Breaking Bad' })).toBeInTheDocument();
    });
};

describe('TvShowProfile', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    describe('Loading State', () => {
        it('should show loading spinner while fetching', () => {
            getTvShowById.mockImplementation(() => new Promise(() => {}));
            getEpisodesByShowId.mockImplementation(() => new Promise(() => {}));
            getAllMixlists.mockImplementation(() => new Promise(() => {}));

            renderComponent();

            expect(screen.getByRole('progressbar')).toBeInTheDocument();
        });
    });

    describe('Data Display', () => {
        it('should display show title', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByRole('heading', { name: 'Breaking Bad' })).toBeInTheDocument();
        });

        it('should render media info card', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-info-card')).toBeInTheDocument();
            });

            expect(screen.getByTestId('media-info-card')).toHaveTextContent('Breaking Bad');
        });

        it('should render sub-components', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByTestId('media-detail-accordion')).toBeInTheDocument();
            expect(screen.getByTestId('topics-genres')).toBeInTheDocument();
            expect(screen.getByTestId('related-notes')).toBeInTheDocument();
            expect(screen.getByTestId('saved-related-media')).toBeInTheDocument();
            expect(screen.getByTestId('similar-items')).toBeInTheDocument();
            expect(screen.getByTestId('mixlist-carousel')).toBeInTheDocument();
        });

        it('should show watch progress bar', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByText('Watch Progress')).toBeInTheDocument();
            // 2 of 3 episodes completed = 67%
            expect(screen.getByText(/2 \/ 3 episodes \(67%\)/)).toBeInTheDocument();
        });

        it('should group episodes by season', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByText(/Season 1 \(2\)/)).toBeInTheDocument();
            expect(screen.getByText(/Season 2 \(1\)/)).toBeInTheDocument();
        });

        it('should display episode titles', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByText('Pilot')).toBeInTheDocument();
            expect(screen.getByText("Cat's in the Bag")).toBeInTheDocument();
            expect(screen.getByText('Grilled')).toBeInTheDocument();
        });

        it('should show episode identifiers', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByText('S01E01')).toBeInTheDocument();
            expect(screen.getByText('S01E02')).toBeInTheDocument();
            expect(screen.getByText('S02E01')).toBeInTheDocument();
        });

        it('should show View button when show has a link', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const viewButton = screen.getByText('View').closest('button') || screen.getByText('View').closest('a');
            expect(viewButton).toBeInTheDocument();
        });

        it('should not show View button when show has no link', async () => {
            getTvShowById.mockResolvedValue({ data: { ...mockShow, link: null } });
            getEpisodesByShowId.mockResolvedValue({ data: mockEpisodes });
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            await waitForDataLoad();

            expect(screen.queryByText('View')).not.toBeInTheDocument();
        });
    });

    describe('No Episodes', () => {
        it('should show Trakt Sync prompt when no episodes', async () => {
            getTvShowById.mockResolvedValue({ data: mockShow });
            getEpisodesByShowId.mockResolvedValue({ data: [] });
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            await waitForDataLoad();

            expect(screen.getByText(/No episodes tracked yet/)).toBeInTheDocument();
            expect(screen.getByText('Go to Trakt Sync')).toBeInTheDocument();
        });

        it('should navigate to Trakt Sync when button clicked', async () => {
            getTvShowById.mockResolvedValue({ data: mockShow });
            getEpisodesByShowId.mockResolvedValue({ data: [] });
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            await waitForDataLoad();

            fireEvent.click(screen.getByText('Go to Trakt Sync'));
            expect(mockNavigate).toHaveBeenCalledWith('/trakt-sync');
        });

        it('should not show watch progress when no episodes', async () => {
            getTvShowById.mockResolvedValue({ data: mockShow });
            getEpisodesByShowId.mockResolvedValue({ data: [] });
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            await waitForDataLoad();

            expect(screen.queryByText('Watch Progress')).not.toBeInTheDocument();
        });
    });

    describe('Actions', () => {
        it('should navigate back when back button clicked', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const backButton = screen.getByTestId('ArrowBackIcon').closest('button');
            fireEvent.click(backButton);

            expect(mockNavigate).toHaveBeenCalledWith('/all-media?mediaType=TVShow');
        });

        it('should navigate to edit page when edit button clicked', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const editButton = screen.getByTestId('EditIcon').closest('button');
            fireEvent.click(editButton);

            expect(mockNavigate).toHaveBeenCalledWith('/media/1/edit');
        });

        it('should show delete confirmation dialog', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const deleteButton = screen.getByText('Delete').closest('button');
            fireEvent.click(deleteButton);

            await waitFor(() => {
                expect(screen.getByText('Delete TV Show?')).toBeInTheDocument();
            });

            expect(screen.getByText(/This will remove "Breaking Bad"/)).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Delete Forever' })).toBeInTheDocument();
        });

        it('should cancel delete dialog', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            fireEvent.click(screen.getByText('Delete').closest('button'));

            await waitFor(() => {
                expect(screen.getByText('Delete TV Show?')).toBeInTheDocument();
            });

            fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

            await waitFor(() => {
                expect(screen.queryByText('Delete TV Show?')).not.toBeInTheDocument();
            });
        });

        it('should call deleteTvShow when confirmed', async () => {
            setupSuccessMocks();
            deleteTvShow.mockResolvedValue({});
            renderComponent();

            await waitForDataLoad();

            fireEvent.click(screen.getByText('Delete').closest('button'));

            await waitFor(() => {
                expect(screen.getByText('Delete TV Show?')).toBeInTheDocument();
            });

            fireEvent.click(screen.getByRole('button', { name: 'Delete Forever' }));

            await waitFor(() => {
                expect(deleteTvShow).toHaveBeenCalledWith('1');
            });
        });

        it('should navigate to episode when clicked', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            fireEvent.click(screen.getByText('Pilot'));

            expect(mockNavigate).toHaveBeenCalledWith('/media/10');
        });
    });

    describe('Error Handling', () => {
        it('should show error state when API fails', async () => {
            getTvShowById.mockRejectedValue(new Error('Network error'));
            getEpisodesByShowId.mockRejectedValue(new Error('Network error'));
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            await waitFor(() => {
                expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
            });

            expect(screen.getByText('TV show not found')).toBeInTheDocument();
        });
    });
});
