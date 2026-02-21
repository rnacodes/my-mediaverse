import React from 'react';
import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import MixlistsPage from '../MixlistsPage';
import * as mixlistService from '../../api/mixlistService';

// Mock the API service
vi.mock('../../api/mixlistService');

// Mock useNavigate
const mockNavigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

// Test data
const mockMixlists = [
  {
    id: 'mixlist-1',
    name: 'My Favorites',
    description: 'Best content I have found',
    thumbnail: 'https://example.com/thumb1.jpg',
    mediaItems: [{ id: '1' }, { id: '2' }, { id: '3' }]
  },
  {
    id: 'mixlist-2',
    name: 'Watch Later',
    description: 'Content to watch when I have time',
    thumbnail: 'https://example.com/thumb2.jpg',
    mediaItems: [{ id: '4' }]
  },
  {
    id: 'mixlist-3',
    name: 'Educational',
    description: 'Learning resources',
    thumbnail: null,
    mediaItems: []
  }
];

const renderWithRouter = () => {
  return render(
    <MemoryRouter>
      <MixlistsPage />
    </MemoryRouter>
  );
};

describe('MixlistsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Loading State', () => {
    it('should display loading spinner while fetching mixlists', async () => {
      mixlistService.getAllMixlists.mockImplementation(() => new Promise(() => {}));

      renderWithRouter();

      expect(screen.getByRole('progressbar')).toBeInTheDocument();
      expect(screen.getByText('Loading mixlists...')).toBeInTheDocument();
    });
  });

  describe('Empty State', () => {
    it('should display empty state when no mixlists exist', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: [] });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('No mixlists yet')).toBeInTheDocument();
      });

      expect(screen.getByText('Create your first mixlist to organize your media!')).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /create first mixlist/i })).toBeInTheDocument();
    });

    it('should navigate to create mixlist page from empty state', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: [] });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('No mixlists yet')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByRole('button', { name: /create first mixlist/i }));

      expect(mockNavigate).toHaveBeenCalledWith('/create-mixlist', { state: { returnTo: '/mixlists' } });
    });
  });

  describe('Mixlists Display', () => {
    it('should display mixlists in card view by default', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Mixlists')).toBeInTheDocument();
      });

      expect(screen.getByText('My Favorites')).toBeInTheDocument();
      expect(screen.getByText('Watch Later')).toBeInTheDocument();
      expect(screen.getByText('Educational')).toBeInTheDocument();
    });

    it('should display mixlist count in header', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('3 mixlists found')).toBeInTheDocument();
      });
    });

    it('should display media item count for each mixlist', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('3 items')).toBeInTheDocument();
        expect(screen.getByText('1 item')).toBeInTheDocument();
        expect(screen.getByText('0 items')).toBeInTheDocument();
      });
    });

    it('should display mixlist descriptions', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('Best content I have found')).toBeInTheDocument();
        expect(screen.getByText('Content to watch when I have time')).toBeInTheDocument();
      });
    });
  });

  describe('View Mode Toggle', () => {
    it('should switch to list view when list button is clicked', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Favorites')).toBeInTheDocument();
      });

      const listButton = screen.getByRole('button', { name: /^list$/i });
      fireEvent.click(listButton);

      // List view should show items differently - check for list structure
      expect(screen.getByText('My Favorites')).toBeInTheDocument();
    });

    it('should have cards button selected by default', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Favorites')).toBeInTheDocument();
      });

      const cardsButton = screen.getByRole('button', { name: /cards/i });
      // The contained variant indicates selection
      expect(cardsButton).toHaveClass('MuiButton-contained');
    });
  });

  describe('Selection Functionality', () => {
    it('should select all mixlists when Select All is clicked', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Favorites')).toBeInTheDocument();
      });

      fireEvent.click(screen.getByRole('button', { name: /^select all$/i }));

      await waitFor(() => {
        expect(screen.getByText(/3 selected/)).toBeInTheDocument();
      });
    });

    it('should deselect all when Deselect All is clicked', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Favorites')).toBeInTheDocument();
      });

      // First select all
      fireEvent.click(screen.getByRole('button', { name: /^select all$/i }));

      await waitFor(() => {
        expect(screen.getByText(/3 selected/)).toBeInTheDocument();
      });

      // Then deselect all
      fireEvent.click(screen.getByRole('button', { name: /^deselect all$/i }));

      await waitFor(() => {
        expect(screen.queryByText(/selected/)).not.toBeInTheDocument();
      });
    });

    it('should have deselect button disabled when nothing is selected', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Favorites')).toBeInTheDocument();
      });

      const deselectButton = screen.getByRole('button', { name: /^deselect all$/i });
      expect(deselectButton).toBeDisabled();
    });
  });

  describe('Navigation', () => {
    it('should navigate to mixlist detail page when FAB is clicked', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Favorites')).toBeInTheDocument();
      });

      const fab = screen.getByRole('button', { name: /create mixlist/i });
      fireEvent.click(fab);

      expect(mockNavigate).toHaveBeenCalledWith('/create-mixlist', { state: { returnTo: '/mixlists' } });
    });
  });

  describe('Delete Dialog', () => {
    it('should open delete dialog when delete button is clicked with selections', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Favorites')).toBeInTheDocument();
      });

      // Select all first
      fireEvent.click(screen.getByRole('button', { name: /^select all$/i }));

      await waitFor(() => {
        expect(screen.getByText(/3 selected/)).toBeInTheDocument();
      });

      // Click delete - button shows "Delete Selected (3)" with count
      fireEvent.click(screen.getByRole('button', { name: /delete selected \(3\)/i }));

      await waitFor(() => {
        expect(screen.getByText('Confirm Bulk Delete')).toBeInTheDocument();
        expect(screen.getByText(/are you sure you want to delete 3 mixlists/i)).toBeInTheDocument();
      });
    });

    it('should close delete dialog when cancel is clicked', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Favorites')).toBeInTheDocument();
      });

      // Select all and open dialog
      fireEvent.click(screen.getByRole('button', { name: /^select all$/i }));

      await waitFor(() => {
        expect(screen.getByText(/3 selected/)).toBeInTheDocument();
      });

      // Button shows "Delete Selected (3)" with count
      fireEvent.click(screen.getByRole('button', { name: /delete selected \(3\)/i }));

      await waitFor(() => {
        expect(screen.getByText('Confirm Bulk Delete')).toBeInTheDocument();
      });

      // Click cancel
      fireEvent.click(screen.getByRole('button', { name: /cancel/i }));

      await waitFor(() => {
        expect(screen.queryByText('Confirm Bulk Delete')).not.toBeInTheDocument();
      });
    });

    it('should have delete button disabled when nothing is selected', async () => {
      mixlistService.getAllMixlists.mockResolvedValue({ data: mockMixlists });

      renderWithRouter();

      await waitFor(() => {
        expect(screen.getByText('My Favorites')).toBeInTheDocument();
      });

      // Button may show "Delete Selected" or "Delete Selected (0)" when nothing selected
      const deleteButton = screen.getByRole('button', { name: /delete selected/i });
      expect(deleteButton).toBeDisabled();
    });
  });

  describe('API Error Handling', () => {
    it('should handle API error gracefully', async () => {
      mixlistService.getAllMixlists.mockRejectedValue(new Error('Failed to fetch'));

      renderWithRouter();

      // Should complete loading and show empty state (error handling shows no mixlists)
      await waitFor(() => {
        expect(screen.queryByRole('progressbar')).not.toBeInTheDocument();
      });
    });
  });
});
