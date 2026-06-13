import { defineConfig, configDefaults } from 'vitest/config';
import react from '@vitejs/plugin-react';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: [
      { find: /^@\/(.*)$/, replacement: path.resolve(__dirname, 'src') + '/$1' },
    ]
  },
  test: {
    exclude: [...configDefaults.exclude, 'src/test/quarantine/**'],
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.js'],
    globals: true,
    css: false,
    pool: 'forks',
    testTimeout: 10000,
    hookTimeout: 10000,
    env: {
      VITE_API_URL: 'http://localhost:5033/api'
    },
  },
  define: {
    'import.meta.env.VITE_API_URL': JSON.stringify('http://localhost:5033/api')
  }
});
