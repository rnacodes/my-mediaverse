import { describe, it, expect, afterEach, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import DemoUnlockPage from './DemoUnlockPage';

// The page derives its state from the auth session: the default /auth/refresh
// handler seeds an authenticated session, so locked-state tests override it.
const stubLoggedOut = () =>
  server.use(
    http.post(`${API_BASE}/auth/refresh`, () => new HttpResponse(null, { status: 401 })),
  );

const unlockSession = () => ({
  message: 'Write access unlocked successfully!',
  token: 'demo-access-token',
  username: 'demo',
  expiresInMinutes: 20,
  expiresAt: new Date(Date.now() + 20 * 60 * 1000).toISOString(),
});

const enterCodeAndUnlock = async (user, code) => {
  await user.type(screen.getByLabelText(/totp code/i), code);
  await user.click(screen.getByRole('button', { name: /unlock/i }));
};

afterEach(() => {
  vi.unstubAllEnvs();
});

describe('DemoUnlockPage', () => {
  it('shows the unlock form when there is no session', async () => {
    stubLoggedOut();

    renderWithProviders(<DemoUnlockPage />);

    expect(await screen.findByRole('heading', { name: /unlock write access/i })).toBeInTheDocument();
    expect(screen.getByLabelText(/totp code/i)).toBeInTheDocument();
  });

  it('keeps the unlock button disabled until six digits are entered', async () => {
    stubLoggedOut();

    const { user } = renderWithProviders(<DemoUnlockPage />);

    const button = await screen.findByRole('button', { name: /unlock/i });
    expect(button).toBeDisabled();

    await user.type(screen.getByLabelText(/totp code/i), '123');
    expect(button).toBeDisabled();

    await user.type(screen.getByLabelText(/totp code/i), '456');
    expect(button).toBeEnabled();
  });

  it('applies the session and switches to the unlocked state on a valid code', async () => {
    stubLoggedOut();
    server.use(
      http.get(`${API_BASE}/demo/unlock`, ({ request }) => {
        const code = new URL(request.url).searchParams.get('code');
        return code === '123456'
          ? HttpResponse.json(unlockSession())
          : HttpResponse.json({ error: 'Invalid TOTP code' }, { status: 401 });
      }),
    );

    const { user } = renderWithProviders(<DemoUnlockPage />);
    await screen.findByLabelText(/totp code/i);

    await enterCodeAndUnlock(user, '123456');

    expect(await screen.findByText(/write access is enabled/i)).toBeInTheDocument();
    expect(screen.getByText(/unlocked for 20 minutes/i)).toBeInTheDocument();
    expect(screen.getByText(/minutes left in this window/i)).toBeInTheDocument();
  });

  it('shows an invalid-code message on a 401 without switching state', async () => {
    stubLoggedOut();
    server.use(
      http.get(`${API_BASE}/demo/unlock`, () =>
        HttpResponse.json({ error: 'Invalid TOTP code' }, { status: 401 }),
      ),
    );

    const { user } = renderWithProviders(<DemoUnlockPage />);
    await screen.findByLabelText(/totp code/i);

    await enterCodeAndUnlock(user, '654321');

    expect(await screen.findByText(/invalid code/i)).toBeInTheDocument();
    expect(screen.queryByText(/write access is enabled/i)).not.toBeInTheDocument();
  });

  it('shows a slow-down message when rate limited', async () => {
    stubLoggedOut();
    server.use(
      http.get(`${API_BASE}/demo/unlock`, () =>
        HttpResponse.json(null, { status: 429 }),
      ),
    );

    const { user } = renderWithProviders(<DemoUnlockPage />);
    await screen.findByLabelText(/totp code/i);

    await enterCodeAndUnlock(user, '111222');

    expect(await screen.findByText(/too many attempts/i)).toBeInTheDocument();
  });

  it('shows the unlocked state when a session already exists', async () => {
    // Default /auth/refresh handler seeds an authenticated session.
    renderWithProviders(<DemoUnlockPage />);

    expect(await screen.findByText(/write access is enabled/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /revoke write access/i })).toBeInTheDocument();
  });

  it('revokes the session and returns to the unlock form', async () => {
    let lockCalled = false;
    server.use(
      http.post(`${API_BASE}/demo/lock`, () => {
        lockCalled = true;
        return HttpResponse.json({ message: 'Write access revoked' });
      }),
    );

    const { user } = renderWithProviders(<DemoUnlockPage />);
    await screen.findByText(/write access is enabled/i);

    await user.click(screen.getByRole('button', { name: /revoke write access/i }));

    expect(await screen.findByText(/read-only again/i)).toBeInTheDocument();
    await waitFor(() => expect(lockCalled).toBe(true));
    expect(screen.getByLabelText(/totp code/i)).toBeInTheDocument();
  });
});
