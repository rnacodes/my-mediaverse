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
