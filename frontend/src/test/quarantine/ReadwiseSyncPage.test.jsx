// QUARANTINED (RAS-17): excluded from the run via vitest.config.js.
// TODO(RAS-30): rewrite against the new test infra (MSW + renderWithProviders).
// Component moved to src/features/imports/pages/ReadwiseSyncPage.jsx in the feature-folder reorg.
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import ReadwiseSyncPage from '../ReadwiseSyncPage';
import * as readwiseService from '../../api/readwiseService';
import * as highlightService from '../../api/highlightService';
import * as bookService from '../../api/bookService';
import * as articleService from '../../api/articleService';

vi.mock('../../api/readwiseService', () => ({
  validateReadwiseConnection: vi.fn(),
  syncAll: vi.fn(),
  fetchArticleContent: vi.fn(),
}));
vi.mock('../../api/highlightService', () => ({
  getUnlinkedHighlights: vi.fn(),
  updateHighlight: vi.fn(),
  cleanHighlightText: vi.fn(),
}));
vi.mock('../../api/bookService', () => ({
  getAllBooks: vi.fn(),
}));
vi.mock('../../api/articleService', () => ({
  getAllArticles: vi.fn(),
}));

const renderWithRouter = (component) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

describe('ReadwiseSyncPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    highlightService.getUnlinkedHighlights.mockResolvedValue([]);
    bookService.getAllBooks.mockResolvedValue({ data: [] });
    articleService.getAllArticles.mockResolvedValue([]);
  });

  describe('Page Rendering', () => {
    it('should render the page with all sections', async () => {
      renderWithRouter(<ReadwiseSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Readwise Sync')).toBeInTheDocument();
      });
      expect(screen.getByText('Connection Status')).toBeInTheDocument();
      expect(screen.getByText('Sync Articles & Highlights')).toBeInTheDocument();
      expect(screen.getByText('Fetch Article Content (Archival)')).toBeInTheDocument();
      expect(screen.getByText('Maintenance')).toBeInTheDocument();
      expect(screen.getByText('Manage Unlinked Highlights')).toBeInTheDocument();
    });

    it('should display all action buttons', async () => {
      renderWithRouter(<ReadwiseSyncPage />);

      expect(screen.getByText('Validate Connection')).toBeInTheDocument();
      expect(screen.getByText('Full Sync')).toBeInTheDocument();
      expect(screen.getByText('Sync Last 7 Days')).toBeInTheDocument();
      expect(screen.getByText('Fetch 25')).toBeInTheDocument();
      expect(screen.getByText('Fetch 50')).toBeInTheDocument();
      expect(screen.getByText('Fetch Recently Synced (7 days)')).toBeInTheDocument();
      expect(screen.getByText('Clean Highlight Text')).toBeInTheDocument();

      await waitFor(() => {
        expect(screen.getByText('Refresh List')).toBeInTheDocument();
      });
    });
  });

  describe('Connection Validation', () => {
    it('should validate connection successfully', async () => {
      readwiseService.validateReadwiseConnection.mockResolvedValue({
        data: { connected: true, message: 'Connection successful' }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const validateButton = screen.getByText('Validate Connection');
      fireEvent.click(validateButton);

      await waitFor(() => {
        expect(screen.getByText('Connection successful')).toBeInTheDocument();
      });

      expect(readwiseService.validateReadwiseConnection).toHaveBeenCalledTimes(1);
    });

    it('should show error when connection validation fails', async () => {
      readwiseService.validateReadwiseConnection.mockRejectedValue({
        response: { data: { details: 'Invalid API token' } }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const validateButton = screen.getByText('Validate Connection');
      fireEvent.click(validateButton);

      await waitFor(() => {
        expect(screen.getByText(/Connection validation failed/)).toBeInTheDocument();
        expect(screen.getByText(/Invalid API token/)).toBeInTheDocument();
      });
    });

    it('should disable button and show loading text during validation', async () => {
      readwiseService.validateReadwiseConnection.mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve({ data: { connected: true } }), 100))
      );

      renderWithRouter(<ReadwiseSyncPage />);

      const validateButton = screen.getByText('Validate Connection');
      fireEvent.click(validateButton);

      expect(screen.getByText('Validating...')).toBeInTheDocument();

      await waitFor(() => {
        expect(screen.getByText('Validate Connection')).toBeInTheDocument();
      });
    });
  });

  describe('Sync Articles & Highlights', () => {
    it('should perform full sync successfully', async () => {
      readwiseService.syncAll.mockResolvedValue({
        data: {
          success: true,
          articlesCreated: 50,
          articlesUpdated: 10,
          highlightsCreated: 100,
          highlightsUpdated: 20,
          highlightsLinked: 80,
          duration: '0:00:05'
        }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fullSyncButton = screen.getByText('Full Sync');
      fireEvent.click(fullSyncButton);

      await waitFor(() => {
        expect(screen.getByText('Sync Results')).toBeInTheDocument();
      });

      expect(readwiseService.syncAll).toHaveBeenCalledWith(false);
    });

    it('should perform incremental sync (last 7 days)', async () => {
      readwiseService.syncAll.mockResolvedValue({
        data: {
          success: true,
          articlesCreated: 5,
          articlesUpdated: 2,
          highlightsCreated: 10,
          highlightsUpdated: 3,
          highlightsLinked: 8
        }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const incrementalSyncButton = screen.getByText('Sync Last 7 Days');
      fireEvent.click(incrementalSyncButton);

      await waitFor(() => {
        expect(screen.getByText('Sync Results')).toBeInTheDocument();
      });

      expect(readwiseService.syncAll).toHaveBeenCalledWith(true);
    });

    it('should handle sync errors', async () => {
      readwiseService.syncAll.mockRejectedValue({
        response: { data: { details: 'Rate limit exceeded' } }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fullSyncButton = screen.getByText('Full Sync');
      fireEvent.click(fullSyncButton);

      await waitFor(() => {
        expect(screen.getByText(/Sync failed/)).toBeInTheDocument();
        expect(screen.getByText(/Rate limit exceeded/)).toBeInTheDocument();
      });
    });

    it('should show loading state during sync', async () => {
      readwiseService.syncAll.mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve({ data: { success: true } }), 100))
      );

      renderWithRouter(<ReadwiseSyncPage />);

      const fullSyncButton = screen.getByText('Full Sync');
      fireEvent.click(fullSyncButton);

      expect(screen.getAllByText('Syncing...').length).toBeGreaterThan(0);

      await waitFor(() => {
        expect(screen.getByText('Full Sync')).toBeInTheDocument();
      });
    });
  });

  describe('Content Fetching', () => {
    it('should fetch 25 articles', async () => {
      readwiseService.fetchArticleContent.mockResolvedValue({
        data: { fetchedCount: 25, message: 'Successfully fetched 25 articles' }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fetch25Button = screen.getByText('Fetch 25');
      fireEvent.click(fetch25Button);

      await waitFor(() => {
        expect(screen.getByText('Fetch Results')).toBeInTheDocument();
      });

      expect(readwiseService.fetchArticleContent).toHaveBeenCalledWith(25, false);
    });

    it('should fetch 50 articles', async () => {
      readwiseService.fetchArticleContent.mockResolvedValue({
        data: { fetchedCount: 50, message: 'Successfully fetched 50 articles' }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fetch50Button = screen.getByText('Fetch 50');
      fireEvent.click(fetch50Button);

      await waitFor(() => {
        expect(screen.getByText('Fetch Results')).toBeInTheDocument();
      });

      expect(readwiseService.fetchArticleContent).toHaveBeenCalledWith(50, false);
    });

    it('should fetch recently synced articles', async () => {
      readwiseService.fetchArticleContent.mockResolvedValue({
        data: { fetchedCount: 10, message: 'Fetched 10 recent articles' }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fetchRecentButton = screen.getByText('Fetch Recently Synced (7 days)');
      fireEvent.click(fetchRecentButton);

      await waitFor(() => {
        expect(screen.getByText('Fetch Results')).toBeInTheDocument();
      });

      expect(readwiseService.fetchArticleContent).toHaveBeenCalledWith(50, true);
    });
  });

  describe('Maintenance', () => {
    it('should clean highlight text successfully', async () => {
      highlightService.cleanHighlightText.mockResolvedValue({
        cleanedCount: 15,
        message: 'Cleaned 15 highlights'
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const cleanButton = screen.getByText('Clean Highlight Text');
      fireEvent.click(cleanButton);

      await waitFor(() => {
        expect(screen.getByText('Cleaned 15 highlights')).toBeInTheDocument();
      });

      expect(highlightService.cleanHighlightText).toHaveBeenCalledTimes(1);
    });

    it('should show loading state during cleaning', async () => {
      highlightService.cleanHighlightText.mockImplementation(
        () => new Promise(resolve => setTimeout(() => resolve({ cleanedCount: 0, message: 'Done' }), 100))
      );

      renderWithRouter(<ReadwiseSyncPage />);

      const cleanButton = screen.getByText('Clean Highlight Text');
      fireEvent.click(cleanButton);

      expect(screen.getByText('Cleaning...')).toBeInTheDocument();

      await waitFor(() => {
        expect(screen.getByText('Clean Highlight Text')).toBeInTheDocument();
      });
    });
  });

  describe('Unlinked Highlights', () => {
    it('should show empty state when no unlinked highlights', async () => {
      highlightService.getUnlinkedHighlights.mockResolvedValue([]);

      renderWithRouter(<ReadwiseSyncPage />);

      await waitFor(() => {
        expect(screen.getByText(/No unlinked highlights found/)).toBeInTheDocument();
      });
    });

    it('should display unlinked highlights list', async () => {
      const mockHighlights = [
        {
          id: '1',
          text: 'This is a test highlight',
          title: 'Test Article',
          author: 'Author Name',
          category: 'article',
          highlightedAt: '2024-01-15T10:00:00Z'
        },
        {
          id: '2',
          text: 'Another highlight text',
          title: 'Another Article',
          author: null,
          category: 'book',
          highlightedAt: '2024-01-16T12:00:00Z'
        }
      ];

      highlightService.getUnlinkedHighlights.mockResolvedValue(mockHighlights);

      renderWithRouter(<ReadwiseSyncPage />);

      await waitFor(() => {
        expect(screen.getByText(/2/)).toBeInTheDocument();
      });

      expect(screen.getByText(/This is a test highlight/)).toBeInTheDocument();
      expect(screen.getByText(/Another highlight text/)).toBeInTheDocument();
    });

    it('should refresh unlinked highlights when Refresh button is clicked', async () => {
      highlightService.getUnlinkedHighlights.mockResolvedValue([]);

      renderWithRouter(<ReadwiseSyncPage />);

      await waitFor(() => {
        expect(highlightService.getUnlinkedHighlights).toHaveBeenCalledTimes(1);
      });

      const refreshButton = screen.getByText('Refresh List');
      fireEvent.click(refreshButton);

      await waitFor(() => {
        expect(highlightService.getUnlinkedHighlights).toHaveBeenCalledTimes(2);
      });
    });
  });

  describe('Duration Formatting', () => {
    it('should format duration with hours', async () => {
      readwiseService.syncAll.mockResolvedValue({
        data: {
          success: true,
          articlesCreated: 100,
          articlesUpdated: 20,
          highlightsCreated: 0,
          highlightsUpdated: 0,
          highlightsLinked: 0,
          duration: '1:23:45'
        }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fullSyncButton = screen.getByText('Full Sync');
      fireEvent.click(fullSyncButton);

      await waitFor(() => {
        expect(screen.getByText('1h 23m 45s')).toBeInTheDocument();
      });
    });

    it('should format duration with only minutes', async () => {
      readwiseService.syncAll.mockResolvedValue({
        data: {
          success: true,
          articlesCreated: 50,
          articlesUpdated: 10,
          highlightsCreated: 0,
          highlightsUpdated: 0,
          highlightsLinked: 0,
          duration: '0:05:30'
        }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fullSyncButton = screen.getByText('Full Sync');
      fireEvent.click(fullSyncButton);

      await waitFor(() => {
        expect(screen.getByText('05m 30s')).toBeInTheDocument();
      });
    });

    it('should format duration with only seconds', async () => {
      readwiseService.syncAll.mockResolvedValue({
        data: {
          success: true,
          articlesCreated: 10,
          articlesUpdated: 2,
          highlightsCreated: 0,
          highlightsUpdated: 0,
          highlightsLinked: 0,
          duration: '0:00:15'
        }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fullSyncButton = screen.getByText('Full Sync');
      fireEvent.click(fullSyncButton);

      await waitFor(() => {
        expect(screen.getByText('15s')).toBeInTheDocument();
      });
    });
  });

  describe('Error Handling', () => {
    it('should clear error when starting new operation', async () => {
      readwiseService.syncAll.mockRejectedValueOnce({
        response: { data: { details: 'First error' } }
      });
      readwiseService.syncAll.mockResolvedValue({
        data: { success: true, articlesCreated: 10, articlesUpdated: 0, highlightsCreated: 0, highlightsUpdated: 0, highlightsLinked: 0 }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fullSyncButton = screen.getByText('Full Sync');

      fireEvent.click(fullSyncButton);
      await waitFor(() => {
        expect(screen.getByText(/First error/)).toBeInTheDocument();
      });

      fireEvent.click(fullSyncButton);
      await waitFor(() => {
        expect(screen.queryByText(/First error/)).not.toBeInTheDocument();
        expect(screen.getByText('Sync Results')).toBeInTheDocument();
      });
    });

    it('should handle network errors gracefully', async () => {
      readwiseService.syncAll.mockRejectedValue({
        message: 'Network Error'
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fullSyncButton = screen.getByText('Full Sync');
      fireEvent.click(fullSyncButton);

      await waitFor(() => {
        expect(screen.getByText(/Sync failed/)).toBeInTheDocument();
        expect(screen.getByText(/Network Error/)).toBeInTheDocument();
      });
    });

    it('should handle content fetch errors', async () => {
      readwiseService.fetchArticleContent.mockRejectedValue({
        response: { data: { details: 'Fetch failed' } }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const fetch25Button = screen.getByText('Fetch 25');
      fireEvent.click(fetch25Button);

      await waitFor(() => {
        expect(screen.getByText(/Content fetch failed/)).toBeInTheDocument();
      });
    });

    it('should handle cleanup errors', async () => {
      highlightService.cleanHighlightText.mockRejectedValue({
        response: { data: { details: 'Cleanup error' } }
      });

      renderWithRouter(<ReadwiseSyncPage />);

      const cleanButton = screen.getByText('Clean Highlight Text');
      fireEvent.click(cleanButton);

      await waitFor(() => {
        expect(screen.getByText(/Cleanup failed/)).toBeInTheDocument();
      });
    });
  });

  describe('Accessibility', () => {
    it('should have proper heading structure', async () => {
      renderWithRouter(<ReadwiseSyncPage />);

      await waitFor(() => {
        expect(screen.getByRole('heading', { level: 1 })).toHaveTextContent('Readwise Sync');
      });
    });

    it('should have proper button states initially', async () => {
      renderWithRouter(<ReadwiseSyncPage />);

      await waitFor(() => {
        expect(screen.getByText('Validate Connection')).not.toBeDisabled();
      });

      const fullSyncButton = screen.getByText('Full Sync');
      expect(fullSyncButton).not.toBeDisabled();
    });
  });
});
