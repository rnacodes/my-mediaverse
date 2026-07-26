import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import BookImportSection from './BookImportSection';

// BookImportSection searches/imports books from a selectable source (Google Books
// or Open Library). Source selection swaps the endpoints, the import payload key
// (volumeId vs openLibraryKey), and the external details link; switching sources
// clears any previous results. Import success shows an Alert then navigates after
// a 1.5s timer (the alert is asserted; the timed nav is not). The MUI-heavy
// accordion renders slowly on CI-class hardware, so the per-test timeout is raised.
vi.setConfig({ testTimeout: 30000 });
const renderExpanded = () =>
  renderWithProviders(
    <BookImportSection expanded="books" onAccordionChange={() => () => {}} />,
    { route: '/import-media' },
  );

const GOOGLE_RESULT = {
  key: 'EMMWDwAAQBAJ',
  title: 'Google Result',
  authors: ['Google Author'],
  firstPublishYear: 2019,
};

const OPENLIBRARY_RESULT = {
  key: '/works/OL45883W',
  title: 'Open Library Result',
  authors: ['OL Author'],
  firstPublishYear: 1965,
};

const selectSource = async (user, label) => {
  await user.click(screen.getByRole('combobox', { name: /source/i }));
  await user.click(screen.getByRole('option', { name: label }));
};

const searchFor = async (user, query) => {
  await user.type(screen.getByRole('textbox', { name: /search books/i }), query);
  await user.click(screen.getByRole('button', { name: /search/i }));
};

describe('BookImportSection', () => {
  it('searches Google Books by default and links result details to Google', async () => {
    server.use(
      http.get(`${API_BASE}/book/search-googlebooks`, () =>
        HttpResponse.json([GOOGLE_RESULT]),
      ),
    );

    const { user } = renderExpanded();

    await searchFor(user, 'dune');

    expect(await screen.findByText('Google Result')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /view details/i })).toHaveAttribute(
      'href',
      'https://books.google.com/books?id=EMMWDwAAQBAJ',
    );
  });

  it('imports a Google Books search result with a volumeId payload', async () => {
    let captured;
    server.use(
      http.get(`${API_BASE}/book/search-googlebooks`, () =>
        HttpResponse.json([GOOGLE_RESULT]),
      ),
      http.post(`${API_BASE}/book/import-from-googlebooks`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json({ id: 'new-book' });
      }),
    );

    const { user } = renderExpanded();

    await searchFor(user, 'dune');
    await screen.findByText('Google Result');
    await user.click(screen.getByRole('button', { name: /^import$/i }));

    expect(await screen.findByText(/imported successfully/i)).toBeInTheDocument();
    expect(captured).toEqual({
      volumeId: 'EMMWDwAAQBAJ',
      title: 'Google Result',
      author: 'Google Author',
    });
  });

  it('searches Open Library when selected and links result details to Open Library', async () => {
    server.use(
      http.get(`${API_BASE}/book/search-openlibrary`, () =>
        HttpResponse.json([OPENLIBRARY_RESULT]),
      ),
    );

    const { user } = renderExpanded();

    await selectSource(user, 'Open Library');
    await searchFor(user, 'dune');

    expect(await screen.findByText('Open Library Result')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /view details/i })).toHaveAttribute(
      'href',
      'https://openlibrary.org/works/OL45883W',
    );
  });

  it('imports an Open Library search result with an openLibraryKey payload', async () => {
    let captured;
    server.use(
      http.get(`${API_BASE}/book/search-openlibrary`, () =>
        HttpResponse.json([OPENLIBRARY_RESULT]),
      ),
      http.post(`${API_BASE}/book/import-from-openlibrary`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json({ id: 'new-book' });
      }),
    );

    const { user } = renderExpanded();

    await selectSource(user, 'Open Library');
    await searchFor(user, 'dune');
    await screen.findByText('Open Library Result');
    await user.click(screen.getByRole('button', { name: /^import$/i }));

    expect(await screen.findByText(/imported successfully/i)).toBeInTheDocument();
    expect(captured).toEqual({
      openLibraryKey: '/works/OL45883W',
      title: 'Open Library Result',
      author: 'OL Author',
    });
  });

  it('clears previous search results when the source changes', async () => {
    server.use(
      http.get(`${API_BASE}/book/search-googlebooks`, () =>
        HttpResponse.json([GOOGLE_RESULT]),
      ),
    );

    const { user } = renderExpanded();

    await searchFor(user, 'dune');
    await screen.findByText('Google Result');

    await selectSource(user, 'Open Library');

    expect(screen.queryByText('Google Result')).not.toBeInTheDocument();
  });
});
