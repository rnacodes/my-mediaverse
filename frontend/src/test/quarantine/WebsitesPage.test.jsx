// QUARANTINED (RAS-17): excluded from the run via vitest.config.js.
// TODO(RAS-25): rewrite against the new test infra (MSW + renderWithProviders).
// Component moved to src/features/imports/pages/WebsitesPage.jsx in the feature-folder reorg.
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import WebsitesPage from '../WebsitesPage';
import * as websiteService from '../../api/websiteService';

// Create mock navigate function
const mockNavigate = vi.fn();

// Mock the API service
vi.mock('../../api/websiteService', () => ({
  getAllWebsites: vi.fn(),
  getWebsitesWithRss: vi.fn(),
  deleteWebsite: vi.fn(),
}));

vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return {
    ...actual,
    useNavigate: () => mockNavigate,
  };
});

const renderWithRouter = (component) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

const mockWebsites = [
  {
    id: '1',
    title: 'The Verge',
    description: 'Technology news and media network',
    link: 'https://www.theverge.com',
    domain: 'theverge.com',
    thumbnail: 'https://example.com/verge.jpg',
    rssFeedUrl: 'https://www.theverge.com/rss/index.xml',
    author: null,
    publication: 'The Verge',
    dateAdded: '2024-01-15T10:00:00Z',
    topics: ['technology', 'news'],
    genres: ['news'],
    status: 'Uncharted',
    mediaType: 'Website'
  },
  {
    id: '2',
    title: 'CSS-Tricks',
    description: 'Web development blog',
    link: 'https://css-tricks.com',
    domain: 'css-tricks.com',
    thumbnail: 'https://example.com/css.jpg',
    rssFeedUrl: 'https://css-tricks.com/feed',
    author: null,
    publication: 'CSS-Tricks',
    dateAdded: '2024-01-16T10:00:00Z',
    topics: ['web development'],
    genres: ['tutorial'],
    status: 'Uncharted',
    mediaType: 'Website'
  },
  {
    id: '3',
    title: 'Example Site',
    description: 'Example website without RSS',
    link: 'https://example.com',
    domain: 'example.com',
    thumbnail: null,
    rssFeedUrl: null,
    author: 'John Doe',
    publication: null,
    dateAdded: '2024-01-17T10:00:00Z',
    topics: [],
    genres: [],
    status: 'Uncharted',
    mediaType: 'Website'
  }
];

describe('WebsitesPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockNavigate.mockClear();
    websiteService.getAllWebsites.mockResolvedValue(mockWebsites);
    websiteService.getWebsitesWithRss.mockResolvedValue(mockWebsites.filter(w => w.rssFeedUrl));
  });

  it('should render websites page with header', async () => {
    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(screen.getByText('Websites')).toBeInTheDocument();
    });
  });

  it('should fetch and display all websites on load', async () => {
    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(websiteService.getAllWebsites).toHaveBeenCalled();
    });

    await waitFor(() => {
      expect(screen.getAllByText('The Verge').length).toBeGreaterThan(0);
      expect(screen.getAllByText('CSS-Tricks').length).toBeGreaterThan(0);
      expect(screen.getAllByText('Example Site').length).toBeGreaterThan(0);
    });
  });

  it('should display statistics chips', async () => {
    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(screen.getByText('3 Total')).toBeInTheDocument();
      expect(screen.getByText('2 With RSS')).toBeInTheDocument();
      expect(screen.getByText('3 Domains')).toBeInTheDocument();
    });
  });

  it('should filter websites by search query', async () => {
    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(screen.getAllByText('The Verge').length).toBeGreaterThan(0);
    });

    const searchInput = screen.getByPlaceholderText('Search websites...');
    fireEvent.change(searchInput, { target: { value: 'css' } });

    await waitFor(() => {
      expect(screen.getAllByText('CSS-Tricks').length).toBeGreaterThan(0);
      expect(screen.queryAllByText('The Verge').length).toBe(0);
      expect(screen.queryAllByText('Example Site').length).toBe(0);
    });
  });

  it('should navigate to import page when Import Website button clicked', async () => {
    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      const importButton = screen.getByText('Import Website');
      fireEvent.click(importButton);
    });

    expect(mockNavigate).toHaveBeenCalledWith('/import-website');
  });

  it('should call deleteWebsite when delete button clicked and confirmed', async () => {
    // Mock window.confirm to return true
    global.confirm = vi.fn(() => true);
    websiteService.deleteWebsite.mockResolvedValue();

    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(screen.getAllByText('The Verge').length).toBeGreaterThan(0);
    });

    // Find and click the delete button (there are multiple, get the first one)
    const deleteButtons = screen.getAllByLabelText('Delete');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(global.confirm).toHaveBeenCalled();
      expect(websiteService.deleteWebsite).toHaveBeenCalled();
    });

    // Clean up
    global.confirm.mockRestore();
  });

  it('should not delete when user cancels confirmation', async () => {
    // Mock window.confirm to return false
    global.confirm = vi.fn(() => false);

    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(screen.getAllByText('The Verge').length).toBeGreaterThan(0);
    });

    const deleteButtons = screen.getAllByLabelText('Delete');
    fireEvent.click(deleteButtons[0]);

    await waitFor(() => {
      expect(global.confirm).toHaveBeenCalled();
      expect(websiteService.deleteWebsite).not.toHaveBeenCalled();
    });

    // Clean up
    global.confirm.mockRestore();
  });

  it('should display empty state when no websites', async () => {
    websiteService.getAllWebsites.mockResolvedValue([]);

    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(screen.getByText('No websites yet')).toBeInTheDocument();
      expect(screen.getByText('Start by importing your first website')).toBeInTheDocument();
    });
  });

  it('should show no results message when search has no matches', async () => {
    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(screen.getAllByText('The Verge').length).toBeGreaterThan(0);
    });

    const searchInput = screen.getByPlaceholderText('Search websites...');
    fireEvent.change(searchInput, { target: { value: 'nonexistentwebsite123' } });

    await waitFor(() => {
      expect(screen.getByText('No websites found')).toBeInTheDocument();
      expect(screen.getByText('Try adjusting your search or filters')).toBeInTheDocument();
    });
  });

  it('should refresh websites when refresh button clicked', async () => {
    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(websiteService.getAllWebsites).toHaveBeenCalledTimes(1);
    });

    const refreshButton = screen.getByLabelText('Refresh');
    fireEvent.click(refreshButton);

    await waitFor(() => {
      expect(websiteService.getAllWebsites).toHaveBeenCalledTimes(2);
    });
  });

  it('should display RSS badge for websites with RSS feeds', async () => {
    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      const rssBadges = screen.getAllByText('RSS');
      // Should have 2 RSS badges (The Verge and CSS-Tricks)
      expect(rssBadges).toHaveLength(2);
    });
  });

  it('should display domain badges for all websites', async () => {
    renderWithRouter(<WebsitesPage />);

    await waitFor(() => {
      expect(screen.getByText('theverge.com')).toBeInTheDocument();
      expect(screen.getByText('css-tricks.com')).toBeInTheDocument();
      expect(screen.getByText('example.com')).toBeInTheDocument();
    });
  });
});
