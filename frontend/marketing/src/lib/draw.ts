// Line drawings that draw themselves (What you get, the house in How it works) and steps that animate in (How it
// works, About). The classes they toggle are styled in styles/site.css and, for the house, ../../brand/drawings.css.
import { addScroller, prefersReducedMotion } from './motion';
import { drawable } from '../../../brand/drawings/drawable';

export { drawable };

const SHAPES = 'polygon, line, ellipse, circle, polyline, path, rect';

/** Prepares a step's icon lines to draw, and staggers its text (How it works, About). */
export function prepareSteps(box: HTMLElement) {
  for (const el of box.querySelectorAll<SVGGeometryElement>(`.ic :is(${SHAPES})`)) {
    if (el.dataset.d !== undefined) continue;
    el.style.setProperty('--len', el.getTotalLength().toFixed(2));
    el.dataset.d = '';
  }
  box.querySelectorAll<HTMLElement>('dl > *, :scope.before > li').forEach((el, i) => el.style.setProperty('--td', `${(.45 + i * .07).toFixed(2)}s`));
}

type Box = HTMLElement & { _on?: boolean };

/**
 * Draws each box when it comes into view. By default it replays every time (the drawings) and resets once the box has
 * fully left; with `once`, it plays the first time and stays (steps: text people read). Nothing moves with reduced
 * motion. Returns the cleanup.
 *
 * The undrawn start is set with transitions off: animated, it slowly undrew the lines off-screen, and after a refresh
 * the browser came back before that had finished, so nothing was left to draw. The class that draws is added two
 * frames after the undrawn state has been painted, so there is always a start to animate from.
 */
export function drawOnView(boxes: HTMLElement[], prepare: (box: HTMLElement) => void, { once = false } = {}): () => void {
  if (prefersReducedMotion() || !boxes.length) return () => {};
  for (const box of boxes) {
    box.classList.add('no-anim', 'draw');
    prepare(box);
    void box.getBoundingClientRect();
    box.classList.remove('no-anim');
  }
  const play = (box: Box) => {
    if (box._on) return;
    box._on = true;
    void box.getBoundingClientRect();
    requestAnimationFrame(() => requestAnimationFrame(() => { if (box._on) box.classList.add('drawn'); }));
  };
  const reset = (box: Box) => {
    box._on = false;
    box.classList.add('no-anim');
    box.classList.remove('drawn');
    void box.getBoundingClientRect();
    box.classList.remove('no-anim');
  };
  const observer = new IntersectionObserver(entries => {
    for (const entry of entries) {
      if (entry.intersectionRatio >= .35) {
        play(entry.target as Box);
        if (once) observer.unobserve(entry.target);
      } else if (!once && !entry.isIntersecting) reset(entry.target as Box);
    }
  }, { threshold: [0, .35] });
  boxes.forEach(box => observer.observe(box));
  // A resize changes the drawing's scale: measure the lines again.
  const remeasure = Object.assign(() => {}, { measure: () => boxes.forEach(prepare) });
  const removeScroller = addScroller(remeasure);
  return () => {
    observer.disconnect();
    removeScroller();
  };
}
