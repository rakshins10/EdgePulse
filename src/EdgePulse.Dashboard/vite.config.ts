import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    proxy: {
      '/api': {
        // API runs on 5104 per src/backend/EdgePulse.API/Properties/launchSettings.json
        target: 'http://localhost:5104',
        changeOrigin: true,
      },
    },
  },
  css: {
    modules: {
      localsConvention: 'camelCase',
    },
  },
  build: {
    rollupOptions: {
      output: {
        // Split large third-party libs out of the main bundle so no single
        // chunk balloons past the warning limit and browsers can cache them.
        manualChunks(id) {
          if (id.includes('node_modules')) {
            if (id.includes('recharts') || id.includes('d3')) return 'charts'
            if (id.includes('react')) return 'react'
            if (id.includes('i18next')) return 'i18n'
            return 'vendor'
          }
        },
      },
    },
  },
})
