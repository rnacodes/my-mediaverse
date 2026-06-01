// QUARANTINED (RAS-17): excluded from the run via vitest.config.js.
// TODO(RAS-28): rewrite against the new test infra (MSW + renderWithProviders).
// Component moved to src/features/videos/pages/YouTubeChannelProfile.jsx in the feature-folder reorg.
import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import YouTubeChannelProfile from '../YouTubeChannelProfile';
import { getYouTubeChannelById, getYouTubeChannelVideos, deleteYouTubeChannel, syncYouTubeChannelMetadata } from '../../api/youtubeService';
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
vi.mock('../../api/youtubeService', () => ({
    getYouTubeChannelById: vi.fn(),
    getYouTubeChannelVideos: vi.fn(),
    deleteYouTubeChannel: vi.fn(),
    syncYouTubeChannelMetadata: vi.fn()
}));
vi.mock('../../api/mixlistService', () => ({ getAllMixlists: vi.fn() }));
vi.mock('axios', () => ({ default: { get: vi.fn(), post: vi.fn() } }));

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
const mockChannel = {
    id: 1,
    title: 'Tech Channel',
    mediaType: 'Channel',
    status: 'ActivelyExploring',
    thumbnail: 'http://example.com/ch.jpg',
    channelExternalId: 'UC12345'
};

const mockVideos = [
    { id: 10, title: 'Video Alpha', mediaType: 'Video', status: 'Completed', dateAdded: '2024-01-01', thumbnail: 'http://example.com/v1.jpg' },
    { id: 11, title: 'Video Beta', mediaType: 'Video', status: 'InProgress', dateAdded: '2024-02-01', thumbnail: 'http://example.com/v2.jpg' }
];

const renderComponent = () => {
    return render(
        <BrowserRouter>
            <YouTubeChannelProfile />
        </BrowserRouter>
    );
};

const setupSuccessMocks = () => {
    getYouTubeChannelById.mockResolvedValue(mockChannel);
    getYouTubeChannelVideos.mockResolvedValue(mockVideos);
    getAllMixlists.mockResolvedValue({ data: [] });
};

const waitForDataLoad = async () => {
    await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'Tech Channel' })).toBeInTheDocument();
    });
};

describe('YouTubeChannelProfile', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    describe('Loading State', () => {
        it('should show loading spinner while fetching', () => {
            getYouTubeChannelById.mockImplementation(() => new Promise(() => {}));
            getYouTubeChannelVideos.mockImplementation(() => new Promise(() => {}));
            getAllMixlists.mockImplementation(() => new Promise(() => {}));

            renderComponent();

            expect(screen.getByRole('progressbar')).toBeInTheDocument();
        });
    });

    describe('Data Display', () => {
        it('should display channel title', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByRole('heading', { name: 'Tech Channel' })).toBeInTheDocument();
        });

        it('should render media info card', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-info-card')).toBeInTheDocument();
            });

            expect(screen.getByTestId('media-info-card')).toHaveTextContent('Tech Channel');
        });

        it('should show video count in accordion', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByText(/My Videos \(2\)/)).toBeInTheDocument();
        });

        it('should display imported video titles', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByText('Video Alpha')).toBeInTheDocument();
            expect(screen.getByText('Video Beta')).toBeInTheDocument();
        });

        it('should show empty state when no videos imported', async () => {
            getYouTubeChannelById.mockResolvedValue(mockChannel);
            getYouTubeChannelVideos.mockResolvedValue([]);
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            await waitForDataLoad();

            expect(screen.getByText(/No videos imported yet/)).toBeInTheDocument();
        });

        it('should render sub-components', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByTestId('topics-genres')).toBeInTheDocument();
            expect(screen.getByTestId('mixlist-carousel')).toBeInTheDocument();
        });
    });

    describe('Actions', () => {
        it('should navigate back when back button clicked', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const backButton = screen.getByTestId('ArrowBackIcon').closest('button');
            fireEvent.click(backButton);

            expect(mockNavigate).toHaveBeenCalledWith('/youtube-channels');
        });

        it('should navigate to edit page when edit button clicked', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const editButton = screen.getByTestId('EditIcon').closest('button');
            fireEvent.click(editButton);

            expect(mockNavigate).toHaveBeenCalledWith('/media/1/edit');
        });

        it('should show YouTube button linking to channel', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const ytButton = screen.getByText('YouTube').closest('a') || screen.getByText('YouTube').closest('button');
            expect(ytButton).toBeInTheDocument();
        });

        it('should call sync and show success message', async () => {
            setupSuccessMocks();
            syncYouTubeChannelMetadata.mockResolvedValue({});
            renderComponent();

            await waitForDataLoad();

            fireEvent.click(screen.getByText('Sync'));

            await waitFor(() => {
                expect(syncYouTubeChannelMetadata).toHaveBeenCalledWith('1');
            });
        });

        it('should show delete confirmation dialog', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            fireEvent.click(screen.getByText('Delete').closest('button'));

            await waitFor(() => {
                expect(screen.getByText('Delete Channel?')).toBeInTheDocument();
            });

            expect(screen.getByText(/This will remove "Tech Channel"/)).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Delete Forever' })).toBeInTheDocument();
        });

        it('should cancel delete dialog', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            fireEvent.click(screen.getByText('Delete').closest('button'));

            await waitFor(() => {
                expect(screen.getByText('Delete Channel?')).toBeInTheDocument();
            });

            fireEvent.click(screen.getByRole('button', { name: 'Cancel' }));

            await waitFor(() => {
                expect(screen.queryByText('Delete Channel?')).not.toBeInTheDocument();
            });
        });

        it('should call deleteYouTubeChannel when confirmed', async () => {
            setupSuccessMocks();
            deleteYouTubeChannel.mockResolvedValue({});
            renderComponent();

            await waitForDataLoad();

            fireEvent.click(screen.getByText('Delete').closest('button'));

            await waitFor(() => {
                expect(screen.getByText('Delete Channel?')).toBeInTheDocument();
            });

            fireEvent.click(screen.getByRole('button', { name: 'Delete Forever' }));

            await waitFor(() => {
                expect(deleteYouTubeChannel).toHaveBeenCalledWith('1');
            });
        });

        it('should navigate to video when clicked', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            fireEvent.click(screen.getByText('Video Alpha'));

            expect(mockNavigate).toHaveBeenCalledWith('/media/10');
        });
    });

    describe('Error Handling', () => {
        it('should show error state when API fails', async () => {
            getYouTubeChannelById.mockRejectedValue(new Error('Network error'));
            getYouTubeChannelVideos.mockRejectedValue(new Error('Network error'));
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            await waitFor(() => {
                expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
            });

            expect(screen.getByText('YouTube channel not found')).toBeInTheDocument();
        });
    });
});
