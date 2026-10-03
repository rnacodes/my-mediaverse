import ImportResultPanel from '@/shared/ImportResultPanel';

const STATS = [
  { label: 'Created', key: 'createdCount', color: '#4caf50' },
  { label: 'Updated', key: 'updatedCount', color: '#90caf9' },
  { label: 'Skipped', key: 'skippedCount' },
  { label: 'Failed seasons', key: 'failedCount', color: (value) => (value ? '#f44336' : 'text.secondary') },
];

/**
 * What one "Import episodes from TMDB" run did: created / updated / skipped episodes,
 * seasons that failed, and the fatal reason when the run aborted.
 */
function TvEpisodeImportResultPanel({ result, onDismiss }) {
  return (
    <ImportResultPanel
      result={result}
      title="Episode import complete"
      failedTitle="Episode import failed"
      stats={STATS}
      footer={(r) => (r.seasonsProcessed
        ? `${r.seasonsProcessed} season${r.seasonsProcessed === 1 ? '' : 's'} checked on TMDB.`
        : '')}
      onDismiss={onDismiss}
      testId="tv-episode-import-result"
    />
  );
}

export default TvEpisodeImportResultPanel;
