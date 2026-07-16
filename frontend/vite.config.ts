import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Optional: use relative `/api` + `/hubs` if you prefer proxy over CORS.
    // Default client config talks straight to http://localhost:5275 (CORS enabled).
    proxy: {
      '/api': {
        target: 'http://localhost:5275',
        changeOrigin: true,
      },
      '/hubs': {
        target: 'http://localhost:5275',
        changeOrigin: true,
        ws: true,
      },
      '/health': {
        target: 'http://localhost:5275',
        changeOrigin: true,
      },
    },
  },
})
