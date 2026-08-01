import { describe, it, expect, afterEach, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { renderWithProviders, screen, stubHostname } from '@/test/test-utils';
import DemoBanner from './DemoBanner';

const DEMO_HOST = 'demo.mymediaverseuniverse.com';

// The banner derives write mode from real auth state: the default /auth/refresh
// handler seeds an authenticated session, so read-only tests override it with a 401.
const stubLoggedOut = () =>
  server.use(
    http.post(`${API_BASE}/auth/refresh`, () => new HttpResponse(null, { status: 401 })),
  );

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
});

describe('DemoBanner', () => {
  it('renders nothing when not in demo mode', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'false');
    stubHostname(DEMO_HOST);
    const { container } = renderWithProviders(<DemoBanner />);
    expect(container).toBeEmptyDOMElement();
  });

  it('renders nothing on localhost even with demo mode on', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname('localhost');
    const { container } = renderWithProviders(<DemoBanner />);
    expect(container).toBeEmptyDOMElement();
  });

  it('shows the read-only message on the deployed demo when writes are locked', async () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname(DEMO_HOST);
    stubLoggedOut();

    renderWithProviders(<DemoBanner />);

    expect(await screen.findByText(/read-only demo/i)).toBeInTheDocument();
    expect(screen.getByText(/creating, editing, and deleting are disabled/i)).toBeInTheDocument();
  });

  it('swaps to the write-mode message with a countdown once a session exists', async () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname(DEMO_HOST);
    // Default /auth/refresh handler seeds an authenticated session.

    renderWithProviders(<DemoBanner />);

    expect(await screen.findByText(/write access enabled/i)).toBeInTheDocument();
    expect(screen.getByText(/min left/i)).toBeInTheDocument();
    expect(screen.queryByText(/read-only demo/i)).not.toBeInTheDocument();
  });
});
