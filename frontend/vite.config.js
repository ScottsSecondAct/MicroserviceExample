import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/auth': { target: 'http://localhost:5000', changeOrigin: true },
      '/users': { target: 'http://localhost:5000', changeOrigin: true },
      '/contacts': { target: 'http://localhost:5000', changeOrigin: true },
      '/accounts': { target: 'http://localhost:5000', changeOrigin: true },
      '/deals': { target: 'http://localhost:5000', changeOrigin: true },
      '/pipeline': { target: 'http://localhost:5000', changeOrigin: true },
      '/activities': { target: 'http://localhost:5000', changeOrigin: true },
      '/reports': { target: 'http://localhost:5000', changeOrigin: true },
      '/admin': { target: 'http://localhost:5000', changeOrigin: true },
    },
  },
})
