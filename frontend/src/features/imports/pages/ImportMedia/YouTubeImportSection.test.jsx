import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { useLocation } from 'react-router-dom';
import { renderWithProviders, screen, waitFor, within } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import YouTubeImportSection from './YouTubeImportSection';

vi.setConfig({ testTimeout: 30000 });

const LocationProbe = () => {
  const location = useLocation();
  return <div data-testid="location">{location.pathname}</div>;
};

const renderExpanded = () => {
  const onSnackbar = vi.fn();
  const utils = renderWithProviders(
    <>
      <YouTubeImportSection expanded="youtube" onAccordionChange={() => () => {}} onSnackbar={onSnackbar} />
      <LocationProbe />
    </>,
    { route: '/import-media' },
  );
  return { ...utils, onSnackbar };
};

// Switches the section to "By YouTube URL", types a video URL and presses Import.
const importByUrl = async (user, url) => {
  const [importMethod] = screen.getAllByRole('combobox');
  await user.click(importMethod);
  await user.click(within(await screen.findByRole('listbox')).getByRole('option', { name: /by youtube url/i }));
  await user.type(screen.getByRole('textbox', { name: /youtube url/i }), url);
  await user.click(screen.getByRole('button', { name: /^import$/i }));
};

describe('YouTubeImportSection URL import', () => {
  it('says the video is already in the library on a 200 and opens the stored item, not a new one', async () => {
    // The backend answers 200 with the stored row when the id is already in the library.
    server.use(http.post(`${API_BASE}/youtube/import/video/:videoId`, () =>
      HttpResponse.json({ id: 'stored-video-id', title: 'Already here' }, { status: 200 })));
    const { user, onSnackbar } = renderExpanded();

    await importByUrl(user, 'https://www.youtube.com/watch?v=S-WmoT4_nxQ');

    await waitFor(() => expect(onSnackbar).toHaveBeenCalledWith(expect.objectContaining({
      severity: 'info',
      message: expect.stringMatching(/already in your library/i),
    })));
    await waitFor(() => expect(screen.getByTestId('location')).toHaveTextContent('/media/stored-video-id'), { timeout: 4000 });
  });

  it('shows the backend error text when an import fails', async () => {
    server.use(http.post(`${API_BASE}/youtube/import/video/:videoId`, () =>
      HttpResponse.json({ error: "YouTube's daily quota is used up. Try again after it resets.", quotaExceeded: true }, { status: 503 })));
    const { user, onSnackbar } = renderExpanded();

    await importByUrl(user, 'https://www.youtube.com/watch?v=S-WmoT4_nxQ');

    await waitFor(() => expect(onSnackbar).toHaveBeenCalledWith(expect.objectContaining({
      severity: 'error',
      message: expect.stringMatching(/daily quota is used up/i),
    })));
    expect(screen.getByTestId('location')).toHaveTextContent('/import-media');
  });
});
