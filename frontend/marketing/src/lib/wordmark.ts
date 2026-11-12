// The logo's data: SOLARIS from the font's outlines, each letter's contours and position, and the sun as its full
// stop (made in frontend/brand/logo).
import wordmark from '../../../brand/logo/wordmark.json';

export type WordmarkData = {
  viewBox: string;
  letters: { x: number; contours: string[] }[];
  dot: { cx: number; cy: number; r: number };
};

export const WORDMARK = wordmark as WordmarkData;

/** The wordmark as a DOM element with its outlines, for the intro (which starts outside React, before it renders). */
export function wordmarkElement(): SVGSVGElement {
  const ns = 'http://www.w3.org/2000/svg';
  const make = <K extends keyof SVGElementTagNameMap>(tag: K, attrs: Record<string, string | number>) => {
    const el = document.createElementNS(ns, tag);
    for (const [name, value] of Object.entries(attrs)) el.setAttribute(name, String(value));
    return el;
  };
  const svg = make('svg', { class: 'wm', viewBox: WORDMARK.viewBox, 'aria-hidden': 'true' });
  WORDMARK.letters.forEach((letter, i) => {
    const g = make('g', { 'data-i': i, transform: `translate(${letter.x} 0)` });
    g.append(make('path', { class: 'wm-fill', d: letter.contours.join('') }));
    for (const d of letter.contours) g.append(make('path', { class: 'wm-line', d }));
    svg.append(g);
  });
  svg.append(make('circle', { class: 'wm-dot', ...WORDMARK.dot }));
  return svg;
}
