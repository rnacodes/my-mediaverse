import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import PodcastSeriesProfile from '../PodcastSeriesProfile';
import { getPodcastSeriesById, getEpisodesBySeriesId, syncPodcastSeriesEpisodes, deletePodcastSeries, importPodcastEpisodeFromApi } from '../../api/podcastService';
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

// Mock API services
vi.mock('../../api/podcastService', () => ({
    getPodcastSeriesById: vi.fn(),
    getEpisodesBySeriesId: vi.fn(),
    syncPodcastSeriesEpisodes: vi.fn(),
    deletePodcastSeries: vi.fn(),
    importPodcastEpisodeFromApi: vi.fn()
}));
vi.mock('../../api/mixlistService', () => ({ getAllMixlists: vi.fn() }));
vi.mock('axios', () => ({ default: { get: vi.fn() } }));

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
const mockSeries = {
    id: 1,
    title: 'Tech Podcast',
    mediaType: 'Podcast',
    status: 'ActivelyExploring',
    thumbnail: 'http://example.com/pod.jpg',
    listenNotesId: 'abc123'
};

const mockEpisodes = [
    { id: 1, title: 'Episode 1', mediaType: 'Podcast', seriesId: 1 },
    { id: 2, title: 'Episode 2', mediaType: 'Podcast', seriesId: 1 }
];

const renderComponent = () => {
    return render(
        <BrowserRouter>
            <PodcastSeriesProfile />
        </BrowserRouter>
    );
};

const setupSuccessMocks = () => {
    getPodcastSeriesById.mockResolvedValue({ data: mockSeries });
    getEpisodesBySeriesId.mockResolvedValue({ data: mockEpisodes });
    getAllMixlists.mockResolvedValue({ data: [] });
};

describe('PodcastSeriesProfile', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    describe('Loading State', () => {
        it('should show loading spinner', () => {
            getPodcastSeriesById.mockImplementation(() => new Promise(() => {}));
            getEpisodesBySeriesId.mockImplementation(() => new Promise(() => {}));
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            expect(screen.getByRole('progressbar')).toBeInTheDocument();
        });
    });

    describe('Data Display', () => {
        it('should display series title', async () => {
            setupSuccessMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByRole('heading', { name: 'Tech Podcast' })).toBeInTheDocument();
            });
        });

        it('should show episode count', async () => {
            setupSuccessMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByText(/My Episodes \(2\)/)).toBeInTheDocument();
            });
        });

        it('should render media info card', async () => {
            setupSuccessMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-info-card')).toBeInTheDocument();
            });
        });
    });

    describe('Actions', () => {
        it('should show back button', async () => {
            setupSuccessMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-info-card')).toBeInTheDocument();
            });

            // The back button is an IconButton with ArrowBack icon
            const backButton = screen.getByTestId('ArrowBackIcon').closest('button');
            expect(backButton).toBeInTheDocument();
        });

        it('should show delete confirmation dialog', async () => {
            setupSuccessMocks();

            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-info-card')).toBeInTheDocument();
            });

            // Click the Delete button in the action bar
            const deleteIcon = screen.getByTestId('DeleteIcon');
            fireEvent.click(deleteIcon.closest('button'));

            await waitFor(() => {
                expect(screen.getByText('Delete Series?')).toBeInTheDocument();
                expect(screen.getByText(/This will remove "Tech Podcast" and all its imported episodes./)).toBeInTheDocument();
            });
        });
    });

    describe('Error Handling', () => {
        it('should handle API errors gracefully', async () => {
            getPodcastSeriesById.mockRejectedValue(new Error('Network error'));
            getEpisodesBySeriesId.mockRejectedValue(new Error('Network error'));
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            await waitFor(() => {
                expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
            });

            // When series is null after error, the component shows an error alert
            expect(screen.getByText('Podcast series not found')).toBeInTheDocument();
        });
    });
});
