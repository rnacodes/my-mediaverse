import { describe, it, expect, afterEach, vi } from 'vitest';
import { renderWithProviders, screen, stubHostname } from '@/test/test-utils';
import DemoBanner from './DemoBanner';

const DEMO_HOST = 'demo.mymediaverseuniverse.com';

afterEach(() => {
  vi.unstubAllEnvs();
  vi.unstubAllGlobals();
  sessionStorage.clear();
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

  it('shows the read-only message on the deployed demo when writes are locked', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname(DEMO_HOST);
    renderWithProviders(<DemoBanner />);
    expect(screen.getByText(/read-only demo/i)).toBeInTheDocument();
    expect(screen.getByText(/creating, editing, and deleting are disabled/i)).toBeInTheDocument();
  });

  it('swaps to the write-mode message when admin writes are unlocked', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    stubHostname(DEMO_HOST);
    // DemoAdminProvider reads this on mount to derive isAdminMode.
    sessionStorage.setItem('demoAdminKey', 'test-admin-key');

    renderWithProviders(<DemoBanner />);

    expect(screen.getByText(/write access enabled/i)).toBeInTheDocument();
    expect(screen.queryByText(/read-only demo/i)).not.toBeInTheDocument();
  });
});
