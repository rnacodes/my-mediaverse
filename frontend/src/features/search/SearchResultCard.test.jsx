import { describe, it, expect, vi } from 'vitest';
import { useLocation } from 'react-router-dom';
import { renderWithProviders, screen } from '@/test/test-utils';
import { SearchResultCard } from './SearchResultCard';

// The card is wrapped in a router Link. These tests guard the bulk-selection checkbox,
// which must toggle the item without triggering that link.

const LocationProbe = () => {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
};

const baseItem = {
  id: 'abc-123',
  title: 'Test Item',
  mediaType: 'Book',
  status: 'Uncharted',
  topics: [],
  dateAdded: '2026-01-01',
};

const renderCard = (overrides = {}, props = {}) =>
  renderWithProviders(
    <>
      <SearchResultCard item={{ ...baseItem, ...overrides }} {...props} />
      <LocationProbe />
    </>,
  );

describe('SearchResultCard navigation', () => {
  it('links a plain media item to /media/:id', () => {
    renderCard();

    expect(screen.getByRole('link')).toHaveAttribute('href', '/media/abc-123');
  });

  it('links a highlight to /highlight/:id', () => {
    renderCard({ isHighlight: true, mediaType: 'Highlight' });

    expect(screen.getByRole('link')).toHaveAttribute('href', '/highlight/abc-123');
  });

  it('navigates when the title is clicked', async () => {
    const { user } = renderCard();

    await user.click(screen.getByText('Test Item'));

    expect(screen.getByTestId('location')).toHaveTextContent('/media/abc-123');
  });
});

describe('SearchResultCard selection', () => {
  it('does not render a checkbox unless showCheckbox is set', () => {
    renderCard();

    expect(screen.queryByRole('checkbox')).not.toBeInTheDocument();
  });

  it('renders the checkbox outside the link so clicks cannot trigger navigation', () => {
    renderCard({}, { showCheckbox: true, onToggleSelect: vi.fn() });

    const checkbox = screen.getByRole('checkbox');
    expect(screen.getByRole('link')).not.toContainElement(checkbox);
  });

  it('calls onToggleSelect with the item id when the checkbox is clicked, without navigating', async () => {
    const onToggleSelect = vi.fn();
    const { user } = renderCard({}, { showCheckbox: true, onToggleSelect });

    await user.click(screen.getByRole('checkbox'));

    expect(onToggleSelect).toHaveBeenCalledTimes(1);
    expect(onToggleSelect).toHaveBeenCalledWith('abc-123');
    expect(screen.getByTestId('location')).not.toHaveTextContent('/media/');
  });

  it('reflects the isSelected prop', () => {
    renderCard({}, { showCheckbox: true, isSelected: true, onToggleSelect: vi.fn() });

    expect(screen.getByRole('checkbox')).toBeChecked();
  });
});

describe('SearchResultCard website link health', () => {
  it('flags a website whose last link check failed', () => {
    renderCard({ mediaType: 'Website', linkStatus: 404 });

    expect(screen.getByText('Link broken')).toBeInTheDocument();
  });

  it('flags a website the checker could not reach at all (status 0)', () => {
    renderCard({ mediaType: 'Website', linkStatus: 0 });

    expect(screen.getByText('Link broken')).toBeInTheDocument();
  });

  it('shows nothing for a healthy or unchecked website', () => {
    renderCard({ mediaType: 'Website', linkStatus: 200 });
    expect(screen.queryByText('Link broken')).not.toBeInTheDocument();

    renderCard({ mediaType: 'Website', linkStatus: null });
    expect(screen.queryByText('Link broken')).not.toBeInTheDocument();
  });
});

describe('SearchResultCard podcasts', () => {
  it('links a series to its profile page and an episode to the media page', () => {
    renderCard({ id: 'series-1', title: 'The Show', mediaType: 'Podcast', podcastType: 'Series' });
    expect(screen.getByRole('link', { name: /the show/i })).toHaveAttribute('href', '/podcast-series/series-1');

    renderCard({ id: 'ep-1', title: 'Pilot', mediaType: 'Podcast', podcastType: 'Episode', seriesId: 'series-1' });
    expect(screen.getByRole('link', { name: /pilot/i })).toHaveAttribute('href', '/media/ep-1');
  });

  it('trusts podcastType over a missing seriesId', () => {
    // An episode whose parent id did not make it into the result must not route as a series.
    renderCard({ id: 'ep-2', title: 'Orphan', mediaType: 'Podcast', podcastType: 'Episode' });

    expect(screen.getByRole('link', { name: /orphan/i })).toHaveAttribute('href', '/media/ep-2');
  });

  it('credits an episode to its show', () => {
    renderCard({
      id: 'ep-3', title: 'Pilot', mediaType: 'Podcast', podcastType: 'Episode',
      seriesId: 'series-1', seriesTitle: 'The Show', publisher: 'Test Network',
    });

    expect(screen.getByText('The Show')).toBeInTheDocument();
  });
});

describe('SearchResultCard TV shows and episodes', () => {
  it('links a show straight to its profile page with first-air year and rating', () => {
    renderCard({ id: 'show-1', title: 'Chernobyl', mediaType: 'TVShow', tvType: 'Show', releaseYear: 2019, tmdbRating: 8.7, creator: 'Craig Mazin' });

    expect(screen.getByRole('link', { name: /chernobyl/i })).toHaveAttribute('href', '/tv-show/show-1');
    expect(screen.getByText('2019 • 8.7★')).toBeInTheDocument();
    expect(screen.getByText('Created by Craig Mazin')).toBeInTheDocument();
  });

  it('links an episode to the media page, credited to its show with its identifier', () => {
    renderCard({
      id: 'ep-1', title: '1:23:45', mediaType: 'TVShow', tvType: 'Episode',
      showId: 'show-1', showTitle: 'Chernobyl', seasonNumber: 1, episodeNumber: 1, tmdbRating: 9.4,
    });

    expect(screen.getByRole('link', { name: /1:23:45/i })).toHaveAttribute('href', '/media/ep-1');
    expect(screen.getByText('Chernobyl')).toBeInTheDocument();
    expect(screen.getByText('S1E1 • 9.4★')).toBeInTheDocument();
  });

  it('sends a TV document without tv_type through the generic profile', () => {
    renderCard({ id: 'old-1', title: 'Old Show', mediaType: 'TVShow' });

    expect(screen.getByRole('link', { name: /old show/i })).toHaveAttribute('href', '/media/old-1');
  });
});
