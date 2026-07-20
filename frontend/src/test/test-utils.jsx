import { vi } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from '@mui/material/styles';
import { CssBaseline } from '@mui/material';
import { MemoryRouter, Routes, Route } from 'react-router-dom';
import { render } from '@testing-library/react';
import userEvent from '@testing-library/user-event';

import { theme } from '@/shared/DesignSystem';
import { AuthProvider } from '@/contexts/AuthProvider';
import { DemoAdminProvider } from '@/contexts/DemoAdminProvider';
import { DemoReadOnlyProvider } from '@/contexts/DemoReadOnlyProvider';

/**
 * Fresh QueryClient per render: no retries (so error states surface immediately)
 * and gcTime 0 (so nothing leaks between tests).
 */
export const makeTestQueryClient = () =>
  new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0, staleTime: 0 },
      mutations: { retry: false },
    },
  });

/**
 * Render `ui` inside the full app provider stack, mirroring App.jsx order:
 *   QueryClientProvider → ThemeProvider → AuthProvider → DemoAdminProvider
 *   → DemoReadOnlyProvider → MemoryRouter
 *
 * Options:
 *   - route:        initial MemoryRouter entry (default '/')
 *   - path:         when set, `ui` is mounted under <Route path={path}> so
 *                   components using useParams() resolve against `route`.
 *                   When omitted, `ui` renders directly inside the router.
 *   - queryClient:  supply your own; defaults to makeTestQueryClient()
 *   - user:         a pre-configured userEvent instance (e.g. for fake timers);
 *                   defaults to userEvent.setup()
 *   - demoReadOnly: reserved flag for demo read-only tests (D.8). The provider
 *                   is always mounted; behavior is driven by MSW 403 responses /
 *                   the demoWriteBlocked event, so this is a forward-looking hook.
 *
 * Auth state is seeded at the HTTP boundary (default /auth/refresh handler
 * returns a valid session); override with server.use(...) for the logged-out path.
 *
 * Returns RTL's render result plus { user, queryClient }.
 */
export function renderWithProviders(
  ui,
  { route = '/', path, queryClient, user, demoReadOnly = false, ...renderOptions } = {},
) {
  const client = queryClient ?? makeTestQueryClient();
  const userInstance = user ?? userEvent.setup();

  function Wrapper({ children }) {
    return (
      <QueryClientProvider client={client}>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <AuthProvider>
            <DemoAdminProvider>
              <DemoReadOnlyProvider>
                <MemoryRouter initialEntries={[route]}>
                  {path ? (
                    <Routes>
                      <Route path={path} element={children} />
                    </Routes>
                  ) : (
                    children
                  )}
                </MemoryRouter>
              </DemoReadOnlyProvider>
            </DemoAdminProvider>
          </AuthProvider>
        </ThemeProvider>
      </QueryClientProvider>
    );
  }

  return {
    user: userInstance,
    queryClient: client,
    ...render(ui, { wrapper: Wrapper, ...renderOptions }),
  };
}

export function stubHostname(hostname) {
  vi.stubGlobal('location', {
    ...window.location,
    hostname,
    href: `https://${hostname}/`,
    origin: `https://${hostname}`,
  });
}

// Re-export RTL so tests import everything from one place, plus userEvent.
export * from '@testing-library/react';
export { userEvent };
