import { describe, it, expect, vi, afterEach } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import { useDemoWriteBlocked } from '@/features/demo/useDemoWriteBlocked';
import MediaHeader from './MediaHeader';

// The header's Reindex and Edit Media buttons are writes: both are disabled on the
// public demo. `actions` lets a profile page add its own controls beside them.

vi.mock('@/features/demo/useDemoWriteBlocked', () => {
  const useDemoWriteBlocked = vi.fn(() => false);
  return { useDemoWriteBlocked, default: useDemoWriteBlocked };
});

afterEach(() => {
  vi.mocked(useDemoWriteBlocked).mockReturnValue(false);
});

describe('MediaHeader', () => {
  it('renders the title with enabled Reindex and Edit Media buttons off the demo', () => {
    renderWithProviders(<MediaHeader title="Chernobyl" mediaId="show-1" onReindex={() => {}} />);

    expect(screen.getByRole('heading', { name: 'Chernobyl' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /reindex/i })).toBeEnabled();
    expect(screen.getByRole('button', { name: /edit media/i })).toBeEnabled();
  });

  it('disables Reindex and Edit Media while the demo blocks writes', () => {
    vi.mocked(useDemoWriteBlocked).mockReturnValue(true);

    renderWithProviders(<MediaHeader title="Chernobyl" mediaId="show-1" onReindex={() => {}} />);

    expect(screen.getByRole('button', { name: /reindex/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /edit media/i })).toBeDisabled();
  });

  it('renders page-specific actions beside the standard buttons', () => {
    renderWithProviders(
      <MediaHeader title="Chernobyl" mediaId="show-1" actions={<button type="button">Import episodes</button>} />,
    );

    expect(screen.getByRole('button', { name: /import episodes/i })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /reindex/i })).not.toBeInTheDocument();
  });
});
