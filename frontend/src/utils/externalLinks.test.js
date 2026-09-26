import { describe, it, expect } from 'vitest';
import {
  getJustWatchUrl,
  getImdbUrl,
  getTmdbMovieUrl,
  getTmdbTvShowUrl,
  getTmdbImageUrl,
} from './externalLinks';

describe('externalLinks', () => {
  it('builds a JustWatch search URL with the title encoded', () => {
    expect(getJustWatchUrl('Chernobyl & Co')).toBe(
      'https://www.justwatch.com/us/search?q=Chernobyl%20%26%20Co',
    );
    expect(getJustWatchUrl('')).toBeNull();
  });

  it('builds IMDb and TMDB title URLs from their ids', () => {
    expect(getImdbUrl('tt1375666')).toBe('https://www.imdb.com/title/tt1375666/');
    expect(getTmdbMovieUrl(27205)).toBe('https://www.themoviedb.org/movie/27205');
    expect(getTmdbTvShowUrl(87108)).toBe('https://www.themoviedb.org/tv/87108');
  });

  it('returns null for a missing id so callers can hide the link', () => {
    expect(getImdbUrl(null)).toBeNull();
    expect(getTmdbMovieUrl(undefined)).toBeNull();
    expect(getTmdbTvShowUrl('')).toBeNull();
  });

  it('builds a hotlinked TMDB image URL at the requested size', () => {
    expect(getTmdbImageUrl('/abc.jpg')).toBe('https://image.tmdb.org/t/p/w500/abc.jpg');
    expect(getTmdbImageUrl('/abc.jpg', 'w185')).toBe('https://image.tmdb.org/t/p/w185/abc.jpg');
    expect(getTmdbImageUrl(null)).toBeNull();
  });
});
