import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import ImportResultPanel from './ImportResultPanel';

const STATS = [
  { label: 'Created', key: 'createdCount', color: '#4caf50' },
  { label: 'Linked', key: 'linkedCount' },
  { label: 'Without details', key: 'failedCount', color: (v) => (v ? '#f44336' : 'text.secondary') },
];

describe('ImportResultPanel', () => {
  it('shows each count under its label, the warnings, and the footer for a completed run', () => {
    renderWithProviders(
      <ImportResultPanel
        result={{ success: true, createdCount: 2, linkedCount: 1, failedCount: 0, warnings: ['One upload had no details.'], reindexTriggered: true }}
        title="Latest uploads imported"
        stats={STATS}
        footer={(r) => `${r.createdCount} new.`}
      />,
    );

    expect(screen.getByRole('heading', { name: 'Latest uploads imported' })).toBeInTheDocument();
    expect(screen.getByText('Created').parentElement).toHaveTextContent('2');
    expect(screen.getByText('Linked').parentElement).toHaveTextContent('1');
    expect(screen.getByText('Without details').parentElement).toHaveTextContent('0');
    expect(screen.getByText('One upload had no details.')).toBeInTheDocument();
    expect(screen.getByText(/2 new\. Search index updated\./)).toBeInTheDocument();
  });

  it('names the fatal reason and uses the failed title when the run aborted', () => {
    renderWithProviders(
      <ImportResultPanel
        result={{ success: false, errorMessage: 'The channel is no longer on YouTube.', createdCount: 0 }}
        title="Latest uploads imported"
        failedTitle="Import failed"
        stats={STATS}
      />,
    );

    expect(screen.getByRole('heading', { name: 'Import failed' })).toBeInTheDocument();
    expect(screen.getByText('The channel is no longer on YouTube.')).toBeInTheDocument();
    expect(screen.queryByRole('heading', { name: 'Latest uploads imported' })).not.toBeInTheDocument();
  });
});
