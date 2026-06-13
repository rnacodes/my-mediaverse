import { describe, it, expect, vi, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { useLocation } from 'react-router-dom';
import { renderWithProviders, screen, within, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import ResponsiveNavigation from './ResponsiveNavigation';

const setMatchMedia = (matches) => {
  window.matchMedia = vi.fn().mockImplementation((query) => ({
    matches,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  }));
};

const loggedOut = () =>
  server.use(
    http.post(`${API_BASE}/auth/refresh`, () => new HttpResponse(null, { status: 401 })),
  );

// Renders alongside the nav so route changes are observable.
function LocationDisplay() {
  const location = useLocation();
  return <div data-testid="location">{`${location.pathname}${location.search}`}</div>;
}

const renderNav = (opts) =>
  renderWithProviders(
    <>
      <ResponsiveNavigation />
      <LocationDisplay />
    </>,
    opts,
  );

afterEach(() => setMatchMedia(false));

describe('ResponsiveNavigation', () => {
  describe('desktop', () => {
    it('renders the top nav bar with links and menus, and no hamburger', async () => {
      setMatchMedia(false);
      loggedOut();
      renderNav();

      const banner = screen.getByRole('banner');
      expect(within(banner).getByRole('link', { name: 'Home' })).toBeInTheDocument();
      expect(within(banner).getByRole('link', { name: 'Search' })).toBeInTheDocument();
      expect(within(banner).getByRole('button', { name: 'Browse Media' })).toBeInTheDocument();
      expect(within(banner).getByRole('button', { name: 'Add Media' })).toBeInTheDocument();
      expect(within(banner).getByRole('button', { name: 'Admin' })).toBeInTheDocument();
      expect(
        within(banner).queryByRole('button', { name: 'open drawer' }),
      ).not.toBeInTheDocument();
    });

    it('routes when a nav link is clicked', async () => {
      setMatchMedia(false);
      loggedOut();
      const { user } = renderNav();

      const banner = screen.getByRole('banner');
      await user.click(within(banner).getByRole('link', { name: 'Search' }));

      expect(screen.getByTestId('location')).toHaveTextContent('/search');
    });

    it('opens a dropdown menu and routes from a menu item', async () => {
      setMatchMedia(false);
      loggedOut();
      const { user } = renderNav();

      const banner = screen.getByRole('banner');
      await user.click(within(banner).getByRole('button', { name: 'Browse Media' }));

      // The Menu is portaled to the body; its items carry role 'menuitem'.
      await user.click(await screen.findByRole('menuitem', { name: 'Books' }));

      expect(screen.getByTestId('location')).toHaveTextContent('/search?mediaType=Book');
    });

    it('shows Login when logged out and Logout when authenticated', async () => {
      setMatchMedia(false);

      // Authenticated (default /auth/refresh handler).
      const { unmount } = renderNav();
      const banner = screen.getByRole('banner');
      expect(await within(banner).findByRole('button', { name: /logout/i })).toBeInTheDocument();
      expect(within(banner).queryByRole('button', { name: /^login$/i })).not.toBeInTheDocument();
      unmount();

      // Logged out (401 refresh).
      loggedOut();
      renderNav();
      const banner2 = screen.getByRole('banner');
      expect(within(banner2).getByRole('button', { name: /login/i })).toBeInTheDocument();
      expect(within(banner2).queryByRole('button', { name: /logout/i })).not.toBeInTheDocument();
    });
  });

  describe('mobile', () => {
    it('shows the hamburger, hides the desktop links, and keeps the drawer closed', () => {
      setMatchMedia(true);
      loggedOut();
      renderNav();

      const banner = screen.getByRole('banner');
      expect(within(banner).getByRole('button', { name: 'open drawer' })).toBeInTheDocument();
      // Desktop nav links are not rendered in the mobile bar.
      expect(within(banner).queryByRole('link', { name: 'Home' })).not.toBeInTheDocument();
      // The closed drawer's items are not accessible yet.
      expect(screen.queryByRole('button', { name: 'Search' })).not.toBeInTheDocument();
    });

    it('opens the drawer from the hamburger and routes from a drawer item', async () => {
      setMatchMedia(true);
      loggedOut();
      const { user } = renderNav();

      const banner = screen.getByRole('banner');
      await user.click(within(banner).getByRole('button', { name: 'open drawer' }));

      // Drawer is now open: its nav items become accessible.
      const searchItem = await screen.findByRole('button', { name: 'Search' });
      await user.click(searchItem);

      await waitFor(() =>
        expect(screen.getByTestId('location')).toHaveTextContent('/search'),
      );
    });
  });
});
