import { describe, it, expect, vi, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeNote } from '@/test/factories/note';
import { useDemoWriteBlocked } from '@/features/demo/useDemoWriteBlocked';
import RelatedNotesSection from './RelatedNotesSection';

// Linking and unlinking a note are writes: both are disabled on the public demo, while the
// linked notes themselves stay readable.

vi.mock('@/features/demo/useDemoWriteBlocked', () => {
  const useDemoWriteBlocked = vi.fn(() => false);
  return { useDemoWriteBlocked, default: useDemoWriteBlocked };
});

afterEach(() => {
  vi.mocked(useDemoWriteBlocked).mockReturnValue(false);
});

const render = () => {
  server.use(
    http.get(`${API_BASE}/note/for-media/:id`, () => HttpResponse.json([makeNote({ title: 'Viewing notes' })])),
  );
  return renderWithProviders(
    <RelatedNotesSection mediaItem={{ id: 'show-1', title: 'Chernobyl' }} setSnackbar={() => {}} onUpdate={() => {}} />,
  );
};

describe('RelatedNotesSection demo guards', () => {
  it('keeps Link Note and unlink enabled off the demo', async () => {
    render();

    await screen.findByText('Viewing notes');

    expect(screen.getByRole('button', { name: /^link note$/i })).toBeEnabled();
    expect(screen.getByRole('button', { name: /unlink note/i })).toBeEnabled();
  });

  it('disables Link Note and unlink while the demo blocks writes', async () => {
    vi.mocked(useDemoWriteBlocked).mockReturnValue(true);

    render();

    expect(await screen.findByRole('link', { name: 'Viewing notes' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^link note$/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /unlink note/i })).toBeDisabled();
  });
});
