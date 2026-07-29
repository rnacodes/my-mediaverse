import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { render, screen, waitFor, act } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';

import ApiErrorListener from './ApiErrorListener';

vi.mock('@/utils/demoMode', () => ({
  isDemoMode: vi.fn(() => false),
}));

import { isDemoMode } from '@/utils/demoMode';

// Mirrors App.jsx: the listener lives outside <Routes>, so it survives the very
// navigation it triggers and its snackbar is still on screen afterwards.
const renderListener = () =>
  render(
    <MemoryRouter initialEntries={['/mixlists']}>
      <ApiErrorListener />
      <Routes>
        <Route path="/mixlists" element={<div>mixlists page</div>} />
        <Route path="/login" element={<div>login page</div>} />
        <Route path="/demo-unlock" element={<div>demo unlock page</div>} />
      </Routes>
    </MemoryRouter>,
  );

const dispatch = (type, detail) =>
  act(() => {
    window.dispatchEvent(new CustomEvent(type, { detail }));
  });

describe('ApiErrorListener', () => {
  beforeEach(() => {
    isDemoMode.mockReturnValue(false);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  describe('sessionExpired', () => {
    it('explains the expiry and routes to sign-in', async () => {
      renderListener();

      await dispatch('sessionExpired', { path: '/api/media' });

      await waitFor(() => {
        expect(screen.getByText('login page')).toBeInTheDocument();
      });
      // The old behavior was a bare window.location assignment with no explanation.
      expect(screen.getByText('Your session has expired. Please sign in again.')).toBeInTheDocument();
    });

    it('routes to the unlock page instead of sign-in on the demo site', async () => {
      // Demo visitors have no credentials to re-enter, so /login is a dead end.
      isDemoMode.mockReturnValue(true);
      renderListener();

      await dispatch('sessionExpired', { path: '/api/media' });

      await waitFor(() => {
        expect(screen.getByText('demo unlock page')).toBeInTheDocument();
      });
      expect(screen.queryByText('login page')).not.toBeInTheDocument();
    });
  });

  describe('apiForbidden', () => {
    it('surfaces the message from the API', async () => {
      renderListener();

      await dispatch('apiForbidden', { message: 'Reindexing is disabled here.' });

      expect(await screen.findByText('Reindexing is disabled here.')).toBeInTheDocument();
      // A forbidden action is not a session problem, so the user stays where they are.
      expect(screen.getByText('mixlists page')).toBeInTheDocument();
    });

    it('falls back to a generic message when the event carries none', async () => {
      renderListener();

      await dispatch('apiForbidden', {});

      expect(
        await screen.findByText('You do not have permission to perform this action.'),
      ).toBeInTheDocument();
    });
  });

  it('stops listening once unmounted', async () => {
    const { unmount } = renderListener();
    unmount();

    await dispatch('apiForbidden', { message: 'Should not appear.' });

    expect(screen.queryByText('Should not appear.')).not.toBeInTheDocument();
  });
});
