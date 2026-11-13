// Line drawings that draw themselves, shared by the marketing site and the staff app. The classes that draw them
// (.draw, .drawn, .no-anim) are styled in ../drawings.css.

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
