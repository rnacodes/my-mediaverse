import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';
import DemoUnlockPage from '../DemoUnlockPage';

const renderWithRouter = (ui) => render(<BrowserRouter>{ui}</BrowserRouter>);

const DEMO_API_BASE = 'https://demo-api.mymediaverseuniverse.com/api/demo';

describe('DemoUnlockPage', () => {
    let windowOpenSpy;

    beforeEach(() => {
        vi.clearAllMocks();
        windowOpenSpy = vi.spyOn(window, 'open').mockImplementation(() => null);
        global.fetch = vi.fn();
    });

    afterEach(() => {
        windowOpenSpy.mockRestore();
        vi.restoreAllMocks();
    });

    const mockFetchStatus = (data) => {
        global.fetch.mockResolvedValue({
            ok: true,
            json: () => Promise.resolve(data),
        });
    };

    const mockFetchError = () => {
        global.fetch.mockRejectedValue(new Error('Network Error'));
    };

    const mockFetchNotOk = () => {
        global.fetch.mockResolvedValue({
            ok: false,
            status: 500,
        });
    };

    describe('Loading State', () => {
        it('should show loading indicator while fetching status', async () => {
            let resolveFetch;
            global.fetch.mockImplementation(
                () => new Promise((resolve) => { resolveFetch = resolve; })
            );

            renderWithRouter(<DemoUnlockPage />);

            const progressbars = screen.getAllByRole('progressbar');
            expect(progressbars.length).toBeGreaterThanOrEqual(1);

            await act(async () => {
                resolveFetch({
                    ok: true,
                    json: () => Promise.resolve({ isDemoEnvironment: false, writeAccessEnabled: false, message: '' }),
                });
            });
        });
    });

    describe('Status Display', () => {
        it('should show "Not Active" chip when isDemoEnvironment is false', async () => {
            mockFetchStatus({
                isDemoEnvironment: false,
                writeAccessEnabled: false,
                message: 'Not in demo environment',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText('Not Active')).toBeInTheDocument();
            });
        });

        it('should show "Active" chip when isDemoEnvironment is true', async () => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: false,
                message: 'Write access is disabled',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText('Active')).toBeInTheDocument();
            });
        });

        it('should show write access disabled state', async () => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: false,
                message: 'Write access is disabled',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText('Disabled (Read-Only)')).toBeInTheDocument();
            });
        });

        it('should show write access enabled state with expiration info', async () => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: true,
                message: 'Write access is enabled via TOTP',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText('Enabled')).toBeInTheDocument();
            });

            expect(screen.getByText(/Access expires ~20 minutes from unlock/)).toBeInTheDocument();
        });
    });

    describe('TOTP Code Input', () => {
        beforeEach(() => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: false,
                message: 'Write access is disabled',
            });
        });

        it('should accept 6-digit numeric input', async () => {
            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByLabelText('TOTP Code')).toBeInTheDocument();
            });

            const input = screen.getByLabelText('TOTP Code');
            fireEvent.change(input, { target: { value: '123456' } });

            expect(input.value).toBe('123456');
        });

        it('should strip non-numeric characters', async () => {
            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByLabelText('TOTP Code')).toBeInTheDocument();
            });

            const input = screen.getByLabelText('TOTP Code');
            fireEvent.change(input, { target: { value: 'abc123def456' } });

            expect(input.value).toBe('123456');
        });

        it('should trigger unlock on Enter key when code is 6 digits', async () => {
            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByLabelText('TOTP Code')).toBeInTheDocument();
            });

            const input = screen.getByLabelText('TOTP Code');
            fireEvent.change(input, { target: { value: '123456' } });
            fireEvent.keyDown(input, { key: 'Enter', code: 'Enter' });

            expect(windowOpenSpy).toHaveBeenCalledWith(
                `${DEMO_API_BASE}/unlock?code=123456`,
                '_blank'
            );
        });

        it('should NOT trigger unlock on Enter key when code is less than 6 digits', async () => {
            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByLabelText('TOTP Code')).toBeInTheDocument();
            });

            const input = screen.getByLabelText('TOTP Code');
            fireEvent.change(input, { target: { value: '123' } });
            fireEvent.keyDown(input, { key: 'Enter', code: 'Enter' });

            expect(windowOpenSpy).not.toHaveBeenCalled();
        });
    });

    describe('Unlock Flow', () => {
        beforeEach(() => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: false,
                message: 'Write access is disabled',
            });
        });

        it('should open unlock URL with TOTP code when Unlock button is clicked', async () => {
            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByLabelText('TOTP Code')).toBeInTheDocument();
            });

            const input = screen.getByLabelText('TOTP Code');
            fireEvent.change(input, { target: { value: '654321' } });

            const unlockButton = screen.getByRole('button', { name: /unlock/i });
            fireEvent.click(unlockButton);

            expect(windowOpenSpy).toHaveBeenCalledWith(
                `${DEMO_API_BASE}/unlock?code=654321`,
                '_blank'
            );
        });

        it('should show info message after unlock click', async () => {
            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByLabelText('TOTP Code')).toBeInTheDocument();
            });

            const input = screen.getByLabelText('TOTP Code');
            fireEvent.change(input, { target: { value: '123456' } });

            const unlockButton = screen.getByRole('button', { name: /unlock/i });
            fireEvent.click(unlockButton);

            expect(screen.getByText(/Unlock request opened in a new tab/)).toBeInTheDocument();
        });

        it('should clear TOTP code after unlock', async () => {
            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByLabelText('TOTP Code')).toBeInTheDocument();
            });

            const input = screen.getByLabelText('TOTP Code');
            fireEvent.change(input, { target: { value: '123456' } });
            expect(input.value).toBe('123456');

            const unlockButton = screen.getByRole('button', { name: /unlock/i });
            fireEvent.click(unlockButton);

            expect(input.value).toBe('');
        });

        it('should disable unlock button when code is less than 6 digits', async () => {
            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByLabelText('TOTP Code')).toBeInTheDocument();
            });

            const unlockButton = screen.getByRole('button', { name: /unlock/i });
            expect(unlockButton).toBeDisabled();

            const input = screen.getByLabelText('TOTP Code');
            fireEvent.change(input, { target: { value: '123' } });
            expect(unlockButton).toBeDisabled();
        });
    });

    describe('Lock Flow', () => {
        it('should open lock URL when Revoke Write Access button is clicked', async () => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: true,
                message: 'Write access is enabled',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByRole('button', { name: /revoke write access/i })).toBeInTheDocument();
            });

            const lockButton = screen.getByRole('button', { name: /revoke write access/i });
            fireEvent.click(lockButton);

            expect(windowOpenSpy).toHaveBeenCalledWith(
                `${DEMO_API_BASE}/lock`,
                '_blank'
            );
        });
    });

    describe('Error Handling', () => {
        it('should display error when fetch fails with network error', async () => {
            mockFetchError();

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText(/Could not connect to the demo API/)).toBeInTheDocument();
            });
        });

        it('should display error when fetch returns non-OK status', async () => {
            mockFetchNotOk();

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText(/Failed to fetch status from demo API/)).toBeInTheDocument();
            });
        });
    });

    describe('Setup Instructions', () => {
        it('should show setup instructions accordion', async () => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: false,
                message: 'Write access is disabled',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText('First-Time Setup Instructions')).toBeInTheDocument();
            });
        });

        it('should expand accordion to show setup steps', async () => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: false,
                message: 'Write access is disabled',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText('First-Time Setup Instructions')).toBeInTheDocument();
            });

            fireEvent.click(screen.getByText('First-Time Setup Instructions'));

            await waitFor(() => {
                expect(screen.getByText(/Install an authenticator app/)).toBeInTheDocument();
                expect(screen.getByText(/Add the demo account/)).toBeInTheDocument();
                expect(screen.getByText(/Enter the code/)).toBeInTheDocument();
            });
        });
    });

    describe('Page Title', () => {
        it('should display the page title', async () => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: false,
                message: 'Write access is disabled',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText('Demo Mode Administration')).toBeInTheDocument();
            });
        });
    });

    describe('Refresh Button', () => {
        it('should re-fetch status when Refresh button is clicked', async () => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: false,
                message: 'Write access is disabled',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText('Demo Write Access Status')).toBeInTheDocument();
            });

            expect(global.fetch).toHaveBeenCalledTimes(1);

            const refreshButton = screen.getByRole('button', { name: /refresh/i });
            fireEvent.click(refreshButton);

            await waitFor(() => {
                expect(global.fetch).toHaveBeenCalledTimes(2);
            });
        });
    });

    describe('Check Directly Button', () => {
        it('should open status endpoint in new tab', async () => {
            mockFetchStatus({
                isDemoEnvironment: true,
                writeAccessEnabled: false,
                message: 'Write access is disabled',
            });

            renderWithRouter(<DemoUnlockPage />);

            await waitFor(() => {
                expect(screen.getByText('Check Directly')).toBeInTheDocument();
            });

            fireEvent.click(screen.getByText('Check Directly'));

            expect(windowOpenSpy).toHaveBeenCalledWith(
                `${DEMO_API_BASE}/status`,
                '_blank'
            );
        });
    });
});
