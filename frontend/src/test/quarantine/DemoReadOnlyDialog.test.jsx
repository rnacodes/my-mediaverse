// QUARANTINED (RAS-17): excluded from the run via vitest.config.js.
// TODO(RAS-29): rewrite against the new test infra (MSW + renderWithProviders).
// Component moved to src/shared/DemoReadOnlyDialog.jsx in the feature-folder reorg.
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';

const mockHideReadOnlyDialog = vi.fn();
let mockIsDialogOpen = false;

vi.mock('../../contexts/DemoReadOnlyContext', () => ({
    useDemoReadOnly: () => ({
        isDialogOpen: mockIsDialogOpen,
        hideReadOnlyDialog: mockHideReadOnlyDialog,
    }),
}));

import DemoReadOnlyDialog from '../shared/DemoReadOnlyDialog';

describe('DemoReadOnlyDialog', () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mockIsDialogOpen = false;
    });

    describe('Rendering', () => {
        it('should show dialog when isDialogOpen is true', () => {
            mockIsDialogOpen = true;

            render(<DemoReadOnlyDialog />);

            expect(screen.getByRole('dialog')).toBeInTheDocument();
        });

        it('should not show dialog when isDialogOpen is false', () => {
            mockIsDialogOpen = false;

            render(<DemoReadOnlyDialog />);

            expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
        });
    });

    describe('Content Display', () => {
        it('should display read-only title and message', () => {
            mockIsDialogOpen = true;

            render(<DemoReadOnlyDialog />);

            expect(screen.getByText('Demo Mode - Read Only')).toBeInTheDocument();
            expect(screen.getByText(/The demo website is read-only/)).toBeInTheDocument();
            expect(screen.getByText(/creating, editing, or deleting data is disabled/)).toBeInTheDocument();
        });

        it('should display the Got it button', () => {
            mockIsDialogOpen = true;

            render(<DemoReadOnlyDialog />);

            expect(screen.getByRole('button', { name: 'Got it' })).toBeInTheDocument();
        });
    });

    describe('User Interaction', () => {
        it('should call hideReadOnlyDialog when Got it button is clicked', () => {
            mockIsDialogOpen = true;

            render(<DemoReadOnlyDialog />);

            const gotItButton = screen.getByRole('button', { name: 'Got it' });
            fireEvent.click(gotItButton);

            expect(mockHideReadOnlyDialog).toHaveBeenCalledTimes(1);
        });

        it('should have autoFocus on Got it button', () => {
            mockIsDialogOpen = true;

            render(<DemoReadOnlyDialog />);

            const gotItButton = screen.getByRole('button', { name: 'Got it' });
            expect(gotItButton).toHaveFocus();
        });
    });
});
