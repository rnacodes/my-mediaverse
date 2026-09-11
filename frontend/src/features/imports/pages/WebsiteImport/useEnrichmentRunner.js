import { useCallback, useRef, useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { runWebsiteEnrichment } from '@/api/websiteService';
import { websiteKeys, mediaKeys } from '@/api/queryKeys';
import { extractErrorMessage } from './importPageStyles';

const PAGE_SIZE = 50;

const EMPTY_TOTALS = { processed: 0, enriched: 0, unchanged: 0, skipped: 0, failed: 0, screenshotsRendered: 0 };

/**
 * Drives the paged enrichment endpoint until nothing is pending: each call fills up to 50
 * websites, or as many as fit in the server's time budget, and reports how many are left, so
 * the loop is just "call again while pending". A page that touched nothing ends the loop; the
 * user can cancel between pages.
 */
export function useEnrichmentRunner() {
  const queryClient = useQueryClient();
  const cancelled = useRef(false);

  const [running, setRunning] = useState(false);
  const [progress, setProgress] = useState({ done: 0, total: 0 });
  const [totals, setTotals] = useState(EMPTY_TOTALS);
  const [quotaReached, setQuotaReached] = useState(false);
  const [warning, setWarning] = useState('');
  const [error, setError] = useState('');
  const [finished, setFinished] = useState(false);

  const start = useCallback(async (initialPending) => {
    cancelled.current = false;
    setRunning(true);
    setFinished(false);
    setError('');
    setWarning('');
    setQuotaReached(false);
    setTotals(EMPTY_TOTALS);
    setProgress({ done: 0, total: initialPending });

    let pending = initialPending;
    const sum = { ...EMPTY_TOTALS };

    try {
      while (pending > 0 && !cancelled.current) {
        const result = await runWebsiteEnrichment(PAGE_SIZE);

        sum.processed += result.totalProcessed ?? 0;
        sum.enriched += result.enrichedCount ?? 0;
        sum.unchanged += result.unchangedCount ?? 0;
        sum.skipped += result.skippedCount ?? 0;
        sum.failed += result.failedCount ?? 0;
        sum.screenshotsRendered += result.screenshotsRendered ?? 0;
        setTotals({ ...sum });

        if (result.quotaReached) setQuotaReached(true);
        if (result.warningMessage) setWarning(result.warningMessage);

        if (result.success === false) {
          setError(result.errorMessage || 'The enrichment run failed.');
          break;
        }

        const previousPending = pending;
        pending = result.pendingCount ?? 0;
        setProgress({ done: Math.max(0, initialPending - pending), total: initialPending });

        if (result.wasCancelled) {
          setError(`The enrichment run was interrupted; ${pending} website${pending === 1 ? '' : 's'} still pending.`);
          break;
        }

        if ((result.totalProcessed ?? 0) === 0) break;
        if (pending >= previousPending) {
          setError(`No progress on the last page; ${pending} website${pending === 1 ? '' : 's'} still pending.`);
          break;
        }
      }
    } catch (err) {
      setError(extractErrorMessage(err, 'The enrichment run failed.'));
    } finally {
      setRunning(false);
      setFinished(true);
      queryClient.invalidateQueries({ queryKey: websiteKeys.all });
      queryClient.invalidateQueries({ queryKey: mediaKeys.lists() });
    }
  }, [queryClient]);

  const cancel = useCallback(() => {
    cancelled.current = true;
  }, []);

  return { running, finished, progress, totals, quotaReached, warning, error, start, cancel };
}
