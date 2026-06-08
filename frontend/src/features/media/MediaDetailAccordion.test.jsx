import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import { makeBook } from '@/test/factories/media';
import MediaDetailAccordion from './MediaDetailAccordion';

// Smoke test only (RAS-34): this large media-type-switch component is left as a future
// decomposition project, so we just mount it with a happy-path item and assert it renders
// its type-specific details heading. No network fires for a Book (RSS/channel queries are
// gated to Website items and the link dialog respectively).

describe('MediaDetailAccordion', () => {
  it('mounts and renders the details heading for its media type', async () => {
    renderWithProviders(<MediaDetailAccordion mediaItem={makeBook()} navigate={() => {}} />);

    expect(await screen.findByRole('heading', { name: 'Book Details' })).toBeInTheDocument();
  });
});
