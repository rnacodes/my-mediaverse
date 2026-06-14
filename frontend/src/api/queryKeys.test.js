import { describe, it, expect } from 'vitest';
import {
  mediaKeys,
  mixlistKeys,
  podcastKeys,
  bookKeys,
  movieKeys,
  tvShowKeys,
  videoKeys,
  articleKeys,
  documentKeys,
  noteKeys,
  highlightKeys,
  websiteKeys,
  youtubeKeys,
  topicKeys,
  genreKeys,
  typesenseKeys,
  aiKeys,
  readwiseKeys,
  traktKeys,
  recommendationKeys,
  relatedMediaKeys,
  tmdbKeys,
  backgroundJobsKeys,
  scriptExecutionKeys,
} from './queryKeys';

// Structural assertions over every query-key factory.

const expectStandardTrio = (keys, root) => {
  expect(keys.all).toEqual([root]);
  expect(keys.lists()).toEqual([root, 'list']);
  expect(keys.detail('id-1')).toEqual([root, 'detail', 'id-1']);
  // hierarchy: list & detail are namespaced under the root
  expect(keys.lists()[0]).toBe(root);
  expect(keys.detail('id-1')[0]).toBe(root);
};

describe('mediaKeys', () => {
  it('roots everything under ["media"]', () => {
    expect(mediaKeys.all).toEqual(['media']);
  });

  it('builds list, filtered-list, search and detail keys', () => {
    expect(mediaKeys.lists()).toEqual(['media', 'list']);
    expect(mediaKeys.byType('Book')).toEqual(['media', 'list', { type: 'Book' }]);
    expect(mediaKeys.byTopic('t-1')).toEqual(['media', 'list', { topicId: 't-1' }]);
    expect(mediaKeys.byGenre('g-1')).toEqual(['media', 'list', { genreId: 'g-1' }]);
    expect(mediaKeys.search('react')).toEqual(['media', 'search', 'react']);
    expect(mediaKeys.detail('m-1')).toEqual(['media', 'detail', 'm-1']);
  });

  it('namespaces filtered lists under lists() so list invalidation cascades', () => {
    expect(mediaKeys.byType('Book').slice(0, 2)).toEqual(mediaKeys.lists());
    expect(mediaKeys.byTopic('t-1').slice(0, 2)).toEqual(mediaKeys.lists());
    expect(mediaKeys.byGenre('g-1').slice(0, 2)).toEqual(mediaKeys.lists());
  });
});

describe('mixlistKeys', () => {
  it('exposes the standard trio under ["mixlist"]', () => {
    expectStandardTrio(mixlistKeys, 'mixlist');
  });
});

describe('podcastKeys', () => {
  it('roots under ["podcast"] with a top-level search', () => {
    expect(podcastKeys.all).toEqual(['podcast']);
    expect(podcastKeys.search('serial')).toEqual(['podcast', 'search', 'serial']);
  });

  it('namespaces series keys under ["podcast","series"]', () => {
    expect(podcastKeys.series.all).toEqual(['podcast', 'series']);
    expect(podcastKeys.series.lists()).toEqual(['podcast', 'series', 'list']);
    expect(podcastKeys.series.detail('s-1')).toEqual(['podcast', 'series', 'detail', 's-1']);
    expect(podcastKeys.series.search('serial')).toEqual(['podcast', 'series', 'search', 'serial']);
    expect(podcastKeys.series.subscribed()).toEqual(['podcast', 'series', 'subscribed']);
    expect(podcastKeys.series.episodes('s-1')).toEqual(['podcast', 'series', 'episodes', 's-1']);
  });

  it('namespaces episode keys under ["podcast","episode"]', () => {
    expect(podcastKeys.episodes.all).toEqual(['podcast', 'episode']);
    expect(podcastKeys.episodes.lists()).toEqual(['podcast', 'episode', 'list']);
    expect(podcastKeys.episodes.detail('e-1')).toEqual(['podcast', 'episode', 'detail', 'e-1']);
  });
});

describe('bookKeys', () => {
  it('exposes the standard trio under ["book"]', () => {
    expectStandardTrio(bookKeys, 'book');
  });
});

describe('movieKeys', () => {
  it('exposes the standard trio under ["movie"]', () => {
    expectStandardTrio(movieKeys, 'movie');
  });
});

describe('tvShowKeys', () => {
  it('exposes the standard trio under ["tvShow"]', () => {
    expectStandardTrio(tvShowKeys, 'tvShow');
  });
});

