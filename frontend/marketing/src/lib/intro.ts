// The intro, on every load of the home page: on black, SOLARIS draws itself large (every letter's outline, then all
// the letters filled together), its sun rises, then the wordmark shrinks into the header to become the page's logo
// while the black fades. Any click, key, scroll or touch skips it. index.html shows the black screen before the
// scripts load, and removes it on other pages and with reduced motion; this module plays what's left.
import { wordmarkElement } from './wordmark';

let resolveFlying!: () => void;
let resolveEnded!: () => void;
/** Resolves when the wordmark starts its flight: the page's own entrance can start. */
export const introFlying = new Promise<void>(resolve => { resolveFlying = resolve; });
/** Resolves once the splash is gone. */
export const introEnded = new Promise<void>(resolve => { resolveEnded = resolve; });

const splash = document.getElementById('splash');
if (!splash) {
  resolveFlying();
  resolveEnded();
}

/** Lines about 1.4 px on screen whatever the size; each letter starts a little after the one before; returns the
 *  seconds until the sun has risen. */
function animateWordmark(svg: SVGSVGElement, { dur = 1.5, stagger = .1, px = 1.4 } = {}) {
  const unit = svg.viewBox.baseVal.width / svg.getBoundingClientRect().width;
  svg.style.setProperty('--sw', (px * unit).toFixed(3));
  for (const g of svg.querySelectorAll<SVGGElement>('g[data-i]')) {
    for (const line of g.querySelectorAll<SVGPathElement>('.wm-line')) {
      line.style.setProperty('--len', line.getTotalLength().toFixed(1));
      line.style.setProperty('--d', `${(Number(g.dataset.i) * stagger).toFixed(2)}s`);
      line.style.setProperty('--dur', `${dur}s`);
    }
  }
  const fillAt = 6 * stagger + dur * .8;
  svg.style.setProperty('--fill-at', `${fillAt.toFixed(2)}s`);
  svg.style.setProperty('--dd', `${(fillAt + .3).toFixed(2)}s`);
  svg.classList.add('anim');
  return fillAt + .3 + .7;
}

let started = false;

/** Plays the intro into the header's logo, if the splash is there. */
export function playIntro(headerLogo: SVGSVGElement) {
  if (!splash || started) return;
  started = true;
  const svg = wordmarkElement();
  splash.prepend(svg);

  // No scrolling while it plays: a scroll would turn the header into its pill and move the logo the wordmark flies to.
  const scrollKeys = [' ', 'ArrowDown', 'ArrowUp', 'PageDown', 'PageUp', 'Home', 'End'];
  const block = (e: Event) => {
    if (e.type !== 'keydown' || scrollKeys.includes((e as KeyboardEvent).key)) e.preventDefault();
  };
  const blocked = ['wheel', 'touchmove', 'keydown'] as const;
  for (const type of blocked) addEventListener(type, block, { passive: false, capture: true });

  const skips = ['click', 'keydown', 'wheel', 'touchstart'] as const;
  const onSkip = () => fly(true);
  let flying = false;

  // From the splash to the header: one transform from the big wordmark's box to the header logo's (same view box).
  function fly(skipped: boolean) {
    if (flying) return;
    flying = true;
    for (const type of skips) removeEventListener(type, onSkip, true);
    if (skipped) svg.classList.add('done');
    const from = svg.getBoundingClientRect(), to = headerLogo.getBoundingClientRect(), scale = to.width / from.width;
    splash!.classList.add('flying');
    requestAnimationFrame(() => requestAnimationFrame(() => {
      svg.style.transform = `translate(${(to.left - from.left).toFixed(1)}px, ${(to.top - from.top).toFixed(1)}px) scale(${scale.toFixed(4)})`;
    }));
    resolveFlying();
    let done = false;
    const finish = () => {
      if (done) return;
      done = true;
      for (const type of blocked) removeEventListener(type, block, { capture: true });
      document.documentElement.classList.remove('intro-running');
      splash!.remove();
      resolveEnded();
    };
    svg.addEventListener('transitionend', e => { if (e.target === svg) finish(); });
    setTimeout(finish, 1500);
  }

  for (const type of skips) addEventListener(type, onSkip, true);
  // Outlines 1.5 s each, 0.1 s apart; all letters fill together once drawn; the sun rises; then the flight.
  requestAnimationFrame(() => {
    const seconds = animateWordmark(svg);
    setTimeout(() => fly(false), (seconds + .1) * 1000);
  });
}
