import { useEffect, useRef } from 'react';
import { drawable } from '../../../brand/drawings/drawable';
import { house } from '../../../brand/drawings/iso';
import { DrawingSvg } from '../components/DrawingSvg';

const HOUSE = house();
const STEPS = ['Survey', 'Design', 'Permits', 'Installation', 'Inspection', 'Switch-on'];

/** The pace of a round, in milliseconds: the house drawing itself, each step, the switched-on house held, the undrawing */
const PACE = { base: 2600, step: 2300, hold: 4200, undraw: 2200 };

/**
 * Beside the sign-in card on large screens: the house from the home page's How it works, drawing itself through the six
 * steps an installation goes through, again and again (about 20 seconds a round). At switch-on the sun rises over the
 * roof, its light reaches the panels, they light up in a wave, and the windows glow: the home on its own power. With
 * reduced motion, the switched-on house, still.
 */
export function StepsDrawing() {
  const box = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const art = box.current!, houseEl = art.querySelector<HTMLElement>('.house')!, svg = houseEl.querySelector('svg')!;
    const items = [...art.querySelectorAll('.steps-row li')];
    const mark = (current: number) => items.forEach((li, i) => (i === current ? li.setAttribute('aria-current', 'step') : li.removeAttribute('aria-current')));

    if (matchMedia('(prefers-reduced-motion: reduce)').matches) {
      houseEl.dataset.step = '5';
      mark(5);
      return;
    }

    let alive = true;
    const timers = new Set<number>();
    const sleep = (ms: number) => new Promise<void>(resolve => { timers.add(window.setTimeout(resolve, ms)); });
    const frames = () => new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve())));

    // Each line measured once, without animating, so the first round has an undrawn start to draw from
    houseEl.classList.add('no-anim', 'draw');
    drawable(svg, { stagger: .09, cap: 1.2, accExtra: .3, perGroup: true });
    void houseEl.getBoundingClientRect();
    houseEl.classList.remove('no-anim');
    // A resize changes the drawing's scale: the strokes are measured again
    const remeasure = () => drawable(svg, { stagger: .09, cap: 1.2, accExtra: .3, perGroup: true });
    addEventListener('resize', remeasure);

    void (async () => {
      while (alive) {
        houseEl.dataset.step = '-1';
        mark(-1);
        await frames();
        if (!alive) return;
        houseEl.classList.add('drawn');
        await sleep(PACE.base);
        for (let i = 0; i < STEPS.length && alive; i++) {
          houseEl.dataset.step = String(i);
          mark(i);
          await sleep(i === STEPS.length - 1 ? PACE.hold : PACE.step);
        }
        houseEl.classList.remove('drawn');
        await sleep(PACE.undraw);
      }
    })();

    return () => {
      alive = false;
      timers.forEach(clearTimeout);
      removeEventListener('resize', remeasure);
    };
  }, []);

  return (
    <div className="signin-art" ref={box} aria-hidden="true">
      <div className="stage">
        <div className="house" data-step="-1"><DrawingSvg drawing={HOUSE} /></div>
      </div>
      <ol className="steps-row">
        {STEPS.map((name, i) => <li key={name}><b>{String(i + 1).padStart(2, '0')}</b>{name}</li>)}
      </ol>
    </div>
  );
}
