import js from '@eslint/js'
import globals from 'globals'
import react from 'eslint-plugin-react'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'
import jsxA11y from 'eslint-plugin-jsx-a11y'
import { defineConfig, globalIgnores } from 'eslint/config'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{js,jsx}'],
    extends: [
      js.configs.recommended,
      react.configs.flat.recommended,
      jsxA11y.flatConfigs.recommended,
      reactHooks.configs['recommended-latest'],
      reactRefresh.configs.vite,
    ],
    languageOptions: {
      ecmaVersion: 2020,
      globals: globals.browser,
      parserOptions: {
        ecmaVersion: 'latest',
        ecmaFeatures: { jsx: true },
        sourceType: 'module',
      },
    },
    settings: {
      react: { version: 'detect' },
    },
    rules: {
      'no-unused-vars': ['error', { varsIgnorePattern: '^_', argsIgnorePattern: '^_' }],
      'react/prop-types': 'off',
      'react/react-in-jsx-scope': 'off',
      'react/jsx-key': 'error',
      'react/no-array-index-key': 'error',
      'react-hooks/exhaustive-deps': 'error',
      // Every API call must go through the service layer in src/api/, which owns the
      // shared client that attaches the auth token. A component calling axios directly
      // sends an unauthenticated request, which fails against an auth-required API.
      'no-restricted-imports': ['error', {
        paths: [{
          name: 'axios',
          message: 'Import a service from @/api/ instead. Only src/api/ may use axios directly — see apiClient.js.',
        }],
      }],
    },
  },
  {
    // The service layer is where the HTTP client lives, so axios is expected here.
    files: ['src/api/**/*.{js,jsx}'],
    rules: {
      'no-restricted-imports': 'off',
    },
  },
  {
    files: ['**/*.test.{js,jsx}', '**/__tests__/**/*.{js,jsx}', '**/test-utils.{js,jsx}'],
    languageOptions: {
      globals: {
        ...globals.node,
        describe: 'readonly',
        it: 'readonly',
        expect: 'readonly',
        beforeEach: 'readonly',
        afterEach: 'readonly',
        beforeAll: 'readonly',
        afterAll: 'readonly',
        vi: 'readonly',
      },
    },
    rules: {
      // Test helpers re-export testing-library wholesale; the rule can't verify
      // `export *` and there is no fast-refresh boundary to protect here.
      'react-refresh/only-export-components': 'off',
    },
  },
  {
    files: ['vite.config.js', 'vitest.config.js', 'src/test/setup.js'],
    languageOptions: {
      globals: globals.node,
    },
  },
  {
    files: ['src/test-mocks/**/*.{js,jsx}'],
    rules: {
      'react-refresh/only-export-components': 'off',
      'no-unused-vars': 'off',
      'jsx-a11y/no-autofocus': 'off',
      'jsx-a11y/click-events-have-key-events': 'off',
      'jsx-a11y/no-static-element-interactions': 'off',
      'jsx-a11y/interactive-supports-focus': 'off',
      'jsx-a11y/no-noninteractive-element-to-interactive-role': 'off',
    },
  },
])
