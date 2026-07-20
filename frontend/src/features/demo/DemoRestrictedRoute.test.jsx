import { describe, it, expect, afterEach, vi } from 'vitest';
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

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
  sessionStorage.clear();
});

describe('DemoRestrictedRoute', () => {
  it('blocks the page on the public demo', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname(DEMO_HOST);

    renderRestricted();

    expect(screen.getByText(/not available in demo/i)).toBeInTheDocument();
    expect(screen.queryByText(/typesense admin controls/i)).not.toBeInTheDocument();
  });

  it('renders the page on localhost despite demo mode', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname('localhost');

    renderRestricted();

    expect(screen.getByText(/typesense admin controls/i)).toBeInTheDocument();
    expect(screen.queryByText(/not available in demo/i)).not.toBeInTheDocument();
  });

  it('renders the page in production', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'false');
    stubHostname('www.mymediaverseuniverse.com');

    renderRestricted();

    expect(screen.getByText(/typesense admin controls/i)).toBeInTheDocument();
  });

  it('restores access on the public demo once admin mode is unlocked', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname(DEMO_HOST);
    sessionStorage.setItem('demoAdminKey', 'test-admin-key');

    renderRestricted();

    expect(screen.getByText(/typesense admin controls/i)).toBeInTheDocument();
    expect(screen.queryByText(/not available in demo/i)).not.toBeInTheDocument();
  });
});
