import { describe, it, expect, vi } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import WebsiteImportSection from './WebsiteImportSection';

const navigate = vi.fn();
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom');
  return { ...actual, useNavigate: () => navigate };
});

// The section embeds the bookmarks-file importer and links to the other two import methods.
describe('WebsiteImportSection', () => {
  const render = () =>
    renderWithProviders(<WebsiteImportSection expanded="websites" onAccordionChange={() => () => {}} />, { route: '/import-media?section=websites' });

  it('embeds the bookmarks-file importer', () => {
    render();

    expect(screen.getByText(/import a bookmarks file/i)).toBeInTheDocument();
    expect(screen.getByTestId('bookmark-dropzone')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /choose file/i })).toBeInTheDocument();
  });

  it('links to the single-URL and paste tabs of the import page', async () => {
    const { user } = render();

    await user.click(screen.getByRole('button', { name: /import a single website/i }));
    await user.click(screen.getByRole('button', { name: /paste a list of links/i }));

    expect(navigate).toHaveBeenCalledWith('/import-website?tab=url');
    expect(navigate).toHaveBeenCalledWith('/import-website?tab=paste');
  });
});
