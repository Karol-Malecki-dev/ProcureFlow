import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  build: {
    rollupOptions: {
      output: {
        manualChunks(id) {
          if (!id.includes('node_modules')) {
            return undefined;
          }

          if (id.includes('lucide-react')) {
            return 'icons';
          }

          if (id.includes('react-router')) {
            return 'router';
          }

          if (id.includes('react-dom') || id.includes('react/') || id.includes('scheduler')) {
            return 'react-vendor';
          }

          return 'vendor';
        },
      },
    },
  },
  server: {
    port: 3000,
    host: '0.0.0.0',
  },
  preview: {
    port: 3000,
    host: '0.0.0.0',
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: './src/setupTests.ts',
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
    pool: 'threads',
    minWorkers: 1,
    maxWorkers: 1,
  },
});