describe('videoKeys', () => {
  it('exposes the standard trio under ["video"]', () => {
    expectStandardTrio(videoKeys, 'video');
  });
});

describe('articleKeys', () => {
  it('exposes the standard trio under ["article"]', () => {
    expectStandardTrio(articleKeys, 'article');
  });
});

describe('documentKeys', () => {
  it('exposes the standard trio under ["document"]', () => {
    expectStandardTrio(documentKeys, 'document');
  });
});

describe('noteKeys', () => {
  it('exposes the standard trio plus search under ["note"]', () => {
    expectStandardTrio(noteKeys, 'note');
    expect(noteKeys.search('todo')).toEqual(['note', 'search', 'todo']);
  });
});

describe('highlightKeys', () => {
  it('exposes the standard trio plus search under ["highlight"]', () => {
    expectStandardTrio(highlightKeys, 'highlight');
    expect(highlightKeys.search('quote')).toEqual(['highlight', 'search', 'quote']);
  });
});

describe('websiteKeys', () => {
  it('exposes the standard trio under ["website"]', () => {
    expectStandardTrio(websiteKeys, 'website');
  });
});

describe('youtubeKeys', () => {
  it('roots under ["youtube"] with an external search', () => {
    expect(youtubeKeys.all).toEqual(['youtube']);
    expect(youtubeKeys.externalSearch('lofi', 'video', 'c-1')).toEqual([
      'youtube',
      'externalSearch',
      { query: 'lofi', type: 'video', channelId: 'c-1' },
    ]);
  });

  it('namespaces channel keys under ["youtube","channel"]', () => {
    expect(youtubeKeys.channels.all).toEqual(['youtube', 'channel']);
    expect(youtubeKeys.channels.lists()).toEqual(['youtube', 'channel', 'list']);
    expect(youtubeKeys.channels.detail('c-1')).toEqual(['youtube', 'channel', 'detail', 'c-1']);
    expect(youtubeKeys.channels.byExternalId('ext-1')).toEqual(['youtube', 'channel', 'external', 'ext-1']);
    expect(youtubeKeys.channels.videos('c-1')).toEqual(['youtube', 'channel', 'videos', 'c-1']);
  });

  it('namespaces playlist keys under ["youtube","playlist"] with includeVideos flag', () => {
    expect(youtubeKeys.playlists.all).toEqual(['youtube', 'playlist']);
    expect(youtubeKeys.playlists.lists()).toEqual(['youtube', 'playlist', 'list']);
    expect(youtubeKeys.playlists.detail('p-1')).toEqual([
      'youtube', 'playlist', 'detail', 'p-1', { includeVideos: false },
    ]);
    expect(youtubeKeys.playlists.detail('p-1', true)).toEqual([
      'youtube', 'playlist', 'detail', 'p-1', { includeVideos: true },
    ]);
    expect(youtubeKeys.playlists.byExternalId('ext-1')).toEqual([
      'youtube', 'playlist', 'external', 'ext-1', { includeVideos: false },
    ]);
    expect(youtubeKeys.playlists.byExternalId('ext-1', true)).toEqual([
      'youtube', 'playlist', 'external', 'ext-1', { includeVideos: true },
    ]);
    expect(youtubeKeys.playlists.videos('p-1')).toEqual(['youtube', 'playlist', 'videos', 'p-1']);
  });

  it('namespaces video keys under ["youtube","video"]', () => {
    expect(youtubeKeys.videos.all).toEqual(['youtube', 'video']);
    expect(youtubeKeys.videos.detail('v-1')).toEqual(['youtube', 'video', 'detail', 'v-1']);
  });
});

describe('topicKeys', () => {
  it('exposes the standard trio plus search under ["topic"]', () => {
    expectStandardTrio(topicKeys, 'topic');
    expect(topicKeys.search('ml')).toEqual(['topic', 'search', 'ml']);
  });
});

describe('genreKeys', () => {
  it('exposes the standard trio plus search under ["genre"]', () => {
    expectStandardTrio(genreKeys, 'genre');
    expect(genreKeys.search('jazz')).toEqual(['genre', 'search', 'jazz']);
  });
});

describe('typesenseKeys', () => {
  it('builds search keys under ["typesense"]', () => {
    const params = { q: 'react', page: 1 };
    expect(typesenseKeys.all).toEqual(['typesense']);
    expect(typesenseKeys.search(params)).toEqual(['typesense', 'search', params]);
    expect(typesenseKeys.mixlistSearch(params)).toEqual(['typesense', 'mixlistSearch', params]);
  });
});

