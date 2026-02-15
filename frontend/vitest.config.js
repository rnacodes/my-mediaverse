import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: [
      { find: /^@mui\/material(\/.*)?$/, replacement: path.resolve(__dirname, 'src/test-mocks/mui-material.jsx') },
      { find: /^@mui\/icons-material(\/.*)?$/, replacement: path.resolve(__dirname, 'src/test-mocks/mui-icons.jsx') },
      { find: /^@emotion\/react(\/.*)?$/, replacement: path.resolve(__dirname, 'src/test-mocks/emotion-react.jsx') },
      { find: /^@emotion\/styled(\/.*)?$/, replacement: path.resolve(__dirname, 'src/test-mocks/emotion-styled.jsx') },
    ]
  },
  test: {
    environment: 'happy-dom',
    setupFiles: ['./src/test-setup.js'],
    globals: true,
    css: false,
    pool: 'forks',
    testTimeout: 10000,
    hookTimeout: 10000,
    reporters: ['verbose'],
    env: {
      VITE_API_URL: 'http://localhost:5033/api'
    },
  },
  define: {
    'import.meta.env.VITE_API_URL': JSON.stringify('http://localhost:5033/api')
  }
});
