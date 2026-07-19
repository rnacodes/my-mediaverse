import { describe, it, expect, afterEach, vi } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import DemoBanner from './DemoBanner';

afterEach(() => {
  vi.unstubAllEnvs();
  sessionStorage.clear();
});

describe('DemoBanner', () => {
  it('renders nothing when not in demo mode', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'false');
    const { container } = renderWithProviders(<DemoBanner />);
    expect(container).toBeEmptyDOMElement();
  });

  it('shows the read-only message in demo mode when writes are locked', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    renderWithProviders(<DemoBanner />);
    expect(screen.getByText(/read-only demo/i)).toBeInTheDocument();
    expect(screen.getByText(/creating, editing, and deleting are disabled/i)).toBeInTheDocument();
  });

  it('swaps to the write-mode message when admin writes are unlocked', () => {
    vi.stubEnv('VITE_DEMO_MODE', 'true');
    // DemoAdminProvider reads this on mount to derive isAdminMode.
    sessionStorage.setItem('demoAdminKey', 'test-admin-key');

    renderWithProviders(<DemoBanner />);

    expect(screen.getByText(/write access enabled/i)).toBeInTheDocument();
    expect(screen.queryByText(/read-only demo/i)).not.toBeInTheDocument();
  });
});
