import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/auth': {
        target: 'http://localhost:5188',
        rewrite: (path) => path.replace(/^\/auth/, ''),
      },
      '/users': {
        target: 'http://localhost:5151',
        rewrite: (path) => path.replace(/^\/users/, ''),
      },
    },
  },
})
