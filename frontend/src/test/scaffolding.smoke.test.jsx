import { describe, it, expect } from 'vitest';
import { Typography } from '@mui/material';
import { renderWithProviders, screen } from './test-utils';

// Verifies the A.2 scaffolding hangs together: renderWithProviders imports
// cleanly, the full provider stack mounts (AuthProvider fires /auth/refresh
// against MSW), and real MUI renders on jsdom.
describe('test scaffolding', () => {
  it('renders a component through the provider stack', () => {
    renderWithProviders(<Typography>scaffolding works</Typography>);
    expect(screen.getByText('scaffolding works')).toBeInTheDocument();
  });
});
