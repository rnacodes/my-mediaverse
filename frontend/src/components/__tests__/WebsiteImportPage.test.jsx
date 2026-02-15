import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import WebsiteImportPage from '../WebsiteImportPage';
import * as websiteService from '../../api/websiteService';

const mockNavigate = vi.fn();

vi.mock('../../api/websiteService', () => ({
  scrapeWebsitePreview: vi.fn(),
  importWebsite: vi.fn(),
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

describe('WebsiteImportPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockNavigate.mockClear();
  });

  it('should render the import form with all fields', () => {
    renderWithRouter(<WebsiteImportPage />);

    expect(screen.getByRole('heading', { name: /Import Website/ })).toBeInTheDocument();
    expect(screen.getByPlaceholderText('https://example.com')).toBeInTheDocument();
    expect(screen.getByText('Preview')).toBeInTheDocument();
    expect(screen.getByText('Import Directly')).toBeInTheDocument();
  });

  it('should disable Preview and Import buttons when URL is empty', () => {
    renderWithRouter(<WebsiteImportPage />);

    const previewButton = screen.getByText('Preview');
    const importButton = screen.getByText('Import Directly');

    expect(previewButton).toBeDisabled();
    expect(importButton).toBeDisabled();
  });

  it('should show error for invalid URL format', async () => {
    renderWithRouter(<WebsiteImportPage />);

    const urlInput = screen.getByPlaceholderText('https://example.com');
    fireEvent.change(urlInput, { target: { value: 'not-a-valid-url' } });

    const previewButton = screen.getByText('Preview');
    fireEvent.click(previewButton);

    await waitFor(() => {
      expect(screen.getByText(/Please enter a valid URL/)).toBeInTheDocument();
    });
  });

  it('should call scrapeWebsitePreview when Preview button is clicked with valid URL', async () => {
    const mockScrapedData = {
      url: 'https://example.com',
      title: 'Example Website',
      description: 'A test website',
      domain: 'example.com',
      imageUrl: 'https://example.com/image.jpg',
      rssFeedUrl: 'https://example.com/feed',
      author: 'Test Author',
      publication: 'Test Publication'
    };

    websiteService.scrapeWebsitePreview.mockResolvedValue(mockScrapedData);

    renderWithRouter(<WebsiteImportPage />);

    const urlInput = screen.getByPlaceholderText('https://example.com');
    fireEvent.change(urlInput, { target: { value: 'https://example.com' } });

    const previewButton = screen.getByText('Preview');
    fireEvent.click(previewButton);

    await waitFor(() => {
      expect(websiteService.scrapeWebsitePreview).toHaveBeenCalledWith('https://example.com');
    });

    await waitFor(() => {
      expect(screen.getByText('Example Website')).toBeInTheDocument();
      expect(screen.getByText('A test website')).toBeInTheDocument();
    });
  });

  it('should display RSS badge when RSS feed is detected', async () => {
    const mockScrapedData = {
      url: 'https://example.com',
      title: 'Example Website',
      domain: 'example.com',
      rssFeedUrl: 'https://example.com/feed'
    };

    websiteService.scrapeWebsitePreview.mockResolvedValue(mockScrapedData);

    renderWithRouter(<WebsiteImportPage />);

    const urlInput = screen.getByPlaceholderText('https://example.com');
    fireEvent.change(urlInput, { target: { value: 'https://example.com' } });

    const previewButton = screen.getByText('Preview');
    fireEvent.click(previewButton);

    await waitFor(() => {
      expect(screen.getByText('RSS Feed Detected')).toBeInTheDocument();
    });
  });

  it('should call importWebsite when Import Directly is clicked', async () => {
    const mockImportResult = {
      id: '123',
      title: 'Imported Website',
      link: 'https://test.com',
      domain: 'test.com'
    };

    websiteService.importWebsite.mockResolvedValue(mockImportResult);

    renderWithRouter(<WebsiteImportPage />);

    const urlInput = screen.getByPlaceholderText('https://example.com');
    fireEvent.change(urlInput, { target: { value: 'https://test.com' } });

    const importButton = screen.getByText('Import Directly');
    fireEvent.click(importButton);

    await waitFor(() => {
      expect(websiteService.importWebsite).toHaveBeenCalledWith({
        url: 'https://test.com',
        notes: null,
        topics: null,
        genres: null,
        titleOverride: null
      });
    });

    await waitFor(() => {
      expect(screen.getByText(/Website "Imported Website" imported successfully/)).toBeInTheDocument();
    });
  });

  it('should include notes, topics, and genres when importing', async () => {
    const mockImportResult = {
      id: '123',
      title: 'Test Website',
      link: 'https://test.com',
      domain: 'test.com'
    };

    websiteService.importWebsite.mockResolvedValue(mockImportResult);

    renderWithRouter(<WebsiteImportPage />);

    const urlInput = screen.getByPlaceholderText('https://example.com');
    fireEvent.change(urlInput, { target: { value: 'https://test.com' } });

    const notesInput = screen.getByPlaceholderText('Add your personal notes about this website');
    fireEvent.change(notesInput, { target: { value: 'Test notes' } });

    const topicsInput = screen.getByPlaceholderText('technology, programming, design');
    fireEvent.change(topicsInput, { target: { value: 'tech, web' } });

    const genresInput = screen.getByPlaceholderText('news, blog, tutorial');
    fireEvent.change(genresInput, { target: { value: 'blog, news' } });

    const importButton = screen.getByText('Import Directly');
    fireEvent.click(importButton);

    await waitFor(() => {
      expect(websiteService.importWebsite).toHaveBeenCalledWith({
        url: 'https://test.com',
        notes: 'Test notes',
        topics: ['tech', 'web'],
        genres: ['blog', 'news'],
        titleOverride: null
      });
    });
  });

  it('should use title override when provided', async () => {
    const mockImportResult = {
      id: '123',
      title: 'Custom Title',
      link: 'https://test.com',
      domain: 'test.com'
    };

    websiteService.importWebsite.mockResolvedValue(mockImportResult);

    renderWithRouter(<WebsiteImportPage />);

    const urlInput = screen.getByPlaceholderText('https://example.com');
    fireEvent.change(urlInput, { target: { value: 'https://test.com' } });

    const titleInput = screen.getByPlaceholderText('Override the scraped title');
    fireEvent.change(titleInput, { target: { value: 'Custom Title' } });

    const importButton = screen.getByText('Import Directly');
    fireEvent.click(importButton);

    await waitFor(() => {
      expect(websiteService.importWebsite).toHaveBeenCalledWith({
        url: 'https://test.com',
        notes: null,
        topics: null,
        genres: null,
        titleOverride: 'Custom Title'
      });
    });
  });

  it('should display error message when import fails', async () => {
    websiteService.importWebsite.mockRejectedValue({
      response: {
        data: {
          error: 'Failed to import website'
        }
      }
    });

    renderWithRouter(<WebsiteImportPage />);

    const urlInput = screen.getByPlaceholderText('https://example.com');
    fireEvent.change(urlInput, { target: { value: 'https://test.com' } });

    const importButton = screen.getByText('Import Directly');
    fireEvent.click(importButton);

    await waitFor(() => {
      expect(screen.getByText('Failed to import website')).toBeInTheDocument();
    });
  });

  it('should disable buttons while loading', async () => {
    websiteService.scrapeWebsitePreview.mockImplementation(() => new Promise(() => {}));

    renderWithRouter(<WebsiteImportPage />);

    const urlInput = screen.getByPlaceholderText('https://example.com');
    fireEvent.change(urlInput, { target: { value: 'https://test.com' } });

    const previewButton = screen.getByText('Preview');
    const importButton = screen.getByText('Import Directly');

    fireEvent.click(previewButton);

    await waitFor(() => {
      expect(previewButton).toBeDisabled();
      expect(importButton).toBeDisabled();
    });
  });

  it('should navigate to media profile after successful import', async () => {
    const mockImportResult = {
      id: '123',
      title: 'Test Website',
      link: 'https://test.com',
      domain: 'test.com'
    };

    websiteService.importWebsite.mockResolvedValue(mockImportResult);

    vi.useFakeTimers();

    renderWithRouter(<WebsiteImportPage />);

    const urlInput = screen.getByPlaceholderText('https://example.com');
    fireEvent.change(urlInput, { target: { value: 'https://test.com' } });

    const importButton = screen.getByText('Import Directly');
    fireEvent.click(importButton);

    await vi.advanceTimersByTimeAsync(2000);

    expect(mockNavigate).toHaveBeenCalledWith('/media/123');

    vi.useRealTimers();
  });
});
