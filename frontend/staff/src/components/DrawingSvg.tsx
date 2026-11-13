import { createElement, type Ref, type SVGProps } from 'react';
import type { Drawing, Shape } from '../../../brand/drawings/iso';

const render = (shape: Shape, key: number) => createElement(shape.tag, { key, ...shape.props });

type Props = { drawing: Drawing; ref?: Ref<SVGSVGElement> } & Omit<SVGProps<SVGSVGElement>, 'viewBox' | 'ref'>;

/**
 * One of the brand's line drawings as an SVG: unclassed shapes directly inside, each named layer as a group. The
 * marketing site has the same few lines: React can't be imported from the shared brand folder, which has no packages.
 */
export function DrawingSvg({ drawing, ref, ...props }: Props) {
  return (
    <svg ref={ref} viewBox={drawing.viewBox} {...props}>
      {drawing.layers.map((layer, i) =>
        layer.className
          ? <g key={i} className={layer.className}>{layer.shapes.map(render)}</g>
          : layer.shapes.map((shape, j) => render(shape, i * 1000 + j)))}
    </svg>
  );
}
