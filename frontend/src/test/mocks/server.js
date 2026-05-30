import { setupServer } from 'msw/node';
import { handlers } from './handlers';

// Node-side MSW server shared across the suite. Lifecycle (listen/reset/close)
// is wired in src/test/setup.js.
export const server = setupServer(...handlers);
