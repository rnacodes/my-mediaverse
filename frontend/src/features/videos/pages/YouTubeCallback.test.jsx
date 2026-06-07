import { describe, it, expect, vi } from 'vitest';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders, screen, act } from '@/test/test-utils';
import YouTubeCallback from './YouTubeCallback';

// YouTubeCallback is a pure OAuth-redirect handler — it makes no network calls. It reads
// `code` / `error` / `state` from the query string, flips between loading/success/error,
// and on success auto-navigates to /import-media after 3s. To observe navigation we mount
// it inside a Routes tree with sentinel destination routes and drive the initial entry
// via MemoryRouter (renderWithProviders' `route`).
const renderCallback = (route, options = {}) =>
  renderWithProviders(
    <Routes>
      <Route path="/youtube/callback" element={<YouTubeCallback />} />
      <Route path="/import-media" element={<div>Import Media Page</div>} />
      <Route path="/" element={<div>Home Page</div>} />
    </Routes>,
    { route, ...options },
  );

describe('YouTubeCallback', () => {
  it('shows the success state when an authorization code is present', () => {
    renderCallback('/youtube/callback?code=abc123&state=xyz');

    expect(screen.getByText(/authentication successful/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /go to home/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /go to import/i })).toBeInTheDocument();
  });

  it('shows the OAuth error state when an error param is present', () => {
    renderCallback('/youtube/callback?error=access_denied');

    expect(screen.getByText(/there was an error during youtube authentication/i)).toBeInTheDocument();
    expect(screen.getByText(/oauth error: access_denied/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /try again/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /go home/i })).toBeInTheDocument();
  });

  it('shows the missing-code error state when no code or error is present', () => {
    renderCallback('/youtube/callback');

    expect(screen.getByText(/the authorization code was not found/i)).toBeInTheDocument();
    expect(screen.getByText(/no authorization code received/i)).toBeInTheDocument();
  });

  it('navigates to the import page when "Go to Import" is clicked', async () => {
    const { user } = renderCallback('/youtube/callback?code=abc123');

    await user.click(screen.getByRole('button', { name: /go to import/i }));

    expect(screen.getByText('Import Media Page')).toBeInTheDocument();
  });

  it('auto-redirects to the import page after the timeout', () => {
    vi.useFakeTimers();
    try {
      renderCallback('/youtube/callback?code=abc123&state=xyz');

      expect(screen.getByText(/authentication successful/i)).toBeInTheDocument();

      act(() => {
        vi.advanceTimersByTime(3000);
      });

      expect(screen.getByText('Import Media Page')).toBeInTheDocument();
    } finally {
      vi.useRealTimers();
    }
  });
});
