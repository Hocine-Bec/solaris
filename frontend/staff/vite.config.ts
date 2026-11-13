import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react-swc';
import { loadEnv, searchForWorkspaceRoot, type ProxyOptions } from 'vite';
import { defineConfig } from 'vitest/config';

const brand = fileURLToPath(new URL('../brand', import.meta.url));

export default defineConfig(({ mode }) => {
  // Where the API runs, for the development and preview servers. Not built into the app (no VITE_ prefix).
  const { API_PROXY_TARGET = 'http://localhost:5228' } = loadEnv(mode, process.cwd(), '');
  // The app calls /api on its own address, so the API's session cookie stays on one site; these servers pass it on.
  // When the API isn't running, they answer as a gateway does (502) rather than with Vite's 500, so the app says the
  // server didn't answer instead of reporting an error in it.
  const proxy: Record<string, ProxyOptions> = {
    '/api': {
      target: API_PROXY_TARGET,
      configure: server => server.on('error', (_error, _request, response) => {
        if ('writeHead' in response && !response.headersSent)
          response.writeHead(502, { 'Content-Type': 'text/plain' }).end(`The API isn't answering at ${API_PROXY_TARGET}.`);
      }),
    },
  };

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
