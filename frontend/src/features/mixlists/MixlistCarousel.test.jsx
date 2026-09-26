import { describe, it, expect, vi, afterEach } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import { useDemoWriteBlocked } from '@/features/demo/useDemoWriteBlocked';
import MixlistCarousel from './MixlistCarousel';

// Adding an item to a mixlist and creating a mixlist are writes: both buttons are disabled
// on the public demo. The mixlists the item already belongs to stay visible.

vi.mock('@/features/demo/useDemoWriteBlocked', () => {
  const useDemoWriteBlocked = vi.fn(() => false);
  return { useDemoWriteBlocked, default: useDemoWriteBlocked };
});

afterEach(() => {
  vi.mocked(useDemoWriteBlocked).mockReturnValue(false);
});

const render = () =>
  renderWithProviders(
    <MixlistCarousel
      mediaItem={{ id: 'show-1', title: 'Chernobyl' }}
      currentMixlists={[{ id: 'mix-1', name: 'Disaster dramas', description: '' }]}
      availableMixlists={[]}
      setCurrentMixlists={() => {}}
      setAvailableMixlists={() => {}}
      setSnackbar={() => {}}
      isMobile={false}
    />,
  );

describe('MixlistCarousel demo guards', () => {
  it('keeps Add to Mixlist and Create New enabled off the demo', () => {
    render();

    expect(screen.getByRole('button', { name: /add to mixlist/i })).toBeEnabled();
    expect(screen.getByRole('button', { name: /create new/i })).toBeEnabled();
  });

  it('disables Add to Mixlist and Create New while the demo blocks writes', () => {
    vi.mocked(useDemoWriteBlocked).mockReturnValue(true);

    render();

    expect(screen.getByRole('button', { name: /add to mixlist/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /create new/i })).toBeDisabled();
    expect(screen.getByText('Disaster dramas')).toBeInTheDocument();
  });
});
