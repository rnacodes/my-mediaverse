import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { Routes, Route } from 'react-router-dom';
import { renderWithProviders, screen, waitFor } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import { makeMixlist } from '@/test/factories/mixlist';
import CreateMixlistForm from './CreateMixlistForm';

// CreateMixlistForm submits via useCreateMixlist -> POST /mixlist, then navigates:
// to location.state.returnTo when present, otherwise to /mixlist/:newId. Topic/genre
// autocompletes only fetch on non-empty input, so nothing hits the network on mount
// (the /auth/refresh default covers AuthProvider). We mount the form inside a small
// Routes tree so we can observe where a successful submit navigates to.

const NAME_PLACEHOLDER = /enter mixlist name/i;

// Renders the form plus landing markers for the two post-submit destinations.
const renderForm = (route = '/create-mixlist') =>
  renderWithProviders(
    <Routes>
      <Route path="/create-mixlist" element={<CreateMixlistForm />} />
      <Route path="/mixlists" element={<div>Mixlists landing</div>} />
      <Route path="/mixlist/:id" element={<div>Mixlist detail</div>} />
    </Routes>,
    { route },
  );

describe('CreateMixlistForm', () => {
  it('renders the form fields', () => {
    renderForm();

    expect(screen.getByRole('heading', { name: /create new mixlist/i })).toBeInTheDocument();
    expect(screen.getByPlaceholderText(NAME_PLACEHOLDER)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /create mixlist/i })).toBeInTheDocument();
  });

  it('disables submit until a name is entered', async () => {
    const { user } = renderForm();

    const submit = screen.getByRole('button', { name: /create mixlist/i });
    expect(submit).toBeDisabled();

    await user.type(screen.getByPlaceholderText(NAME_PLACEHOLDER), 'Road Trip');
    expect(submit).toBeEnabled();
  });

  it('submits the trimmed name and navigates to returnTo on success', async () => {
    let captured;
    server.use(
      http.post(`${API_BASE}/mixlist`, async ({ request }) => {
        captured = await request.json();
        return HttpResponse.json(makeMixlist({ id: 'new-mix', name: captured.name }));
      }),
    );

    const { user } = renderForm({ pathname: '/create-mixlist', state: { returnTo: '/mixlists' } });

    await user.type(screen.getByPlaceholderText(NAME_PLACEHOLDER), '  Road Trip  ');
    await user.click(screen.getByRole('button', { name: /create mixlist/i }));

    // Lands on the returnTo destination.
    expect(await screen.findByText('Mixlists landing')).toBeInTheDocument();
    // Submitted payload is trimmed.
    expect(captured.name).toBe('Road Trip');
  });

  it('navigates to the new mixlist detail when no returnTo is provided', async () => {
    server.use(
      http.post(`${API_BASE}/mixlist`, () =>
        HttpResponse.json(makeMixlist({ id: 'new-mix' })),
      ),
    );

    const { user } = renderForm();

    await user.type(screen.getByPlaceholderText(NAME_PLACEHOLDER), 'Solo List');
    await user.click(screen.getByRole('button', { name: /create mixlist/i }));

    expect(await screen.findByText('Mixlist detail')).toBeInTheDocument();
  });

  it('alerts and stays on the form when the create request fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    const alertSpy = vi.spyOn(window, 'alert').mockImplementation(() => {});
    server.use(
      http.post(`${API_BASE}/mixlist`, () => new HttpResponse(null, { status: 500 })),
    );

    const { user } = renderForm();

    await user.type(screen.getByPlaceholderText(NAME_PLACEHOLDER), 'Doomed List');
    await user.click(screen.getByRole('button', { name: /create mixlist/i }));

    await waitFor(() => expect(alertSpy).toHaveBeenCalled());
    // Still on the form — no navigation occurred.
    expect(screen.getByRole('heading', { name: /create new mixlist/i })).toBeInTheDocument();

    alertSpy.mockRestore();
    consoleError.mockRestore();
  });
});
