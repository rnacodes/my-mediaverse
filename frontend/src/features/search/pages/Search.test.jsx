import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import Search from './Search';

describe('Search page', () => {
  it('browses all media by default (as served at /all-media)', async () => {
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media' });

    // Both seeded media items render — no filter selection required.
    expect(await screen.findByText('Test Book')).toBeInTheDocument();
    expect(await screen.findByText('Test Movie')).toBeInTheDocument();

    // The "please select filters" empty prompt must NOT appear in browse-all mode.
    expect(screen.queryByText('Select filters to search')).not.toBeInTheDocument();
  });

  it('narrows to a single media type from the mediaType URL param', async () => {
    renderWithProviders(<Search defaultMediaTypes={['all']} />, { route: '/all-media?mediaType=Book' });

    // Only the Book comes back — the filter param drove a media_type:=Book query.
    expect(await screen.findByText('Test Book')).toBeInTheDocument();
    expect(screen.queryByText('Test Movie')).not.toBeInTheDocument();
  });
});
