import { describe, it, expect } from 'vitest';
import { renderWithProviders, screen } from '@/test/test-utils';
import DemoDataUploadPage from './DemoDataUploadPage';

// Smoke test only - left as a future decomposition

describe('DemoDataUploadPage', () => {
  it('mounts and renders its primary heading', async () => {
    renderWithProviders(<DemoDataUploadPage />, { route: '/demo-data-upload' });

    expect(await screen.findByText('Demo Data Upload')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Demo Data Upload' })).toBeInTheDocument();
  });
});
