import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'url'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  define: {
    // Make environment variables available at build time
    __API_URL__: JSON.stringify(process.env.VITE_API_URL || 'http://localhost:5033/api')
  },
  server: {
    // Configure the dev server to handle client-side routing
    // This ensures that refreshing a page doesn't result in a 404
    historyApiFallback: true,
    proxy: {
      // Optional: proxy API requests during development
      '/api': {
        target: 'http://localhost:5033',
        changeOrigin: true,
        secure: false
      }
    }
  }
})
