import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import { makeBook } from '@/test/factories/media';
import MediaDetailAccordion from './MediaDetailAccordion';

// Smoke test only - left as a future decomposition

describe('MediaDetailAccordion', () => {
  it('mounts and renders the details heading for its media type', async () => {
    renderWithProviders(<MediaDetailAccordion mediaItem={makeBook()} navigate={() => {}} />);

    expect(await screen.findByRole('heading', { name: 'Book Details' })).toBeInTheDocument();
  });
});
