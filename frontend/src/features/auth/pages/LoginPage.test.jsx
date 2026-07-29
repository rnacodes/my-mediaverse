import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { AuthProvider } from '@/contexts/AuthProvider';
import LoginPage from './LoginPage';

const renderLoginPage = (state) =>
  render(
    <AuthProvider>
      <MemoryRouter initialEntries={[{ pathname: '/login', state }]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<div>home page</div>} />
          <Route path="/mixlists" element={<div>mixlists page</div>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );

const submitLogin = async (user) => {
  await user.type(screen.getByLabelText('Username'), 'testuser');
  await user.type(screen.getByLabelText('Password'), 'testpass');
  await user.click(screen.getByRole('button', { name: 'Sign In' }));
};

describe('LoginPage', () => {
  it('returns to the page that redirected here after a successful login', async () => {
    const user = userEvent.setup();
    renderLoginPage({ from: { pathname: '/mixlists', search: '' } });

    await submitLogin(user);

    await waitFor(() => {
      expect(screen.getByText('mixlists page')).toBeInTheDocument();
    });
  });

  it('preserves the query string of the original destination', async () => {
    const user = userEvent.setup();
    renderLoginPage({ from: { pathname: '/mixlists', search: '?sort=title' } });

    await submitLogin(user);

    await waitFor(() => {
      expect(screen.getByText('mixlists page')).toBeInTheDocument();
    });
  });

  it('falls back to home when there is no saved destination', async () => {
    const user = userEvent.setup();
    renderLoginPage(undefined);

    await submitLogin(user);

    await waitFor(() => {
      expect(screen.getByText('home page')).toBeInTheDocument();
    });
  });

  it('does not redirect back to /login', async () => {
    const user = userEvent.setup();
    renderLoginPage({ from: { pathname: '/login', search: '' } });

    await submitLogin(user);

    await waitFor(() => {
      expect(screen.getByText('home page')).toBeInTheDocument();
    });
  });

  it('shows the error message and stays put when login fails', async () => {
    const user = userEvent.setup();
    const consoleSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
    server.use(
      http.post(`${API_BASE}/auth/login`, () =>
        HttpResponse.json({ message: 'Invalid username or password' }, { status: 401 }),
      ),
    );
    renderLoginPage({ from: { pathname: '/mixlists', search: '' } });

    await submitLogin(user);

    await waitFor(() => {
      expect(screen.getByText('Invalid username or password')).toBeInTheDocument();
    });
    expect(screen.queryByText('mixlists page')).not.toBeInTheDocument();

    consoleSpy.mockRestore();
  });
});
