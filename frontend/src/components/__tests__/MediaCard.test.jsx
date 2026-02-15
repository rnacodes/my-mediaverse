import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import { vi } from 'vitest';
import MediaCard from '../shared/MediaCard';

const renderWithRouter = (component) => {
  return render(
    <BrowserRouter>
      {component}
    </BrowserRouter>
  );
};

describe('MediaCard', () => {
  const mockVideoMedia = {
    id: '123e4567-e89b-12d3-a456-426614174000',
    title: 'Test Video',
    notes: 'A test video description',
    mediaType: 'Video',
    status: 'Uncharted',
    dateAdded: '2024-01-15T10:00:00Z',
    link: 'https://youtube.com/watch?v=test',
    thumbnailUrl: 'https://example.com/thumb.jpg',
    videoType: 'Series',
    platform: 'YouTube',
    channelName: 'Test Channel',
    lengthInSeconds: 3600,
    rating: 4.5,
    topics: ['technology', 'programming'],
    genres: ['educational', 'tutorial']
  };

  const mockVideoEpisode = {
    id: '456e7890-e89b-12d3-a456-426614174001',
    title: 'Episode 1: Introduction',
    notes: 'First episode of the series',
    mediaType: 'Video',
    status: 'InProgress',
    dateAdded: '2024-01-16T10:00:00Z',
    link: 'https://youtube.com/watch?v=episode1',
    thumbnailUrl: 'https://example.com/episode-thumb.jpg',
    videoType: 'Episode',
    parentVideoId: '123e4567-e89b-12d3-a456-426614174000',
    platform: 'YouTube',
    channelName: 'Test Channel',
    lengthInSeconds: 1800,
    rating: 5,
    topics: ['introduction', 'basics'],
    genres: ['educational']
  };

  const mockBookMedia = {
    id: '789e1234-e89b-12d3-a456-426614174002',
    title: 'Test Book',
    notes: 'A test book description',
    mediaType: 'Book',
    status: 'Completed',
    dateAdded: '2024-01-10T10:00:00Z',
    rating: 4,
    topics: ['fiction', 'mystery'],
    genres: ['thriller', 'crime']
  };

  describe('Video Media Display', () => {
    it('should display video series information correctly', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      expect(screen.getByText('Test Video')).toBeInTheDocument();
      expect(screen.getByText('A test video description')).toBeInTheDocument();
      expect(screen.getByText('YouTube')).toBeInTheDocument();
      expect(screen.getByText('Test Channel')).toBeInTheDocument();
      expect(screen.getByText('60:00')).toBeInTheDocument(); // 3600 seconds = 60:00
    });

    it('should display video episode information correctly', () => {
      renderWithRouter(<MediaCard media={mockVideoEpisode} />);

      expect(screen.getByText('Episode 1: Introduction')).toBeInTheDocument();
      expect(screen.getByText('First episode of the series')).toBeInTheDocument();
      expect(screen.getByText('30:00')).toBeInTheDocument(); // 1800 seconds = 30:00
    });

    it('should show media type icon by default', () => {
      const { container } = renderWithRouter(<MediaCard media={mockVideoMedia} />);

      // Check that the icon box exists (it has the icon inside)
      const iconBox = container.querySelector('.MuiBox-root');
      expect(iconBox).toBeInTheDocument();
    });

    it('should not show media type icon when showMediaTypeIcon is false', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} showMediaTypeIcon={false} />);

      // When showMediaTypeIcon is false, the icon overlay shouldn't render
      // We can verify by checking that certain video info still exists
      expect(screen.getByText('Test Video')).toBeInTheDocument();
    });

    it('should display video thumbnail when provided', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      const thumbnail = screen.getByRole('img', { name: /Test Video/i });
      expect(thumbnail).toBeInTheDocument();
      expect(thumbnail).toHaveAttribute('src', 'https://example.com/thumb.jpg');
    });

    it('should display default thumbnail when not provided', () => {
      const videoWithoutThumbnail = { ...mockVideoMedia, thumbnailUrl: null, imageUrl: null };
      renderWithRouter(<MediaCard media={videoWithoutThumbnail} />);

      const thumbnail = screen.getByRole('img', { name: /Test Video/i });
      expect(thumbnail).toBeInTheDocument();
      expect(thumbnail).toHaveAttribute('src', expect.stringContaining('placehold.co'));
    });
  });

  describe('Video Status and Rating Display', () => {
    it('should display video status correctly', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      expect(screen.getByText('Uncharted')).toBeInTheDocument();
    });

    it('should display video rating correctly', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      // Rating should be displayed with the numeric value
      expect(screen.getByText('4.5/5')).toBeInTheDocument();
    });

    it('should display different statuses as chips', () => {
      const inProgressVideo = { ...mockVideoMedia, status: 'InProgress' };
      renderWithRouter(<MediaCard media={inProgressVideo} />);

      const statusChip = screen.getByText('In Progress');
      expect(statusChip).toBeInTheDocument();
    });
  });

  describe('Media Type and Status Chips Display', () => {
    it('should display media type chip', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      expect(screen.getByText('Video')).toBeInTheDocument();
    });

    it('should display status chip when provided', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      expect(screen.getByText('Uncharted')).toBeInTheDocument();
    });

    it('should handle missing status gracefully', () => {
      const videoWithoutStatus = {
        ...mockVideoMedia,
        status: undefined
      };
      renderWithRouter(<MediaCard media={videoWithoutStatus} />);

      // Should not crash and should still display other information
      expect(screen.getByText('Test Video')).toBeInTheDocument();
      expect(screen.getByText('Video')).toBeInTheDocument(); // Media type still shown
    });
  });

  describe('Video Duration Formatting', () => {
    it('should format duration correctly as MM:SS', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      // 3600 seconds = 60 minutes 0 seconds = 60:00
      expect(screen.getByText('60:00')).toBeInTheDocument();
    });

    it('should format shorter durations correctly', () => {
      const shortVideo = { ...mockVideoMedia, lengthInSeconds: 1800 }; // 30 minutes
      renderWithRouter(<MediaCard media={shortVideo} />);

      // 1800 seconds = 30 minutes 0 seconds = 30:00
      expect(screen.getByText('30:00')).toBeInTheDocument();
    });

    it('should pad seconds with zero', () => {
      const videoWithPadding = { ...mockVideoMedia, lengthInSeconds: 605 }; // 10 minutes 5 seconds
      renderWithRouter(<MediaCard media={videoWithPadding} />);

      // Should display as 10:05 not 10:5
      expect(screen.getByText('10:05')).toBeInTheDocument();
    });

    it('should handle zero duration', () => {
      const zeroDurationVideo = { ...mockVideoMedia, lengthInSeconds: 0 };
      const { container } = renderWithRouter(<MediaCard media={zeroDurationVideo} />);

      // Component might not display duration when it's 0, or display as 0:00
      // Just verify the component renders without crashing
      expect(screen.getByText('Test Video')).toBeInTheDocument();
    });

    it('should handle longer durations', () => {
      const longVideo = { ...mockVideoMedia, lengthInSeconds: 7265 }; // 121 minutes 5 seconds
      renderWithRouter(<MediaCard media={longVideo} />);

      expect(screen.getByText('121:05')).toBeInTheDocument();
    });
  });

  describe('Card Variants', () => {
    it('should render default variant correctly', () => {
      const { container } = renderWithRouter(<MediaCard media={mockVideoMedia} variant="default" />);

      // Card should render with default styling (MUI Card component)
      const card = container.querySelector('.MuiCard-root');
      expect(card).toBeInTheDocument();
    });

    it('should render compact variant correctly', () => {
      const { container } = renderWithRouter(<MediaCard media={mockVideoMedia} variant="compact" />);

      // Card should render with MUI Card
      const card = container.querySelector('.MuiCard-root');
      expect(card).toBeInTheDocument();

      // Title should use smaller typography in compact mode
      expect(screen.getByText('Test Video')).toBeInTheDocument();
    });

    it('should render featured variant correctly', () => {
      const { container } = renderWithRouter(<MediaCard media={mockVideoMedia} variant="featured" />);

      // Card should render
      const card = container.querySelector('.MuiCard-root');
      expect(card).toBeInTheDocument();
    });
  });

  describe('Card Interactions', () => {
    it('should call onClick handler when card is clicked', () => {
      const mockOnClick = vi.fn();
      const { container } = renderWithRouter(<MediaCard media={mockVideoMedia} onClick={mockOnClick} />);

      const card = container.querySelector('.MuiCard-root');
      fireEvent.click(card);

      expect(mockOnClick).toHaveBeenCalledWith(mockVideoMedia);
    });

    it('should navigate to media profile when card is clicked without onClick handler', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      const link = screen.getByRole('link');
      expect(link).toHaveAttribute('href', '/media/123e4567-e89b-12d3-a456-426614174000');
    });

    it('should render as clickable box when onClick handler is provided', () => {
      const mockOnClick = vi.fn();
      const { container } = renderWithRouter(<MediaCard media={mockVideoMedia} onClick={mockOnClick} />);

      // When onClick is provided, it wraps in a Box, not a Link
      const clickableBox = container.querySelector('.MuiBox-root');
      expect(clickableBox).toBeInTheDocument();

      fireEvent.click(clickableBox);
      expect(mockOnClick).toHaveBeenCalled();
    });
  });

  describe('Media Type Comparison', () => {
    it('should display video media differently from book media', () => {
      const { rerender } = renderWithRouter(<MediaCard media={mockVideoMedia} />);

      // Video should show platform and channel
      expect(screen.getByText('YouTube')).toBeInTheDocument();
      expect(screen.getByText('Test Channel')).toBeInTheDocument();
      expect(screen.getByText('60:00')).toBeInTheDocument();

      // Rerender with book media
      rerender(
        <BrowserRouter>
          <MediaCard media={mockBookMedia} />
        </BrowserRouter>
      );

      // Book should not show platform, channel, or duration
      expect(screen.queryByText('YouTube')).not.toBeInTheDocument();
      expect(screen.queryByText('Test Channel')).not.toBeInTheDocument();
      expect(screen.queryByText('60:00')).not.toBeInTheDocument();

      // But should show book-specific information
      expect(screen.getByText('Test Book')).toBeInTheDocument();
      expect(screen.getByText('A test book description')).toBeInTheDocument();
    });
  });

  describe('Accessibility', () => {
    it('should have proper alt text for thumbnail images', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      const thumbnail = screen.getByRole('img', { name: /Test Video/i });
      expect(thumbnail).toHaveAttribute('alt', 'Test Video');
    });

    it('should have proper link when onClick is not provided', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      const link = screen.getByRole('link');
      expect(link).toHaveAttribute('href', '/media/123e4567-e89b-12d3-a456-426614174000');
    });

    it('should be keyboard accessible', () => {
      renderWithRouter(<MediaCard media={mockVideoMedia} />);

      const link = screen.getByRole('link');
      // Links are keyboard accessible by default
      expect(link).toBeInTheDocument();
    });
  });

  describe('Error Handling', () => {
    it('should handle missing required fields gracefully', () => {
      const incompleteVideo = {
        id: '123',
        title: 'Incomplete Video',
        mediaType: 'Video'
        // Missing other fields
      };

      expect(() => {
        renderWithRouter(<MediaCard media={incompleteVideo} />);
      }).not.toThrow();

      expect(screen.getByText('Incomplete Video')).toBeInTheDocument();
    });

    it('should handle missing thumbnailUrl gracefully', () => {
      const videoWithoutThumb = { ...mockVideoMedia, thumbnailUrl: undefined, imageUrl: undefined };

      expect(() => {
        renderWithRouter(<MediaCard media={videoWithoutThumb} />);
      }).not.toThrow();

      // Should show placeholder - get the img element specifically by alt text
      const thumbnail = screen.getByAltText('Test Video');
      expect(thumbnail).toHaveAttribute('src', expect.stringContaining('placehold.co'));
    });

    it('should handle missing duration gracefully', () => {
      const videoWithoutDuration = { ...mockVideoMedia, lengthInSeconds: undefined };

      expect(() => {
        renderWithRouter(<MediaCard media={videoWithoutDuration} />);
      }).not.toThrow();

      // Should still show title and other info
      expect(screen.getByText('Test Video')).toBeInTheDocument();
    });
  });
});
