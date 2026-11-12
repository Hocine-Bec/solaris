// Motion helpers shared by the sections. All motion is written by hand: CSS transitions and keyframes, plus a few
// scroll-driven effects that run in one animation frame per scroll (no animation library).

const query = (q: string) => () => matchMedia(q).matches;

export const prefersReducedMotion = query('(prefers-reduced-motion: reduce)');
/** Where the pinned sections pin */
export const isWide = query('(min-width: 1024px)');
/** Where the hero's panel flies */
export const isWideLandscape = query('(min-width: 1024px) and (orientation: landscape)');

/** Runs on every scroll frame; its optional measure() runs first after a resize. */
export type Scroller = { (): void; measure?: () => void };

const scrollers = new Set<Scroller>();
let ticking = false;

function runScrollers() {
  ticking = false;
  for (const fn of scrollers) fn();
}

/** Asks for one scroll frame (several calls in the same frame run the scrollers once). */
export function requestScrollFrame() {
  if (ticking) return;
  ticking = true;
  requestAnimationFrame(runScrollers);
}

addEventListener('scroll', requestScrollFrame, { passive: true });
addEventListener('resize', () => {
  for (const fn of scrollers) fn.measure?.();
  requestScrollFrame();
});

/** Adds a scroller and runs it on the next frame; returns the function that removes it. */
export function addScroller(fn: Scroller): () => void {
  scrollers.add(fn);
  requestScrollFrame();
  return () => {
    scrollers.delete(fn);
  };
}

/** 0 before a, 1 after b, linear in between */
export const progress = (p: number, a: number, b: number) => Math.min(1, Math.max(0, (p - a) / (b - a)));
