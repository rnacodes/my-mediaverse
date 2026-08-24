import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import Search from './Search';

// Serves one highlight alongside the two seeded media items ("Test Book" / "Test Movie").
const serveOneHighlight = () => {
  server.use(
    http.get(`${API_BASE}/search/highlights`, () =>
      HttpResponse.json({
        found: 1,
        out_of: 1,
        page: 1,
        hits: [
          {
            document: {
              id: 'hl-1',
              text: 'Test highlight text',
              title: 'Test Highlight',
              category: 'books',
              tags: [],
              created_at: 1700000000,
            },
          },
        ],
      }),
    ),
  );
};

describe('Search page', () => {
  it('browses all media by default (as served at /all-media)', async () => {
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media' });

    // Both seeded media items render — no filter selection required.
    expect(await screen.findByText('Test Book')).toBeInTheDocument();
    expect(await screen.findByText('Test Movie')).toBeInTheDocument();

    // The "please select filters" empty prompt must NOT appear in browse-all mode.
    expect(screen.queryByText('Select filters to search')).not.toBeInTheDocument();
  });

  it('narrows to a single media type from the mediaType URL param', async () => {
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media?mediaType=Book' });

    // Only the Book comes back — the filter param drove a media_type:=Book query.
    expect(await screen.findByText('Test Book')).toBeInTheDocument();
    expect(screen.queryByText('Test Movie')).not.toBeInTheDocument();
  });
});

describe('Search bulk actions', () => {
  it('bulk delete routes media and highlight ids to their own endpoints', async () => {
    serveOneHighlight();
    const deleted = { media: null, highlights: null, notes: null };
    server.use(
      http.delete(`${API_BASE}/media/bulk`, async ({ request }) => {
        deleted.media = (await request.json()).ids;
        return HttpResponse.json({ deletedCount: deleted.media.length });
      }),
      http.delete(`${API_BASE}/highlight/bulk`, async ({ request }) => {
        deleted.highlights = (await request.json()).ids;
        return HttpResponse.json({ deletedCount: deleted.highlights.length });
      }),
      http.delete(`${API_BASE}/note/bulk`, async ({ request }) => {
        deleted.notes = (await request.json()).ids;
        return HttpResponse.json({ deletedCount: deleted.notes.length });
      }),
    );

    const { user } = renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media' });
    expect(await screen.findByText('Test highlight text')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Select All' }));
    await user.click(screen.getByRole('button', { name: /^Delete \(3\)$/ }));

    // Confirmation dialog names what is being deleted, per kind.
    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText(/2 media items, 1 highlight/)).toBeInTheDocument();
    await user.click(within(dialog).getByRole('button', { name: 'Delete' }));

    expect(await screen.findByText(/Successfully deleted 2 media, 1 highlights!/)).toBeInTheDocument();
    expect(deleted.media).toEqual(['ts-book', 'ts-movie']);
    expect(deleted.highlights).toEqual(['hl-1']);
    expect(deleted.notes).toBeNull(); // no notes selected — endpoint never hit
  });

  it('disables Add to Mixlist while a highlight is selected', async () => {
    serveOneHighlight();
    const { user } = renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media' });
    expect(await screen.findByText('Test highlight text')).toBeInTheDocument();

    // Media-only selection keeps the button enabled.
    await user.click(screen.getByRole('checkbox', { name: 'Select Test Book' }));
    expect(screen.getByRole('button', { name: 'Add to Mixlist' })).toBeEnabled();

    // Adding a highlight disables it (highlights have no mixlist relationship).
    await user.click(screen.getByRole('checkbox', { name: 'Select Test Highlight' }));
    expect(screen.getByRole('button', { name: 'Add to Mixlist' })).toBeDisabled();
  });
});
