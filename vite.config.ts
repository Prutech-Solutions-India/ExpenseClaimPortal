import { fileURLToPath, URL } from 'node:url';

import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// The API this bundle talks to in development. `dotnet run` binds here, and
// every request the browser makes to /api or /health is forwarded to it, so
// the frontend calls the same paths in development that it calls in
// production. Without the proxy the app would need one base URL for local work
// and another once deployed, which is how a frontend ends up shipping with a
// localhost address baked into it.
const API_ORIGIN = process.env.API_ORIGIN ?? 'http://localhost:5080';

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src/web', import.meta.url)),
    },
  },
  server: {
    proxy: {
      '/api': { target: API_ORIGIN, changeOrigin: true },
      '/health': { target: API_ORIGIN, changeOrigin: true },
    },
  },
  build: {
    // Straight into the service's static root. The two layers ship as one
    // artifact: ASP.NET serves the bundle it was published with, so a deployed
    // frontend can never be a different commit from the API behind it.
    outDir: 'src/PilotService/wwwroot',
    emptyOutDir: true,
  },
  test: {
    globals: true,
    // Component tests need a DOM. The smoke suite talks to a deployed URL
    // instead and opts out per file with `@vitest-environment node`.
    environment: 'jsdom',
    setupFiles: ['./tests/web/setup.ts'],
    include: ['tests/web/**/*.test.{ts,tsx}'],
    reporters: ['default', 'junit'],
    outputFile: {
      junit: 'reports/vitest.xml',
    },
  },
});
