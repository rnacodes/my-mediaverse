import { useQuery } from '@tanstack/react-query';
import { useAllMixlists } from '@/hooks/useMixlist';
import { getAllMixlists } from '@/api/mixlistService';
import { fetchActivelyExploringMedia } from '@/api/typesenseService';
import { mediaKeys } from '@/api/queryKeys';

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

export default function useHomepageData() {
  const sharedQueryOptions = {
    retry: (failureCount, error) => isRetryableError(error) && failureCount < MAX_RETRIES,
    refetchOnWindowFocus: false,
  };

  const mixlistsQuery = useAllMixlists({
    ...sharedQueryOptions,
    queryFn: async () => withTimeout(getAllMixlists().then((r) => r.data)),
  });

  const mediaQuery = useQuery({
    queryKey: mediaKeys.lists(),
    ...sharedQueryOptions,
    queryFn: async () => withTimeout(fetchActivelyExploringMedia()),
  });

  const mixlists = mixlistsQuery.data ?? [];
  const mixlistsLoading = mixlistsQuery.isLoading;
  const mixlistsError = mixlistsQuery.error ? getErrorMessage(mixlistsQuery.error, 'mixlists') : null;

  // `isFetching` plus `failureCount` tells us a retry is in flight — surface "waking up the server".
  const wakingUp = (mixlistsQuery.isFetching && mixlistsQuery.failureCount > 0)
    || (mediaQuery.isFetching && mediaQuery.failureCount > 0);

  const activelyExploringMedia = mediaQuery.data ?? [];
  const activelyExploringLoading = mediaQuery.isLoading;
  const activelyExploringError = mediaQuery.error ? getErrorMessage(mediaQuery.error, 'actively exploring media') : null;

  return {
    mixlists,
    mixlistsLoading,
    mixlistsError,
    activelyExploringMedia,
    activelyExploringLoading,
    activelyExploringError,
    wakingUp,
  };
}
