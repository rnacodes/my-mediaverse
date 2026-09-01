import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import SearchBar from './SearchBar';

// The dropdown searches via Typesense: GET /search (media) and GET /search/mixlists.
// Both return Typesense-shaped { found, hits: [{ document }] } payloads.
const mockSearch = ({ media = [], mixlists = [] } = {}) =>
  server.use(
    http.get(`${API_BASE}/search`, () =>
      HttpResponse.json({
        found: media.length,
        hits: media.map((m) => ({
          document: { id: m.id, title: m.title, media_type: m.mediaType },
        })),
      }),
    ),
    http.get(`${API_BASE}/search/mixlists`, () =>
      HttpResponse.json({
        found: mixlists.length,
        hits: mixlists.map((m) => ({
          document: { id: m.id, name: m.name, media_item_count: m.itemCount ?? 0 },
        })),
      }),
    ),
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

  it('runs a debounced search on typed input and surfaces results in the panel without submitting', async () => {
    mockSearch({ media: [{ id: 'm1', title: 'The Matrix', mediaType: 'Movie' }] });
    const onSearch = vi.fn();

    const { user } = renderWithProviders(
      <SearchBar onSearch={onSearch} />,
      { route: '/' },
    );

    await user.type(screen.getByRole('textbox'), 'matrix');

    // The debounced search resolves and opens the suggestions panel...
    expect(await screen.findByText('The Matrix')).toBeInTheDocument();
    // ...but typing must not submit — onSearch (navigation) fires only on Enter/icon click.
    expect(onSearch).not.toHaveBeenCalled();
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

  it('clears the input without submitting a search', async () => {
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
    // Clearing is not a submit, so it must not trigger onSearch (navigation).
    expect(onSearch).not.toHaveBeenCalled();
  });

  it('offers a View all mixlists link under the mixlist suggestions', async () => {
    mockSearch({ mixlists: [{ id: 'mx1', name: 'Road Trip Mix', itemCount: 4 }] });

    const { user } = renderWithProviders(<SearchBar />, { route: '/' });

    await user.type(screen.getByRole('textbox'), 'mix');

    expect(await screen.findByText('Road Trip Mix')).toBeInTheDocument();
    const viewAll = screen.getByRole('link', { name: /view all mixlists/i });
    expect(viewAll).toHaveAttribute('href', '/search?searchMode=mixlists');
  });
});
