import { describe, it, expect, vi } from 'vitest';
import { renderWithProviders, screen, fireEvent } from '../test/test-utils';
import MediaCard from './MediaCard';
import { makeBook, makeVideo } from '../test/factories/media';

// Placeholders are bundled per-type SVG assets resolved by mediaImageUtils;
// in the test environment the import resolves to its asset path.

describe('MediaCard', () => {
  describe('success — populated media', () => {
    it('renders the core video fields', () => {
      // status defaults to 'Uncharted', rating defaults to null (no rating row)
      renderWithProviders(<MediaCard media={makeVideo()} />);

      expect(screen.getByText('Test Video')).toBeInTheDocument(); // title
      expect(screen.getByText('Video')).toBeInTheDocument(); // media-type chip
      expect(screen.getByText('Uncharted')).toBeInTheDocument(); // status chip
      expect(screen.getByText('YouTube')).toBeInTheDocument(); // platform chip
      expect(screen.getByText('Test Channel')).toBeInTheDocument(); // channel
      expect(screen.getByText('60:00')).toBeInTheDocument(); // 3600s formatted

      const thumb = screen.getByRole('img', { name: 'Test Video' });
      expect(thumb).toHaveAttribute('src', 'https://example.com/thumb.jpg');
    });

    it('renders the rating row when a rating is present', () => {
      renderWithProviders(<MediaCard media={makeVideo({ rating: 4.5 })} />);
      expect(screen.getByText('4.5/5')).toBeInTheDocument();
    });

    it('renders a book without the video-only fields', () => {
      renderWithProviders(
        <MediaCard media={makeBook({ notes: 'A book about testing' })} />,
      );

      expect(screen.getByText('Test Book')).toBeInTheDocument();
      expect(screen.getByText('Book')).toBeInTheDocument();
      expect(screen.getByText('A book about testing')).toBeInTheDocument();

      // platform / channel / duration belong to Video only
      expect(screen.queryByText('YouTube')).not.toBeInTheDocument();
      expect(screen.queryByText('Test Channel')).not.toBeInTheDocument();
      expect(screen.queryByText('60:00')).not.toBeInTheDocument();
    });
  });

  describe('duration formatting', () => {
    it.each([
      [3600, '60:00'],
      [1800, '30:00'],
      [605, '10:05'], // seconds zero-padded
      [7265, '121:05'], // over an hour
    ])('formats %i seconds as %s', (lengthInSeconds, expected) => {
      renderWithProviders(<MediaCard media={makeVideo({ lengthInSeconds })} />);
      expect(screen.getByText(expected)).toBeInTheDocument();
    });

    it('omits the duration when length is zero', () => {
      renderWithProviders(<MediaCard media={makeVideo({ lengthInSeconds: 0 })} />);
      expect(screen.getByText('Test Video')).toBeInTheDocument();
      expect(screen.queryByText('0:00')).not.toBeInTheDocument();
    });
  });

  describe('empty / sparse media', () => {
    it('omits the status chip, rating and notes when they are absent', () => {
      renderWithProviders(
        <MediaCard media={makeBook({ status: undefined, rating: null, notes: null })} />,
      );

      expect(screen.getByText('Test Book')).toBeInTheDocument();
      expect(screen.queryByText('Uncharted')).not.toBeInTheDocument();
      expect(screen.queryByText(/\/5$/)).not.toBeInTheDocument(); // rating caption
    });

    it('falls back to the per-type placeholder when no thumbnail is provided', () => {
      renderWithProviders(
        <MediaCard media={makeBook({ thumbnail: null })} />,
      );
      const thumb = screen.getByRole('img', { name: 'Test Book' });
      expect(thumb.getAttribute('src')).toContain('book.svg');
    });

    it('reads the canonical `thumbnail` field only (legacy thumbnailUrl is ignored)', () => {
      // The API returns `thumbnail`. Legacy `thumbnailUrl`/`imageUrl` shapes are no
      // longer read, so a thumbnail-less item falls back to the placeholder even
      // when a legacy field is present.
      renderWithProviders(
        <MediaCard
          media={makeBook({
            thumbnail: null,
            thumbnailUrl: 'https://example.com/legacy-thumb.jpg',
          })}
        />,
      );
      const thumb = screen.getByRole('img', { name: 'Test Book' });
      expect(thumb.getAttribute('src')).toContain('book.svg');
    });
  });

  describe('resilience', () => {
    it('does not throw on incomplete media', () => {
      const incomplete = { id: 'x1', title: 'Incomplete', mediaType: 'Video' };
      expect(() => renderWithProviders(<MediaCard media={incomplete} />)).not.toThrow();
      expect(screen.getByText('Incomplete')).toBeInTheDocument();
    });

    it('swaps to the placeholder when the thumbnail fails to load', () => {
      renderWithProviders(<MediaCard media={makeVideo()} />);
      const thumb = screen.getByRole('img', { name: 'Test Video' });
      // No userEvent equivalent for an <img> load error — a raw DOM event is the
      // only way to drive the onError fallback.
      fireEvent.error(thumb);
      expect(thumb.getAttribute('src')).toContain('video.svg');
    });
  });

  describe('media-type icon overlay', () => {
    it('shows the type icon by default and hides it when disabled', () => {
      const { rerender } = renderWithProviders(<MediaCard media={makeVideo()} />);
      // The overlay icon is decorative (no accessible name); MUI's data-testid is
      // the only stable handle, so this is the sanctioned getByTestId exception.
      expect(screen.getByTestId('YouTubeIcon')).toBeInTheDocument();

      rerender(<MediaCard media={makeVideo()} showMediaTypeIcon={false} />);
      expect(screen.queryByTestId('YouTubeIcon')).not.toBeInTheDocument();
    });
  });

  describe('interaction', () => {
    it('calls onClick with the media item when an onClick handler is provided', async () => {
      const onClick = vi.fn();
      const video = makeVideo();
      const { user } = renderWithProviders(
        <MediaCard media={video} onClick={onClick} />,
      );

      // Clicking any child bubbles to the wrapping Box's onClick.
      await user.click(screen.getByText('Test Video'));
      expect(onClick).toHaveBeenCalledWith(video);
    });

    it('links to the media profile when no onClick handler is provided', () => {
      const video = makeVideo();
      renderWithProviders(<MediaCard media={video} />);
      expect(screen.getByRole('link')).toHaveAttribute('href', `/media/${video.id}`);
    });
  });

  describe('variants', () => {
    it('hides notes in the compact variant', () => {
      const media = makeBook({ notes: 'Hidden in compact' });
      const { rerender } = renderWithProviders(
        <MediaCard media={media} variant="default" />,
      );
      expect(screen.getByText('Hidden in compact')).toBeInTheDocument();

      rerender(<MediaCard media={media} variant="compact" />);
      expect(screen.queryByText('Hidden in compact')).not.toBeInTheDocument();
    });
  });
});
