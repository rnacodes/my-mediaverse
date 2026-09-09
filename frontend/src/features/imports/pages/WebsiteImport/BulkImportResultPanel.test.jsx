import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import { server } from '@/test/mocks/server';
import { API_BASE } from '@/test/mocks/handlers';
import BulkImportResultPanel from './BulkImportResultPanel';

// "Enrich now" calls POST /website/enrichment/run one page at a time until the server reports
// nothing pending, accumulating the counts along the way.
const result = (overrides = {}) => ({
  success: true, operation: 'bookmark-import', totalProcessed: 5, createdCount: 3, updatedCount: 1,
  skippedCount: 1, failedCount: 0, nonWebLinkCount: 0, errors: [], foldersFound: 1, topicsCreatedCount: 1,
  pendingEnrichmentCount: 3, reindexTriggered: true, warningMessage: null,
  ...overrides,
});

const page = (overrides = {}) => ({
  success: true, operation: 'website-enrichment', totalProcessed: 0, enrichedCount: 0, unchangedCount: 0,
  skippedCount: 0, failedCount: 0, screenshotsRendered: 0, quotaReached: false, pendingCount: 0,
  warningMessage: null, ...overrides,
});

describe('BulkImportResultPanel', () => {
  it('shows the counts, the warning and the problems list', async () => {
    const { user } = renderWithProviders(
      <BulkImportResultPanel result={result({ failedCount: 1, warningMessage: '1 entry could not be imported.', errors: ['https://bad: nope'] })} />,
    );

    const panel = screen.getByTestId('bulk-import-result');
    expect(within(panel).getByText('Created').previousSibling).toHaveTextContent('3');
    expect(within(panel).getByText('Failed').previousSibling).toHaveTextContent('1');
    expect(within(panel).getByText(/could not be imported/i)).toBeInTheDocument();
    await user.click(within(panel).getByText(/problems \(1\)/i));
    expect(await within(panel).findByText('https://bad: nope')).toBeInTheDocument();
    expect(within(panel).getByText(/3 websites are waiting/i)).toBeInTheDocument();
  });

  it('runs enrichment page by page until nothing is pending', async () => {
    const calls = [];
    const responses = [
      page({ totalProcessed: 2, enrichedCount: 2, pendingCount: 1 }),
      page({ totalProcessed: 1, enrichedCount: 1, pendingCount: 0 }),
    ];
    server.use(
      http.post(`${API_BASE}/website/enrichment/run`, ({ request }) => {
        calls.push(new URL(request.url).searchParams.get('limit'));
        return HttpResponse.json(responses[calls.length - 1] ?? page());
      }),
    );
    const { user } = renderWithProviders(<BulkImportResultPanel result={result()} />);

    await user.click(screen.getByRole('button', { name: /enrich now \(3 pending\)/i }));

    expect(await screen.findByText(/enrichment finished/i)).toBeInTheDocument();
    expect(screen.getByText(/3 enriched, 0 already complete/i)).toBeInTheDocument();
    expect(screen.getByText(/enriched 3 of 3/i)).toBeInTheDocument();
    expect(calls).toEqual(['50', '50']);
    expect(screen.getByRole('button', { name: /enrich now/i })).toBeDisabled();
  });

  it('stops and explains when the screenshot budget runs out', async () => {
    server.use(
      http.post(`${API_BASE}/website/enrichment/run`, () =>
        HttpResponse.json(page({ totalProcessed: 3, enrichedCount: 3, pendingCount: 0, quotaReached: true, warningMessage: 'Screenshot quota reached; 2 website(s) were left without a thumbnail.' })),
      ),
    );
    const { user } = renderWithProviders(<BulkImportResultPanel result={result()} />);

    await user.click(screen.getByRole('button', { name: /enrich now/i }));

    expect(await screen.findByText(/monthly screenshot budget ran out/i)).toBeInTheDocument();
    expect(screen.getByText(/enrichment finished/i)).toBeInTheDocument();
  });

  it('offers nothing to enrich when the library is already complete', () => {
    renderWithProviders(<BulkImportResultPanel result={result({ pendingEnrichmentCount: 0 })} />);

    expect(screen.getByText(/already has its details/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /enrich now \(0 pending\)/i })).toBeDisabled();
  });
});
