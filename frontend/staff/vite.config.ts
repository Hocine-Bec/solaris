import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react-swc';
import { loadEnv, searchForWorkspaceRoot } from 'vite';
import { defineConfig } from 'vitest/config';

const brand = fileURLToPath(new URL('../brand', import.meta.url));

export default defineConfig(({ mode }) => {
  // Where the API runs, for the development and preview servers. Not built into the app (no VITE_ prefix).
  const { API_PROXY_TARGET = 'http://localhost:5228' } = loadEnv(mode, process.cwd(), '');
  // The app calls /api on its own address, so the API's session cookie stays on one site; these servers pass it on.
  const proxy = { '/api': { target: API_PROXY_TARGET } };

  return {
    plugins: [react()],
    server: {
      port: 5174,   // the marketing site's server takes 5173
      proxy,
      // The tokens, fonts and components live in ../brand, shared with the marketing site.
      fs: { allow: [searchForWorkspaceRoot(process.cwd()), brand] },
    },
    preview: { port: 5174, proxy },
    test: {
      include: ['src/**/*.test.ts'],
      environment: 'node',
    },
  };
});
