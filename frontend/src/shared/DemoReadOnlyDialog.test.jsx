import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen, within, act, waitFor } from '@/test/test-utils';
import DemoReadOnlyDialog from './DemoReadOnlyDialog';

// Smoke test only - left as a future decomposition
const fireBlockedWrite = () =>
  act(() => {
    window.dispatchEvent(new CustomEvent('demoWriteBlocked', { detail: null }));
  });

describe('DemoReadOnlyDialog', () => {
  it('is not rendered until a blocked write opens it', () => {
    renderWithProviders(<DemoReadOnlyDialog />);

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('opens with the read-only messaging when a demoWriteBlocked event fires', async () => {
    renderWithProviders(<DemoReadOnlyDialog />);

    fireBlockedWrite();

    const dialog = await screen.findByRole('dialog');
    expect(within(dialog).getByText('Demo Mode - Read Only')).toBeInTheDocument();
    expect(within(dialog).getByText(/the demo website is read-only/i)).toBeInTheDocument();
    expect(
      within(dialog).getByText(/creating,\s*editing, or deleting data is disabled/i),
    ).toBeInTheDocument();
    expect(within(dialog).getByRole('button', { name: /got it/i })).toBeInTheDocument();
  });

  it('focuses the "Got it" action when opened', async () => {
    renderWithProviders(<DemoReadOnlyDialog />);

    fireBlockedWrite();

    const gotIt = await screen.findByRole('button', { name: /got it/i });
    await waitFor(() => expect(gotIt).toHaveFocus());
  });

  it('closes when "Got it" is clicked', async () => {
    const { user } = renderWithProviders(<DemoReadOnlyDialog />);

    fireBlockedWrite();

    const dialog = await screen.findByRole('dialog');
    await user.click(within(dialog).getByRole('button', { name: /got it/i }));

    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });
});
