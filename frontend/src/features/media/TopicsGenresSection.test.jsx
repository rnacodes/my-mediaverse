import { describe, it, expect, vi, afterEach } from 'vitest';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import { useDemoWriteBlocked } from '@/features/demo/useDemoWriteBlocked';
import TopicsGenresSection from './TopicsGenresSection';

// Adding or removing a topic or genre is a write: on the public demo the add buttons are
// disabled and the chips lose their remove icon. The chips themselves stay clickable,
// because a click only navigates to a search.

vi.mock('@/features/demo/useDemoWriteBlocked', () => {
  const useDemoWriteBlocked = vi.fn(() => false);
  return { useDemoWriteBlocked, default: useDemoWriteBlocked };
});

afterEach(() => {
  vi.mocked(useDemoWriteBlocked).mockReturnValue(false);
});

const mediaItem = { id: 'show-1', title: 'Chernobyl', topics: ['history'], genres: ['drama'] };

const render = () =>
  renderWithProviders(<TopicsGenresSection mediaItem={mediaItem} setSnackbar={() => {}} onUpdate={() => {}} />);

const chip = (label) => screen.getByText(label).closest('.MuiChip-root');

describe('TopicsGenresSection demo guards', () => {
  it('keeps Add Topic, Add Genre and the chip remove icons available off the demo', () => {
    render();

    expect(screen.getByRole('button', { name: /add topic/i })).toBeEnabled();
    expect(screen.getByRole('button', { name: /add genre/i })).toBeEnabled();
    expect(within(chip('history')).getByTestId('CloseIcon')).toBeInTheDocument();
    expect(within(chip('drama')).getByTestId('CloseIcon')).toBeInTheDocument();
  });

  it('disables Add Topic and Add Genre and drops the remove icons while the demo blocks writes', () => {
    vi.mocked(useDemoWriteBlocked).mockReturnValue(true);

    render();

    expect(screen.getByRole('button', { name: /add topic/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /add genre/i })).toBeDisabled();
    expect(within(chip('history')).queryByTestId('CloseIcon')).not.toBeInTheDocument();
    expect(within(chip('drama')).queryByTestId('CloseIcon')).not.toBeInTheDocument();
    // The chips still lead to a search.
    expect(chip('history')).not.toHaveClass('Mui-disabled');
  });
});
