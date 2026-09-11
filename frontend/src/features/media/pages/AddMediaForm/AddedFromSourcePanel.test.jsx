import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import AddedFromSourcePanel from './AddedFromSourcePanel';

// The panel is static: it tells the user which media types are created from a source
// instead of typed in, and links to the right importer for each.
describe('AddedFromSourcePanel', () => {
  it('links each source-added type to its importer', () => {
    renderWithProviders(<AddedFromSourcePanel />, { route: '/add-media' });

    expect(screen.getByText(/some media is added from its source/i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /import a website/i })).toHaveAttribute('href', '/import-website');
    expect(screen.getByRole('link', { name: /readwise sync/i })).toHaveAttribute('href', '/import-media?section=readwise');
    expect(screen.getByRole('link', { name: /youtube import/i })).toHaveAttribute('href', '/import-media?section=youtube');
    expect(screen.getByRole('link', { name: /import media page/i })).toHaveAttribute('href', '/import-media');
  });
});
