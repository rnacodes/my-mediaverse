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
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html', 'lcov'],
      reportsDirectory: './coverage',
      // Listing the source folders makes files with no tests at all count as 0%,
      // instead of being left out of the report.
      include: ['src/**/*.{js,jsx}'],
      exclude: [
        '**/*.test.{js,jsx}',
        'src/test/**',
        'src/test-utils/**',
        'src/test-utils.js',
        'src/main.jsx',
        'src/assets/**',
      ],
    },
  },
  define: {
    'import.meta.env.VITE_API_URL': JSON.stringify('http://localhost:5033/api')
  }
});
