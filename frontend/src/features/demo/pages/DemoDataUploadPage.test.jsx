import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import DemoDataUploadPage from './DemoDataUploadPage';

// Smoke test only (RAS-34): this big import page is left as a future decomposition
// project, so we just mount it and assert the primary heading. No mount-time network
// calls fire — the topic/genre searches are disabled until the user types.
//
// We settle with a cheap findByText (resolves on first render) before asserting the
// heading once: polling the large form tree with findByRole recomputes accessible names
// on every retry and is slow enough to time out under jsdom.

describe('DemoDataUploadPage', () => {
  it('mounts and renders its primary heading', async () => {
    renderWithProviders(<DemoDataUploadPage />, { route: '/demo-data-upload' });

    expect(await screen.findByText('Demo Data Upload')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Demo Data Upload' })).toBeInTheDocument();
  });
});
