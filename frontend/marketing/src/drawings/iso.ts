// Isometric line drawings, built from coordinates (as on the brand page): the house in How it works and the four
// figures in What you get. Pure functions: they return the shapes, and a component renders them.

type Point3 = [x: number, y: number, z?: number];

export type Shape = { tag: 'polygon' | 'line' | 'circle' | 'ellipse' | 'polyline'; props: Record<string, string | number | object> };
/** Shapes without a className sit directly in the SVG; a layer with one is a group the CSS can show or draw. */
export type Layer = { className?: string; shapes: Shape[] };
export type Drawing = { viewBox: string; layers: Layer[] };

const f = (n: number) => n.toFixed(1);

function iso() {
  const points: [number, number][] = [];
  const layers: Layer[] = [{ shapes: [] }];
  const project = ([x, y, z = 0]: Point3): [number, number] => {
    const q: [number, number] = [(x - y) * .866, (x + y) * .5 - z];
    points.push(q);
    return q;
  };
  const add = (tag: Shape['tag'], props: Shape['props']) => layers[layers.length - 1].shapes.push({ tag, props });
  const api = {
    /** Starts a layer; with no class, shapes go back to the drawing itself. */
    group(className?: string) {
      layers.push({ className, shapes: [] });
      return api;
    },
    poly(corners: Point3[], className: string) {
      add('polygon', { points: corners.map(c => project(c).map(f).join(',')).join(' '), className });
    },
    line(a: Point3, b: Point3, className: string) {
      const [ax, ay] = project(a), [bx, by] = project(b);
      add('line', { x1: f(ax), y1: f(ay), x2: f(bx), y2: f(by), className });
    },
    at: (p: Point3) => project(p),
    raw: add,
    box(x0: number, y0: number, z0: number, x1: number, y1: number, z1: number, className: string) {
      api.poly([[x1, y0, z0], [x1, y1, z0], [x1, y1, z1], [x1, y0, z1]], className);
      api.poly([[x0, y1, z0], [x1, y1, z0], [x1, y1, z1], [x0, y1, z1]], className);
      api.poly([[x0, y0, z1], [x1, y0, z1], [x1, y1, z1], [x0, y1, z1]], className);
    },
    grid(x0: number, y0: number, x1: number, y1: number, z: number, nx: number, ny: number, className: string) {
      for (let i = 1; i < nx; i++) { const x = x0 + (x1 - x0) * i / nx; api.line([x, y0, z], [x, y1, z], className); }
      for (let j = 1; j < ny; j++) { const y = y0 + (y1 - y0) * j / ny; api.line([x0, y, z], [x1, y, z], className); }
    },
    /** The drawing, with a view box around everything drawn */
    done(pad = 10): Drawing {
      const xs = points.map(q => q[0]), ys = points.map(q => q[1]);
      const x0 = Math.min(...xs) - pad, y0 = Math.min(...ys) - pad;
      const viewBox = `${f(x0)} ${f(y0)} ${f(Math.max(...xs) - x0 + pad)} ${f(Math.max(...ys) - y0 + pad)}`;
      return { viewBox, layers: layers.filter(layer => layer.shapes.length) };
    },
  };
  return api;
}

