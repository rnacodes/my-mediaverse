import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import AttributionBadge from './AttributionBadge';

describe('AttributionBadge', () => {
  it('renders the TMDB logo beside its required notice', () => {
    renderWithProviders(<AttributionBadge provider="tmdb" />);

    expect(screen.getByAltText('TMDB')).toHaveAttribute('src', '/tmdb-primary-short-logo.svg');
    expect(screen.getByText(/not endorsed or certified by TMDB/i)).toBeInTheDocument();
  });

  it('renders the Trakt logo and a "Powered by" line, both linked to Trakt', () => {
    renderWithProviders(<AttributionBadge provider="trakt" />);

    expect(screen.getByAltText('Trakt logo')).toHaveStyle({ height: '40px' });
    const links = screen.getAllByRole('link');
    expect(links).toHaveLength(2);
    links.forEach((link) => expect(link).toHaveAttribute('href', 'https://trakt.tv'));
    expect(screen.getByText(/powered by/i)).toBeInTheDocument();
  });

  it('draws the large Trakt size with the bigger logo', () => {
    renderWithProviders(<AttributionBadge provider="trakt" size="large" />);

    expect(screen.getByAltText('Trakt logo')).toHaveStyle({ height: '50px' });
  });

  it('renders Apple Podcasts as a plain caption with no logo or link', () => {
    renderWithProviders(<AttributionBadge provider="apple" />);

    expect(screen.getByText('Search results from Apple Podcasts')).toBeInTheDocument();
    expect(screen.queryByRole('img')).not.toBeInTheDocument();
    expect(screen.queryByRole('link')).not.toBeInTheDocument();
  });

  it('renders Podcast Index as a caption ending in its link', () => {
    renderWithProviders(<AttributionBadge provider="podcastindex" />);

    expect(screen.getByRole('link', { name: 'Podcast Index' })).toHaveAttribute('href', 'https://podcastindex.org');
  });

  it('renders nothing for a provider it does not know', () => {
    const { container } = renderWithProviders(<AttributionBadge provider="nope" />);

    expect(container).toBeEmptyDOMElement();
  });
});