describe('singleton key roots', () => {
  it('exposes bare roots for ai, readwise, trakt', () => {
    expect(aiKeys.all).toEqual(['ai']);
    expect(readwiseKeys.all).toEqual(['readwise']);
    expect(traktKeys.all).toEqual(['trakt']);
  });
});

describe('recommendationKeys', () => {
  it('builds list and by-media keys under ["recommendation"]', () => {
    expect(recommendationKeys.all).toEqual(['recommendation']);
    expect(recommendationKeys.lists()).toEqual(['recommendation', 'list']);
    expect(recommendationKeys.byMedia('m-1')).toEqual(['recommendation', 'byMedia', 'm-1']);
  });
});

describe('relatedMediaKeys', () => {
  it('builds by-media, saved and by-embedding keys under ["relatedMedia"]', () => {
    expect(relatedMediaKeys.all).toEqual(['relatedMedia']);
    expect(relatedMediaKeys.byMedia('m-1')).toEqual(['relatedMedia', 'byMedia', 'm-1']);
    expect(relatedMediaKeys.saved('m-1')).toEqual(['relatedMedia', 'saved', 'm-1']);
    expect(relatedMediaKeys.byEmbedding('m-1')).toEqual(['relatedMedia', 'byEmbedding', 'm-1']);
  });
});

describe('tmdbKeys', () => {
  it('roots under ["tmdb"]', () => {
    expect(tmdbKeys.all).toEqual(['tmdb']);
  });

  it('builds search keys carrying query/page/language', () => {
    expect(tmdbKeys.movieSearch('dune', 1, 'en')).toEqual([
      'tmdb', 'search', 'movies', { query: 'dune', page: 1, language: 'en' },
    ]);
    expect(tmdbKeys.tvSearch('loki', 2, 'en')).toEqual([
      'tmdb', 'search', 'tv', { query: 'loki', page: 2, language: 'en' },
    ]);
    expect(tmdbKeys.multiSearch('marvel', 1, 'en')).toEqual([
      'tmdb', 'search', 'multi', { query: 'marvel', page: 1, language: 'en' },
    ]);
  });

  it('builds detail, popular, genre and image keys', () => {
    expect(tmdbKeys.movieDetails('123', 'en')).toEqual(['tmdb', 'movie', '123', { language: 'en' }]);
    expect(tmdbKeys.tvDetails('456', 'en')).toEqual(['tmdb', 'tv', '456', { language: 'en' }]);
    expect(tmdbKeys.popularMovies(1, 'en')).toEqual(['tmdb', 'popular', 'movies', { page: 1, language: 'en' }]);
    expect(tmdbKeys.popularTv(1, 'en')).toEqual(['tmdb', 'popular', 'tv', { page: 1, language: 'en' }]);
    expect(tmdbKeys.movieGenres('en')).toEqual(['tmdb', 'genres', 'movies', { language: 'en' }]);
    expect(tmdbKeys.tvGenres('en')).toEqual(['tmdb', 'genres', 'tv', { language: 'en' }]);
    expect(tmdbKeys.imageUrl('/poster.jpg', 'w500')).toEqual([
      'tmdb', 'image', { imagePath: '/poster.jpg', size: 'w500' },
    ]);
  });
});

describe('backgroundJobsKeys', () => {
  it('builds enrichment-status keys under ["backgroundJobs"]', () => {
    expect(backgroundJobsKeys.all).toEqual(['backgroundJobs']);
    expect(backgroundJobsKeys.bookEnrichmentStatus()).toEqual(['backgroundJobs', 'bookEnrichment', 'status']);
    expect(backgroundJobsKeys.movieTvEnrichmentStatus()).toEqual(['backgroundJobs', 'movieTvEnrichment', 'status']);
    expect(backgroundJobsKeys.podcastEnrichmentStatus()).toEqual(['backgroundJobs', 'podcastEnrichment', 'status']);
  });
});

describe('scriptExecutionKeys', () => {
  it('builds health and job keys under ["scriptExecution"]', () => {
    expect(scriptExecutionKeys.all).toEqual(['scriptExecution']);
    expect(scriptExecutionKeys.health()).toEqual(['scriptExecution', 'health']);
    expect(scriptExecutionKeys.jobs(10)).toEqual(['scriptExecution', 'jobs', { limit: 10 }]);
    expect(scriptExecutionKeys.job('j-1')).toEqual(['scriptExecution', 'job', 'j-1']);
  });
});
