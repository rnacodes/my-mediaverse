import { useMemo } from 'react';
import { useAllMixlists, useSeedMixlists } from '@/hooks/useMixlist';
import { useAllMedia } from '@/hooks/useMedia';
import { getAllMixlists } from '@/api/mixlistService';
import { getAllMedia } from '@/api/mediaService';

const REQUEST_TIMEOUT_MS = 30000;
const MAX_RETRIES = 2;

// Race the query against a timeout so a hung server surfaces as a retryable error
// instead of leaving the UI on a permanent spinner (cold-start protection).
const withTimeout = (promise) => Promise.race([
    promise,
    new Promise((_, reject) => setTimeout(() => reject(new Error('TIMEOUT')), REQUEST_TIMEOUT_MS)),
]);

const isRetryableError = (error) =>
    error?.message === 'TIMEOUT' ||
    error?.code === 'ERR_NETWORK' ||
    error?.message === 'Network Error';

const getErrorMessage = (error, context) => {
    if (error?.message === 'TIMEOUT') {
        return 'Request timed out. The server may still be starting up — please try again.';
    }
    if (error?.code === 'ERR_NETWORK' || error?.message === 'Network Error') {
        return 'Unable to connect to the server. Please make sure the backend API is running.';
    }
    if (error?.response?.status === 404) {
        return 'API endpoint not found. Please check the backend configuration.';
    }
    if (error?.response?.status >= 500) {
        return 'Server error occurred. Please try again later.';
    }
    return `Failed to load ${context}. Please check your connection.`;
};

// Composes the homepage's data needs: recent mixlists, the full media list (used to
// derive "actively exploring"), cold-start handling, and the dev-only seed mutation.
// The query keys still match the shared useAllMixlists/useAllMedia caches; we only
// override queryFn to wrap each request in a timeout race.
export default function useHomepageData() {
  const sharedQueryOptions = {
    retry: (failureCount, error) => isRetryableError(error) && failureCount < MAX_RETRIES,
    refetchOnWindowFocus: false,
  };

  const mixlistsQuery = useAllMixlists({
    ...sharedQueryOptions,
    queryFn: async () => withTimeout(getAllMixlists().then((r) => r.data)),
  });

  const mediaQuery = useAllMedia({
    ...sharedQueryOptions,
    queryFn: async () => withTimeout(getAllMedia().then((r) => r.data)),
  });

  const mixlists = mixlistsQuery.data ?? [];
  const mixlistsLoading = mixlistsQuery.isLoading;
  const mixlistsError = mixlistsQuery.error ? getErrorMessage(mixlistsQuery.error, 'mixlists') : null;

  // `isFetching` plus `failureCount` tells us a retry is in flight — surface "waking up the server".
  const wakingUp = (mixlistsQuery.isFetching && mixlistsQuery.failureCount > 0)
    || (mediaQuery.isFetching && mediaQuery.failureCount > 0);

  const activelyExploringMedia = useMemo(() => {
    const items = mediaQuery.data ?? [];
    return items.filter((item) => {
      const status = item.status || item.Status;
      return status && (
        status.toLowerCase() === 'actively exploring' ||
        status.toLowerCase() === 'activelyexploring' ||
        status.toLowerCase() === 'inprogress'
      );
    });
  }, [mediaQuery.data]);
  const activelyExploringLoading = mediaQuery.isLoading;
  const activelyExploringError = mediaQuery.error ? getErrorMessage(mediaQuery.error, 'actively exploring media') : null;

  const seedMutation = useSeedMixlists();

  return {
    mixlists,
    mixlistsLoading,
    mixlistsError,
    activelyExploringMedia,
    activelyExploringLoading,
    activelyExploringError,
    wakingUp,
    seedMutation,
  };
}
