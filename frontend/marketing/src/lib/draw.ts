// Line drawings that draw themselves (What you get, the house in How it works) and steps that animate in (How it
// works, About). The classes they toggle are styled in styles/site.css (.draw, .drawn, .no-anim).
import { addScroller, prefersReducedMotion } from './motion';

const SHAPES = 'polygon, line, ellipse, circle, polyline, path, rect';

type DrawableOptions = { stagger?: number; cap?: number; accExtra?: number; perGroup?: boolean };

/**
 * Prepares an SVG so each of its lines can be drawn from its length. Strokes are set in the drawing's own units, so the
 * dash length matches the line. Lines whose own dash pattern can't be reused fade in instead. With perGroup, the
 * stagger restarts in each layer.
 */
export function drawable(svg: SVGSVGElement, { stagger = .047, cap = 1.5, accExtra = .6, perGroup = false }: DrawableOptions = {}) {
  const viewBox = svg.viewBox.baseVal, box = svg.getBoundingClientRect();
  if (!box.width || !viewBox || !viewBox.width) return;
  const unit = 1 / Math.min(box.width / viewBox.width, box.height / viewBox.height);
  const counters = new Map<Node, number>();
  for (const el of svg.querySelectorAll<SVGGeometryElement>(SHAPES)) {
    if (!el.dataset.sw) el.dataset.sw = String(parseFloat(getComputedStyle(el).strokeWidth) || 1);
    el.style.vectorEffect = 'none';
    el.style.strokeWidth = `${(Number(el.dataset.sw) * unit).toFixed(3)}px`;
    if (el.dataset.d !== undefined || el.dataset.fade !== undefined) continue;
    const key = perGroup ? el.parentNode! : svg, i = counters.get(key) ?? 0;
    counters.set(key, i + 1);
    const extra = el.classList.contains('acc') || el.classList.contains('accline') ? accExtra : 0;
    el.style.setProperty('--dl', `${(Math.min(i * stagger, cap) + extra).toFixed(3)}s`);
    const dash = getComputedStyle(el).strokeDasharray;
    if (dash && dash !== 'none') el.dataset.fade = '';
    else {
      el.style.setProperty('--len', el.getTotalLength().toFixed(1));
      el.dataset.d = '';
    }
  }
}

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
