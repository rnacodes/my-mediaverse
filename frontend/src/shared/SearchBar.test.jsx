import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import SearchBar from './SearchBar';

// MVP coverage (RAS-31): exercise the four key interactions — empty input, typed
// (debounced) input, submit, and clear — plus both outcome directions of a search.
//
// SearchBar fetches nothing on mount. A search (debounced 300ms after typing, on
// Enter, or via the search icon) runs searchAll(), which hits TWO endpoints in
// parallel: GET /media/search and GET /mixlist/search. onUnhandledRequest:'error'
// means any typing test must mock BOTH. The page's contract with its parent is the
// `onSearch` callback: it fires (query, results) on a successful search and ('')
// on clear. The clear icon only renders while there is a query.
//
// Real timers are used: the 300ms debounce resolves well within waitFor's window,
// which avoids the fragility of pairing fake timers with userEvent + MSW.

const mockSearch = ({ media = [], mixlists = [] } = {}) =>
  server.use(
    http.get(`${API_BASE}/media/search`, () => HttpResponse.json(media)),
    http.get(`${API_BASE}/mixlist/search`, () => HttpResponse.json(mixlists)),
  );

describe('SearchBar', () => {
  it('renders the empty state with no clear icon and no suggestions panel', () => {
    renderWithProviders(<SearchBar />, { route: '/' });

    expect(
      screen.getByRole('textbox', { name: '' }) ||
        screen.getByPlaceholderText('Search your media library...'),
    ).toBeInTheDocument();
    expect(screen.queryByTestId('ClearIcon')).not.toBeInTheDocument();
    expect(screen.queryByText(/no results found/i)).not.toBeInTheDocument();
  });

  it('runs a debounced search on typed input and surfaces results via panel and onSearch', async () => {
    mockSearch({ media: [{ id: 'm1', title: 'The Matrix', mediaType: 'Movie' }] });
    const onSearch = vi.fn();

    const { user } = renderWithProviders(
      <SearchBar onSearch={onSearch} />,
      { route: '/' },
    );

    await user.type(screen.getByRole('textbox'), 'matrix');

    // The debounced search resolves, opens the suggestions panel, and reports back.
    expect(await screen.findByText('The Matrix')).toBeInTheDocument();
    await waitFor(() =>
      expect(onSearch).toHaveBeenCalledWith('matrix', expect.anything()),
    );
  });

  it('shows the no-results message when a typed search returns nothing', async () => {
    mockSearch({ media: [], mixlists: [] });

    const { user } = renderWithProviders(<SearchBar />, { route: '/' });

    await user.type(screen.getByRole('textbox'), 'zzznope');

    expect(await screen.findByText(/no results found for "zzznope"/i)).toBeInTheDocument();
  });

  it('submits on Enter and reports the results through onSearch', async () => {
    mockSearch({ media: [{ id: 'm2', title: 'Submitted Result', mediaType: 'Book' }] });
    const onSearch = vi.fn();

    const { user } = renderWithProviders(
      <SearchBar onSearch={onSearch} />,
      { route: '/' },
    );

    await user.type(screen.getByRole('textbox'), 'dune{Enter}');

    await waitFor(() =>
      expect(onSearch).toHaveBeenCalledWith('dune', expect.anything()),
    );
    expect(await screen.findByText('Submitted Result')).toBeInTheDocument();
  });

  it('clears the input and notifies onSearch with an empty query', async () => {
    mockSearch({ media: [{ id: 'm3', title: 'Anything', mediaType: 'Movie' }] });
    const onSearch = vi.fn();

    const { user } = renderWithProviders(
      <SearchBar onSearch={onSearch} />,
      { route: '/' },
    );

    const input = screen.getByRole('textbox');
    await user.type(input, 'matrix');
    expect(input).toHaveValue('matrix');

    // The clear affordance only exists once there is a query.
    await user.click(screen.getByTestId('ClearIcon'));

    expect(input).toHaveValue('');
    expect(onSearch).toHaveBeenCalledWith('');
  });
});
