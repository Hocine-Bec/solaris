import { fileURLToPath } from 'node:url';
import react from '@vitejs/plugin-react-swc';
import { searchForWorkspaceRoot, type Plugin } from 'vite';
import { defineConfig } from 'vitest/config';
import { headTags, NOT_FOUND, PAGES, pageFor } from './src/content/pages';

const MARK = '<!--page-head-->';

/**
 * Each page gets its own HTML file with its title, description and link-preview tags, so a link shared to /about
 * previews as About without running the site's JavaScript. The app keeps them up to date as the visitor moves
 * around (src/lib/pageMeta.ts). 404.html is for hosts that serve it for unknown paths.
 */
function pageHeads(): Plugin {
  let template = '';
  return {
    name: 'solaris-page-heads',
    // After Vite's own HTML step, which runs the post transform below and so hands over the finished template.
    enforce: 'post',
    transformIndexHtml: {
      order: 'post',
      handler(html, ctx) {
        if (ctx.server) return html.replace(MARK, headTags(pageFor(ctx.originalUrl ?? '/')));
        template = html;
        return html.replace(MARK, headTags(pageFor('/')));
      },
    },
    generateBundle() {
      if (!template) return;
      for (const page of [...PAGES.filter(p => p.path !== '/'), NOT_FOUND]) {
        const fileName = page === NOT_FOUND ? '404.html' : `${page.path.slice(1)}/index.html`;
        this.emitFile({ type: 'asset', fileName, source: template.replace(MARK, headTags(page)) });
      }
    },
  };
}

const brand = fileURLToPath(new URL('../brand', import.meta.url));

export default defineConfig({
  plugins: [react(), pageHeads()],
  server: {
    // The tokens, fonts and logo live in ../brand, shared with the CRM app.
    fs: { allow: [searchForWorkspaceRoot(process.cwd()), brand] },
  },
  test: {
    include: ['src/**/*.test.ts'],
    environment: 'node',
  },
});
