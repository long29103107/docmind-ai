import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, '.', 'DOCMIND_')

  return {
    plugins: [react()],
    server: {
      proxy: {
        '/api': {
          target: env.DOCMIND_API_URL || 'http://localhost:5050',
          changeOrigin: true,
        },
      },
    },
  }
})
