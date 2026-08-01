import { describe, it, expect, afterEach, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { renderWithProviders, screen, stubHostname } from '@/test/test-utils';
import DemoRestrictedRoute from './DemoRestrictedRoute';

const DEMO_HOST = 'demo.mymediaverseuniverse.com';
const AdminPage = () => <div>Typesense Admin Controls</div>;

const renderRestricted = () =>
  renderWithProviders(
    <DemoRestrictedRoute>
      <AdminPage />
    </DemoRestrictedRoute>,
  );

// The guard keys off real auth state: the default /auth/refresh handler seeds an
// authenticated session, so the blocked path overrides it with a 401.
const stubLoggedOut = () =>
  server.use(
    http.post(`${API_BASE}/auth/refresh`, () => new HttpResponse(null, { status: 401 })),
  );

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

describe('DemoRestrictedRoute', () => {
  it('blocks the page on the public demo without a session', async () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname(DEMO_HOST);
    stubLoggedOut();

    renderRestricted();

    expect(await screen.findByText(/not available in demo/i)).toBeInTheDocument();
    expect(screen.queryByText(/typesense admin controls/i)).not.toBeInTheDocument();
  });

  it('renders the page on localhost despite demo mode', async () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname('localhost');
    stubLoggedOut();

    renderRestricted();

    expect(await screen.findByText(/typesense admin controls/i)).toBeInTheDocument();
    expect(screen.queryByText(/not available in demo/i)).not.toBeInTheDocument();
  });

  it('renders the page in production', async () => {
    vi.stubEnv('VITE_DEMO_MODE', 'false');
    stubHostname('www.mymediaverseuniverse.com');

    renderRestricted();

    expect(await screen.findByText(/typesense admin controls/i)).toBeInTheDocument();
  });

  it('restores access on the public demo once a session exists', async () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname(DEMO_HOST);
    // Default /auth/refresh handler seeds an authenticated session (as a TOTP unlock would).

    renderRestricted();

    expect(await screen.findByText(/typesense admin controls/i)).toBeInTheDocument();
    expect(screen.queryByText(/not available in demo/i)).not.toBeInTheDocument();
  });
});
