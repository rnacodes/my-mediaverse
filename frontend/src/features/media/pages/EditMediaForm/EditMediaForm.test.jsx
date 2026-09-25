import { describe, it, expect, vi, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeBook } from '@/test/factories/media';
import { makeNote } from '@/test/factories/note';
import EditMediaForm from './EditMediaForm';

// The form is heavy, so the per-test timeout is raised.
vi.setConfig({ testTimeout: 30000 });

vi.mock('@/features/demo/useDemoWriteBlocked', () => {
  const useDemoWriteBlocked = vi.fn(() => false);
  return { useDemoWriteBlocked, default: useDemoWriteBlocked };
});
import { useDemoWriteBlocked } from '@/features/demo/useDemoWriteBlocked';

afterEach(() => {
  vi.mocked(useDemoWriteBlocked).mockReturnValue(false);
});

const MEDIA_ID = 'book-edit-1';
const seedBook = () =>
  server.use(
    http.get(`${API_BASE}/media/:id`, ({ params }) => HttpResponse.json(makeBook({ id: params.id, title: 'Dune' }))),
    http.get(`${API_BASE}/book/:id`, ({ params }) => HttpResponse.json(makeBook({ id: params.id, title: 'Dune' }))),
    http.get(`${API_BASE}/note/for-media/:id`, () => HttpResponse.json([makeNote({ title: 'Reading notes' })])),
  );

const render = () =>
  renderWithProviders(<EditMediaForm />, { route: `/media/${MEDIA_ID}/edit`, path: '/media/:id/edit' });

const writeButtons = () => [
  screen.getByRole('button', { name: /save changes/i }),
  screen.getByRole('button', { name: /delete media/i }),
  screen.getByRole('button', { name: /add to mixlist/i }),
  screen.getByRole('button', { name: /^link note$/i }),
  screen.getByRole('button', { name: /unlink note/i }),
];

describe('EditMediaForm demo guards', () => {
  it('keeps every write enabled off the demo', async () => {
    seedBook();
    render();

    await screen.findByText('Reading notes');

    for (const button of writeButtons()) expect(button).toBeEnabled();
    expect(screen.getByRole('button', { name: /cancel/i })).toBeEnabled();
  });

  it('disables Save, Delete, Add to Mixlist, Link Note and unlink note while the demo blocks writes', async () => {
    vi.mocked(useDemoWriteBlocked).mockReturnValue(true);
    seedBook();
    render();

    await screen.findByText('Reading notes');

    for (const button of writeButtons()) expect(button).toBeDisabled();
    // Navigation stays available.
    expect(screen.getByRole('button', { name: /cancel/i })).toBeEnabled();
    expect(screen.getByRole('button', { name: /back/i })).toBeEnabled();
  });
});
