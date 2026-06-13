import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import MediaInfoCard from './MediaInfoCard';

// MediaInfoCard is a pure presentational component: it does no fetching and takes
// formatter functions as props. These tests cover the podcast-type chip logic and
// the thumbnail URL it feeds to the hero image.

// Identity/no-op stubs for the formatter props. formatMediaType/formatStatus echo
// their input so the media-type and status chips have predictable, non-colliding
// labels (neither equals "Series"/"Episode").
const formatterProps = {
  formatMediaType: (type) => type,
  formatStatus: (status) => status,
  getMediaTypeColor: () => '#000000',
  getStatusColor: () => '#000000',
  getRatingIcon: () => null,
  getRatingText: () => 'Unrated',
};

const renderCard = (mediaItem) =>
  renderWithProviders(<MediaInfoCard mediaItem={mediaItem} {...formatterProps} />);

describe('MediaInfoCard podcast-type chip', () => {
  it('renders a "Series" chip for a podcast with podcastType "Series"', () => {
    renderCard({ mediaType: 'Podcast', status: 'Consuming', podcastType: 'Series' });

    expect(screen.getByText('Series')).toBeInTheDocument();
    expect(screen.queryByText('Episode')).not.toBeInTheDocument();
  });

  it('renders an "Episode" chip for a podcast with podcastType "Episode"', () => {
    renderCard({ mediaType: 'Podcast', status: 'Consuming', podcastType: 'Episode' });

    expect(screen.getByText('Episode')).toBeInTheDocument();
    expect(screen.queryByText('Series')).not.toBeInTheDocument();
  });

  it('renders no podcast-type chip when podcastType is undefined', () => {
    renderCard({ mediaType: 'Podcast', status: 'Consuming' });

    expect(screen.queryByText('Series')).not.toBeInTheDocument();
    expect(screen.queryByText('Episode')).not.toBeInTheDocument();
  });

  it('renders no podcast-type chip for non-podcast media', () => {
    // A Book carrying a stray podcastType must still not render the chip — the
    // guard keys off mediaType === 'Podcast', not the field alone.
    renderCard({ mediaType: 'Book', status: 'Consuming', podcastType: 'Series' });

    expect(screen.queryByText('Series')).not.toBeInTheDocument();
    expect(screen.queryByText('Episode')).not.toBeInTheDocument();
  });
});

describe('MediaInfoCard thumbnail', () => {
  it('loads a podcast thumbnail directly from its stored URL (no proxy)', () => {
    const thumbnail = 'https://cdn.example.com/podcast-cover.jpg';
    renderCard({ mediaType: 'Podcast', status: 'Consuming', title: 'My Podcast', thumbnail });

    const img = screen.getByRole('img', { name: 'My Podcast' });
    expect(img).toHaveAttribute('src', thumbnail);
  });

  it('shows the per-type placeholder image when no thumbnail is set', () => {
    renderCard({ mediaType: 'Podcast', status: 'Consuming', title: 'My Podcast' });

    const img = screen.getByRole('img', { name: 'My Podcast' });
    expect(img.getAttribute('src')).toContain('podcast.svg');
  });
});
