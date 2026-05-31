import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach, afterEach } from 'vitest';
import YouTubePlaylistProfile from '../YouTubePlaylistProfile';
import { getYouTubePlaylistById, getYouTubePlaylistVideos, deleteYouTubePlaylist, syncYouTubePlaylist, addVideoToYouTubePlaylist } from '../../api/youtubeService';
import { getAllMixlists } from '../../api/mixlistService';

// --- Mocks ---

vi.mock('../MediaInfoCard', () => ({
    default: ({ mediaItem }) => <div data-testid="media-info-card">{mediaItem?.title}</div>
}));
vi.mock('../MixlistCarousel', () => ({
    default: () => <div data-testid="mixlist-carousel">Mixlists</div>
}));
vi.mock('../TopicsGenresSection', () => ({
    default: () => <div data-testid="topics-genres">Topics</div>
}));

vi.mock('../../api/youtubeService', () => ({
    getYouTubePlaylistById: vi.fn(),
    getYouTubePlaylistVideos: vi.fn(),
    deleteYouTubePlaylist: vi.fn(),
    syncYouTubePlaylist: vi.fn(),
    addVideoToYouTubePlaylist: vi.fn()
}));
vi.mock('../../api/mixlistService', () => ({ getAllMixlists: vi.fn() }));
vi.mock('axios', () => ({ default: { get: vi.fn() } }));

const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
    const actual = await vi.importActual('react-router-dom');
    return {
        ...actual,
        useParams: () => ({ id: '1' }),
        useNavigate: () => mockNavigate
    };
});

// --- Mock Data ---

const mockPlaylist = {
    id: 1,
    title: 'My Playlist',
    mediaType: 'Playlist',
    status: 'ActivelyExploring',
    thumbnail: 'http://example.com/pl.jpg',
    youTubeUrl: 'https://youtube.com/playlist?list=PLtest'
};

const mockVideos = [
    { id: 1, title: 'Video 1', mediaType: 'Video', thumbnail: 'http://example.com/v1.jpg' },
    { id: 2, title: 'Video 2', mediaType: 'Video', thumbnail: 'http://example.com/v2.jpg' }
];

// --- Helpers ---

const renderComponent = () => {
    return render(
        <BrowserRouter>
            <YouTubePlaylistProfile />
        </BrowserRouter>
    );
};

const setupSuccessMocks = () => {
    getYouTubePlaylistById.mockResolvedValue(mockPlaylist);
    getYouTubePlaylistVideos.mockResolvedValue(mockVideos);
    getAllMixlists.mockResolvedValue({ data: [] });
};

const waitForDataLoad = async () => {
    await waitFor(() => {
        expect(screen.getByRole('heading', { name: 'My Playlist' })).toBeInTheDocument();
    });
};

// --- Tests ---

describe('YouTubePlaylistProfile', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    afterEach(() => {
        vi.clearAllMocks();
    });

    describe('Loading State', () => {
        it('should show loading spinner while fetching', () => {
            getYouTubePlaylistById.mockImplementation(() => new Promise(() => {}));
            getYouTubePlaylistVideos.mockImplementation(() => new Promise(() => {}));
            getAllMixlists.mockImplementation(() => new Promise(() => {}));

            renderComponent();

            expect(screen.getByRole('progressbar')).toBeInTheDocument();
        });
    });

    describe('Data Display', () => {
        it('should display playlist title', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            expect(screen.getByRole('heading', { name: 'My Playlist' })).toBeInTheDocument();
        });

        it('should show video count', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const videosHeading = screen.getByText(/My Videos/);
            expect(videosHeading).toBeInTheDocument();
            expect(videosHeading.textContent).toContain('2');
        });

        it('should render media info card', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitFor(() => {
                expect(screen.getByTestId('media-info-card')).toBeInTheDocument();
            });

            expect(screen.getByTestId('media-info-card')).toHaveTextContent('My Playlist');
        });
    });

    describe('Actions', () => {
        it('should show back button', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const backButton = screen.getByTestId('ArrowBackIcon').closest('button');
            expect(backButton).toBeInTheDocument();

            fireEvent.click(backButton);
            expect(mockNavigate).toHaveBeenCalledWith('/all-media?mediaType=Playlist');
        });

        it('should show delete confirmation dialog when delete is clicked', async () => {
            setupSuccessMocks();
            renderComponent();

            await waitForDataLoad();

            const deleteButton = screen.getByText('Delete').closest('button');
            fireEvent.click(deleteButton);

            await waitFor(() => {
                expect(screen.getByText('Delete Playlist?')).toBeInTheDocument();
            });

            expect(screen.getByText(/This will remove/)).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Cancel' })).toBeInTheDocument();
            expect(screen.getByRole('button', { name: 'Delete Forever' })).toBeInTheDocument();
        });
    });

    describe('Error Handling', () => {
        it('should handle API errors gracefully', async () => {
            getYouTubePlaylistById.mockRejectedValue(new Error('Network error'));
            getYouTubePlaylistVideos.mockRejectedValue(new Error('Network error'));
            getAllMixlists.mockResolvedValue({ data: [] });

            renderComponent();

            await waitFor(() => {
                expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
            });

            expect(screen.getByText('YouTube playlist not found')).toBeInTheDocument();
        });
    });
});
