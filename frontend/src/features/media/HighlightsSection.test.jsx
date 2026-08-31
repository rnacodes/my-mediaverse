import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen, within } from '@/test/test-utils';
import { makeHighlight } from '@/test/factories/note';
import HighlightsSection from './HighlightsSection';

// HighlightsSection is a pure presentational child: it does NO fetching. The page
// owns the highlight queries and passes the results down as props, so these tests
// drive the component directly with props (no MSW needed). The section only renders
// for Book/Article media and is collapsed by default — bodies/empty/loading text
// appear only after the header is clicked to expand.

const expandHeader = async (user) => {
  await user.click(screen.getByRole('heading', { name: 'Highlights' }));
};

describe('HighlightsSection', () => {
  it('renders nothing for media that is not a Book or Article', () => {
    renderWithProviders(
      <HighlightsSection
        mediaItem={{ mediaType: 'Movie' }}
        highlights={[]}
        highlightsLoading={false}
      />,
    );

    expect(screen.queryByRole('heading', { name: 'Highlights' })).not.toBeInTheDocument();
  });

  it('shows the empty state for a Book once expanded', async () => {
    const { user } = renderWithProviders(
      <HighlightsSection
        mediaItem={{ mediaType: 'Book' }}
        highlights={[]}
        highlightsLoading={false}
      />,
    );

    // Header is always present for a Book; no count chip when there are none.
    expect(screen.getByRole('heading', { name: 'Highlights' })).toBeInTheDocument();

    await expandHeader(user);

    expect(screen.getByText(/no highlights found for this book\./i)).toBeInTheDocument();
    expect(screen.getByRole('link', { name: /readwise sync page/i })).toBeInTheDocument();
  });

  it('shows a count chip and the highlight bodies for a populated Book', async () => {
    const highlights = [
      makeHighlight({ text: 'First insightful highlight.' }),
      makeHighlight({ text: 'Second insightful highlight.' }),
    ];

    const { user } = renderWithProviders(
      <HighlightsSection
        mediaItem={{ mediaType: 'Article' }}
        highlights={highlights}
        highlightsLoading={false}
      />,
    );

    // Count chip lives in the (collapsed) header next to the title.
    const heading = screen.getByRole('heading', { name: 'Highlights' });
    expect(within(heading.parentElement).getByText('2')).toBeInTheDocument();

    await expandHeader(user);

    expect(screen.getByText('First insightful highlight.')).toBeInTheDocument();
    expect(screen.getByText('Second insightful highlight.')).toBeInTheDocument();
  });

  it('pages long highlight lists ten at a time behind a Show More button', async () => {
    const highlights = Array.from({ length: 25 }, (_, i) =>
      makeHighlight({ text: `Highlight number ${i + 1}.` }),
    );

    const { user } = renderWithProviders(
      <HighlightsSection
        mediaItem={{ mediaType: 'Article' }}
        highlights={highlights}
        highlightsLoading={false}
      />,
    );

    await expandHeader(user);

    // First page only — a heavily-highlighted article must not render everything at once.
    expect(screen.getByText('Highlight number 10.')).toBeInTheDocument();
    expect(screen.queryByText('Highlight number 11.')).not.toBeInTheDocument();
    expect(screen.getByText('Showing 10 of 25 highlights')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /show more \(15 remaining\)/i }));
    expect(screen.getByText('Highlight number 20.')).toBeInTheDocument();
    expect(screen.queryByText('Highlight number 21.')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /show more \(5 remaining\)/i }));
    expect(screen.getByText('Highlight number 25.')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /show more/i })).not.toBeInTheDocument();
  });

  it('shows the loading state (and no count chip) while highlights load', async () => {
    const { user } = renderWithProviders(
      <HighlightsSection
        mediaItem={{ mediaType: 'Book' }}
        highlights={[]}
        highlightsLoading={true}
      />,
    );

    // While loading the count chip is suppressed even though the header renders.
    const heading = screen.getByRole('heading', { name: 'Highlights' });
    expect(within(heading.parentElement).queryByText(/^\d+$/)).not.toBeInTheDocument();

    await expandHeader(user);

    expect(screen.getByText(/loading highlights\.\.\./i)).toBeInTheDocument();
  });
});
