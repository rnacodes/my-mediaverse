import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import Footer from './Footer';

describe('Footer', () => {
  it('carries the TMDB notice and the Trakt mark beside the books credit', () => {
    renderWithProviders(<Footer />);

    expect(screen.getByText('This product uses the TMDB API but is not endorsed or certified by TMDB')).toBeInTheDocument();
    expect(screen.getByRole('img', { name: 'TMDB' })).toHaveAttribute('src', '/tmdb-primary-short-logo.svg');
    expect(screen.getByRole('img', { name: /trakt logo/i })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Trakt' })).toHaveAttribute('href', 'https://trakt.tv');
    expect(screen.getByRole('link', { name: 'Google Books' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Open Library' })).toBeInTheDocument();
  });
});