/** The house, with one layer per step: survey, design, permits, panels, inspection, switch-on. */
export function house(): Drawing {
  const d = iso();
  const L = 120, W = 90, H = 52, R = 30, o = 5;
  const slope = (x: number, t: number): Point3 => [x, W / 2 + t * (W / 2 + o), H + R - t * (R + o * R / (W / 2)) + .01];
  const slots: Point3[][] = [];
  for (let r = 0; r < 2; r++) for (let c = 0; c < 5; c++) {
    const x0 = 13 + c * 19.2, x1 = x0 + 17, t0 = .1 + r * .41, t1 = t0 + .37;
    slots.push([slope(x0, t0), slope(x1, t0), slope(x1, t1), slope(x0, t1)].map(([x, y, z = 0]) => [x, y, z + 1.4]));
  }
  d.poly([[-6, -6, 0], [L + 6, -6, 0], [L + 6, W + 6, 0], [-6, W + 6, 0]], 'dash');
  d.poly([[-o, -o, H - o * R / (W / 2)], [L + o, -o, H - o * R / (W / 2)], [L + o, W / 2, H + R], [-o, W / 2, H + R]], 'f');
  d.poly([[0, W, 0], [L, W, 0], [L, W, H], [0, W, H]], 'f');
  d.poly([[L, 0, 0], [L, W, 0], [L, W, H], [L, W / 2, H + R], [L, 0, H]], 'f');
  d.poly([[46, W, 0], [62, W, 0], [62, W, 30], [46, W, 30]], 'l2');
  d.poly([[78, W, 20], [102, W, 20], [102, W, 36], [78, W, 36]], 'l2');
  d.poly([[14, W, 20], [32, W, 20], [32, W, 36], [14, W, 36]], 'l2');
  d.poly([[L, 30, 20], [L, 60, 20], [L, 60, 36], [L, 30, 36]], 'l2');
  d.poly([slope(-o, 0), slope(L + o, 0), slope(L + o, 1), slope(-o, 1)], 'f2');
  // 1 Survey: the roof outlined, and a magnifier
  d.group('L L-survey');
  d.poly([slope(-o - 3, -.04), slope(L + o + 3, -.04), slope(L + o + 3, 1.04), slope(-o - 3, 1.04)], 'dash-acc');
  {
    const [cx, cy] = d.at([L * .55, W * .72, H + R * .5]);
    d.raw('circle', { cx, cy: cy - 40, r: 15, className: 'accline' });
    d.raw('line', { x1: cx + 10.6, y1: cy - 29.4, x2: cx + 22, y2: cy - 18, className: 'accline' });
  }
  // 2 Design: the panel slots, dashed
  d.group('L L-design');
  for (const q of slots) d.poly(q, 'dash-acc');
  // 3 Permits: a sheet on the ground, stamped
  d.group('L L-permits');
  d.poly([[L + 12, 8, 0], [L + 12, 34, 0], [L + 12, 34, 36], [L + 12, 8, 36]], 'f2');
  for (const z of [28, 23, 18]) d.line([L + 12, 12, z], [L + 12, 30, z], 'l3');
  { const [cx, cy] = d.at([L + 12, 27, 9]); d.raw('ellipse', { cx, cy, rx: 6, ry: 6, className: 'accline' }); }
  // 4 Installation: the panels
  d.group('L L-panels');
  const mid = (a: Point3, b: Point3): Point3 => [(a[0] + b[0]) / 2, (a[1] + b[1]) / 2, ((a[2] ?? 0) + (b[2] ?? 0)) / 2];
  for (const q of slots) { d.poly(q, 'f'); d.line(mid(q[0], q[1]), mid(q[3], q[2]), 'l3'); }
  // 5 Inspection: a check
  d.group('L L-inspect');
  {
    const [cx, cy] = d.at([L * .2, W * .2, H + R + 4]);
    d.raw('circle', { cx, cy, r: 11, className: 'acc' });
    d.raw('polyline', { points: `${cx - 5},${cy} ${cx - 1.5},${cy + 3.5} ${cx + 5},${cy - 3.5}`, fill: 'none', strokeWidth: 2, style: { stroke: 'var(--on-accent)' } });
  }
  // 6 Switch-on: the panels lit, the sun up
  d.group('L L-on');
  for (const q of slots) d.poly(q, 'acc');
  { const [cx, cy] = d.at([-26, 52, 74]); d.raw('circle', { cx, cy, r: 13, className: 'acc' }); }
  return d.done(4);
}

export type FigureName = 'panel' | 'inverter' | 'papers' | 'watch';

/** What you get: the panels, the inverter, the paperwork, and the production watched */
export function figure(name: FigureName): Drawing {
  const d = iso();
  if (name === 'panel') {
    d.box(-10, 22, 0, 70, 26, 3, 'f'); d.box(-10, 74, 0, 70, 78, 3, 'f'); d.box(0, 0, 5, 60, 100, 8, 'f2');
    d.poly([[40, 20, 8], [50, 20, 8], [50, 30, 8], [40, 30, 8]], 'acc');
    d.grid(0, 0, 60, 100, 8, 6, 10, 'l3');
    d.poly([[0, 0, 8], [60, 0, 8], [60, 100, 8], [0, 100, 8]], 'l2');
  } else if (name === 'inverter') {
    d.box(0, 0, 0, 50, 22, 70, 'f');
    d.poly([[10, 22, 50], [40, 22, 50], [40, 22, 60], [10, 22, 60]], 'acc');
    for (const z of [20, 26, 32, 38]) d.line([8, 22, z], [42, 22, z], 'l3');
    d.line([50, 8, 10], [80, 8, 10], 'l2'); d.line([50, 14, 10], [80, 14, 10], 'l2');
  } else if (name === 'papers') {
    d.box(0, 0, 0, 70, 50, 2, 'f'); d.box(6, 6, 2, 76, 56, 4, 'f'); d.box(12, 12, 4, 82, 62, 6, 'f2');
    for (const y of [22, 30, 38]) d.line([20, y, 6], [70, y, 6], 'l3');
    { const [cx, cy] = d.at([62, 50, 6]); d.raw('ellipse', { cx, cy, rx: 9, ry: 5.2, className: 'accline' }); }
  } else {
    d.box(0, 0, 0, 100, 60, 2, 'f');
    const pts = [[8, 50], [20, 42], [32, 46], [44, 30], [56, 34], [68, 22], [80, 26], [92, 14]];
    for (let i = 1; i < pts.length; i++) d.line([pts[i - 1][0], pts[i - 1][1], 2], [pts[i][0], pts[i][1], 2], 'accline');
    for (let i = 1; i < pts.length; i++) d.line([pts[i - 1][0], pts[i - 1][1] + 6, 2], [pts[i][0], pts[i][1] + 6, 2], 'dash');
    d.grid(0, 0, 100, 60, 2, 5, 3, 'l3');
  }
  return d.done(10);
}
