import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import { makeBook } from '@/test/factories/media';
import { makePodcastEpisode } from '@/test/factories/podcast';
import MediaDetailAccordion from './MediaDetailAccordion';

// Smoke test only - left as a future decomposition

describe('MediaDetailAccordion', () => {
  it('mounts and renders the details heading for its media type', async () => {
    renderWithProviders(<MediaDetailAccordion mediaItem={makeBook()} navigate={() => {}} />);

    expect(await screen.findByRole('heading', { name: 'Book Details' })).toBeInTheDocument();
  });

  describe('book "Fetch Description" button', () => {
    const expandDetails = async (user) => {
      await user.click(screen.getByRole('button', { name: /book details/i }));
    };

    it('is enabled for a book with an author but no ISBN', async () => {
      const { user } = renderWithProviders(
        <MediaDetailAccordion
          mediaItem={makeBook({ isbn: null, title: 'Dune', author: 'Frank Herbert' })}
          navigate={() => {}}
        />,
      );

      await expandDetails(user);

      expect(screen.getByRole('button', { name: /fetch description/i })).toBeEnabled();
      expect(screen.queryByText(/requires isbn or title \+ author/i)).not.toBeInTheDocument();
    });

    it('is disabled with a hint when the book has no ISBN and only the placeholder author', async () => {
      const { user } = renderWithProviders(
        <MediaDetailAccordion
          mediaItem={makeBook({ isbn: null, title: 'Mystery', author: 'Unknown Author' })}
          navigate={() => {}}
        />,
      );

      await expandDetails(user);

      expect(screen.getByRole('button', { name: /fetch description/i })).toBeDisabled();
      expect(screen.getByText(/requires isbn or title \+ author/i)).toBeInTheDocument();
    });
  });

  describe('podcast episode audio link', () => {
    // Details live inside a collapsed accordion; expand it first, as a visitor would.
    const expandDetails = async (user) => {
      await user.click(screen.getByRole('button', { name: /podcast details/i }));
    };

    it('renders a download link pointing at the raw audio URL when present', async () => {
      const audioLink = 'https://audio.listennotes.com/e/p/abc123/';
      const { user } = renderWithProviders(
        <MediaDetailAccordion mediaItem={makePodcastEpisode({ audioLink })} navigate={() => {}} />,
      );

      await expandDetails(user);

      const link = screen.getByRole('link', { name: /click to download audio/i });
      expect(link).toHaveAttribute('href', audioLink);
      expect(link).toHaveAttribute('target', '_blank');
    });

    it('omits the audio link when the episode has no audioLink', async () => {
      const { user } = renderWithProviders(
        <MediaDetailAccordion mediaItem={makePodcastEpisode()} navigate={() => {}} />,
      );

      await expandDetails(user);

      expect(screen.queryByRole('link', { name: /download audio/i })).not.toBeInTheDocument();
    });
  });
});
