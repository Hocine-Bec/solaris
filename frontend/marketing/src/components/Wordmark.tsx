import type { Ref } from 'react';
import { WORDMARK } from '../lib/wordmark';

/** The logo: SOLARIS with the sun as its full stop, drawn from the font's outlines (frontend/brand/logo). */
export function Wordmark({ ref }: { ref?: Ref<SVGSVGElement> }) {
  return (
    <svg ref={ref} className="wm" viewBox={WORDMARK.viewBox} aria-hidden="true" focusable="false">
      {WORDMARK.letters.map((letter, i) => (
        <g key={i} data-i={i} transform={`translate(${letter.x} 0)`}>
          <path className="wm-fill" d={letter.contours.join('')} />
        </g>
      ))}
      <circle className="wm-dot" cx={WORDMARK.dot.cx} cy={WORDMARK.dot.cy} r={WORDMARK.dot.r} />
    </svg>
  );
}
