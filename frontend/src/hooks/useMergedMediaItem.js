import { useMemo } from 'react';
import { useMediaItem } from '@/hooks/useMedia';
import { useBook } from '@/hooks/useBook';
import { useMovie } from '@/hooks/useMovie';
import { useTvShow, useTvShowEpisode } from '@/hooks/useTvShow';
import { useVideo } from '@/hooks/useVideo';
import { useArticle } from '@/hooks/useArticle';
import { usePodcastSeries, usePodcastEpisode } from '@/hooks/usePodcast';
import { useWebsite } from '@/hooks/useWebsite';

export function useMergedMediaItem(id) {
  const basicQuery = useMediaItem(id);
  const basicMedia = basicQuery.data ?? null;
  const mediaType = basicMedia?.mediaType;

  // Type-specific detail fetches, each gated by the base mediaType.
  const bookQuery = useBook(id, { enabled: mediaType === 'Book' });
  const movieQuery = useMovie(id, { enabled: mediaType === 'Movie' });
  const videoQuery = useVideo(id, { enabled: mediaType === 'Video' });
  const articleQuery = useArticle(id, { enabled: mediaType === 'Article' });
  const tvShowQuery = useTvShow(id, { enabled: mediaType === 'TVShow', retry: false });
  // TV episodes share the TVShow media type. Probe the show endpoint first; when it
  // misses, the item is an episode and its own endpoint has the details.
  const tvEpisodeQuery = useTvShowEpisode(id, {
    enabled: mediaType === 'TVShow' && tvShowQuery.isError,
    retry: false,
  });
  const isTvEpisode = mediaType === 'TVShow' && !tvShowQuery.data && !!tvEpisodeQuery.data;
  // Websites carry link health and archive fields the generic media response does not.
  const websiteQuery = useWebsite(id, { enabled: mediaType === 'Website' });

  // Podcast: probe series first; if it resolves the item is a series, otherwise
  // fall back to the episode detail (and pull in the parent series for context).
  const seriesProbe = usePodcastSeries(id, { enabled: mediaType === 'Podcast', retry: false });
  const isPodcastSeries = mediaType === 'Podcast' && !!seriesProbe.data;
  const episodeQuery = usePodcastEpisode(id, {
    enabled: mediaType === 'Podcast' && seriesProbe.isError,
  });
  const parentSeriesId = episodeQuery.data?.seriesId;
  const parentSeriesQuery = usePodcastSeries(parentSeriesId, { enabled: !!parentSeriesId });

  const podcastKind = mediaType === 'Podcast'
    ? (isPodcastSeries ? 'Series' : (episodeQuery.data ? 'Episode' : null))
    : null;

  const isDetailReady = (() => {
    if (!basicMedia) return false;
    switch (mediaType) {
      case 'Book': return !!bookQuery.data;
      case 'Movie': return !!movieQuery.data;
      case 'Video': return !!videoQuery.data;
      case 'Article': return !!articleQuery.data;
      case 'Website': return !!websiteQuery.data || websiteQuery.isError;
      // Detail fetched via a probe that may legitimately error; treat the error as
      // "settled" so prefill falls back to base fields instead of hanging.
      case 'TVShow': return !!tvShowQuery.data || !!tvEpisodeQuery.data || tvEpisodeQuery.isError;
      case 'Podcast': return (isPodcastSeries && !!seriesProbe.data) || !!episodeQuery.data;
      // Types with no dedicated detail endpoint are complete once the base loads.
      default: return true;
    }
  })();

  const mediaItem = useMemo(() => {
    if (!basicMedia) return null;
    if (mediaType === 'Book' && bookQuery.data) return { ...basicMedia, ...bookQuery.data };
    if (mediaType === 'Movie' && movieQuery.data) return { ...basicMedia, ...movieQuery.data };
    if (mediaType === 'Video' && videoQuery.data) return { ...basicMedia, ...videoQuery.data };
    if (mediaType === 'Article' && articleQuery.data) return { ...basicMedia, ...articleQuery.data };
    if (mediaType === 'TVShow' && tvShowQuery.data) return { ...basicMedia, ...tvShowQuery.data };
    if (isTvEpisode) return { ...basicMedia, ...tvEpisodeQuery.data, isTvEpisode: true };
    if (mediaType === 'Website' && websiteQuery.data) return { ...basicMedia, ...websiteQuery.data };
    if (mediaType === 'Podcast' && isPodcastSeries && seriesProbe.data) {
      return { ...basicMedia, ...seriesProbe.data };
    }
    if (mediaType === 'Podcast' && episodeQuery.data) {
      const merged = { ...basicMedia, ...episodeQuery.data };
      if (parentSeriesQuery.data) merged.series = parentSeriesQuery.data;
      return merged;
    }
    return basicMedia;
  }, [
    basicMedia, mediaType, bookQuery.data, movieQuery.data, videoQuery.data,
    articleQuery.data, tvShowQuery.data, isTvEpisode, tvEpisodeQuery.data, websiteQuery.data,
    isPodcastSeries, seriesProbe.data, episodeQuery.data, parentSeriesQuery.data,
  ]);

  return {
    basicQuery,
    mediaItem,
    mediaType,
    podcastKind,
    isDetailReady,
    isPodcastSeries,
    isTvEpisode,
    tvShowData: tvShowQuery.data ?? null,
    isLoading: basicQuery.isLoading,
    error: basicQuery.error,
  };
}

export default useMergedMediaItem;
