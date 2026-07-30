import { describe, it, expect, afterEach, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom';
import { http, HttpResponse, delay } from 'msw';

import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { AuthProvider } from '@/contexts/AuthProvider';
import ConditionalProtectedRoute from './ConditionalProtectedRoute';

const SecretPage = () => <div>secret page</div>;

// Echoes the redirect state so tests can assert the original destination
// survives the trip to /login (LoginPage uses it to navigate back).
const LoginProbe = () => {
  const location = useLocation();
  return <div>login page, from:{location.state?.from?.pathname}</div>;
};

const renderProtected = () =>
  render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/secret']}>
        <Routes>
          <Route
            path="/secret"
            element={
              <ConditionalProtectedRoute>
                <SecretPage />
              </ConditionalProtectedRoute>
            }
          />
          <Route path="/login" element={<LoginProbe />} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );

afterEach(() => {
  vi.unstubAllEnvs();
});

describe('ConditionalProtectedRoute', () => {
  // Each test pins VITE_DEMO_MODE rather than inheriting it from .env, where
  // local dev keeps it 'true' — that would silently bypass the auth being tested.
  it('shows the loading spinner while the session check is in flight', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'false');
    server.use(http.post(`${API_BASE}/auth/refresh`, () => delay('infinite')));

    renderProtected();

    expect(screen.getByRole('progressbar')).toBeInTheDocument();
    expect(screen.queryByText('secret page')).not.toBeInTheDocument();
  });

  it('renders the protected page once the session check succeeds', async () => {
    vi.stubEnv('VITE_DEMO_MODE', 'false');

    renderProtected();

    expect(await screen.findByText('secret page')).toBeInTheDocument();
  });

  it('redirects to login when there is no session, preserving the destination', async () => {
    vi.stubEnv('VITE_DEMO_MODE', 'false');
    server.use(
      http.post(`${API_BASE}/auth/refresh`, () =>
        HttpResponse.json({ message: 'No session' }, { status: 401 }),
      ),
    );

    renderProtected();

    expect(await screen.findByText('login page, from:/secret')).toBeInTheDocument();
    expect(screen.queryByText('secret page')).not.toBeInTheDocument();
  });

  it('renders the page without any auth in demo mode', async () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    server.use(
      http.post(`${API_BASE}/auth/refresh`, () =>
        HttpResponse.json({ message: 'No session' }, { status: 401 }),
      ),
    );

    renderProtected();

    expect(await screen.findByText('secret page')).toBeInTheDocument();
    expect(screen.queryByText(/login page/)).not.toBeInTheDocument();
  });
});
