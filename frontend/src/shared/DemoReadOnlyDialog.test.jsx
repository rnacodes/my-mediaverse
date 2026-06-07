import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen, within, act, waitFor } from '@/test/test-utils';
import DemoReadOnlyDialog from './DemoReadOnlyDialog';

// DemoReadOnlyDialog is the read-only notice shown when a write is blocked on the demo
// site. It's driven entirely by the real DemoReadOnlyProvider (mounted by
// renderWithProviders), which opens the dialog in response to a `demoWriteBlocked`
// window event. Per the C.3 / RAS-20 convention we exercise the real provider rather
// than mocking useDemoReadOnly, so this also covers the event wiring.
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
