import { describe, it, expect } from 'vitest';
import { useLocation } from 'react-router-dom';
import { renderWithProviders, screen } from '@/test/test-utils';
import { MediaListItem } from './MediaListItem';

// MediaListItem navigates imperatively via useNavigate on row click. These tests assert
// the resulting path for each item shape, mirroring SearchResultCard.getItemPath so the
// two views stay converged (regression guard for RAS-109: highlights in list view used to
// fall through to /media/:id).

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
};

const renderRow = (overrides = {}) =>
  renderWithProviders(
    <>
      <MediaListItem item={{ ...baseItem, ...overrides }} />
      <LocationProbe />
    </>,
  );

const clickRow = async (user) => {
  await user.click(screen.getByText('Test Item'));
};

describe('MediaListItem navigation', () => {
  it('routes a highlight to /highlight/:id', async () => {
    const { user } = renderRow({ isHighlight: true, mediaType: 'Highlight' });

    await clickRow(user);

    expect(screen.getByTestId('location')).toHaveTextContent('/highlight/abc-123');
  });

  it('routes a note to /note/:id', async () => {
    const { user } = renderRow({ isNote: true, mediaType: 'Note' });

    await clickRow(user);

    expect(screen.getByTestId('location')).toHaveTextContent('/note/abc-123');
  });

  it('routes a plain media item to /media/:id', async () => {
    const { user } = renderRow();

    await clickRow(user);

    expect(screen.getByTestId('location')).toHaveTextContent('/media/abc-123');
  });
});
