import { useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { introEnded, introFlying } from '../../lib/intro';
import { addScroller, isWideLandscape, prefersReducedMotion } from '../../lib/motion';

const WORDS = ['home', 'shop', 'office', 'roof'];
const SUN_PERIOD = 24000;
const CELLS = 60;
/** The panel's angles in the hero and behind the idea's paragraph: it straightens a little, but keeps a roof's slope */
const REST = { rx: 50, rz: -32 }, LAND = { rx: 40, rz: -26 };

/** The hero: the sun's day along an arc, the title rising after the intro, a rotating word, and the panel. */
export function Hero() {
  const sectionRef = useRef<HTMLElement>(null);
  const titleRef = useRef<HTMLHeadingElement>(null);
  const wordRef = useRef<HTMLElement>(null);
  const pathRef = useRef<SVGPathElement>(null);
  const sunRef = useRef<SVGGElement>(null);
  const stageRef = useRef<HTMLDivElement>(null);
  const slabRef = useRef<HTMLDivElement>(null);

  // The headline rises once the intro has handed over.
  useEffect(() => {
    let alive = true;
    introFlying.then(() => requestAnimationFrame(() => { if (alive) titleRef.current?.classList.add('play'); }));
    return () => { alive = false; };
  }, []);

  // Home, shop, office, roof: every 2.8 s; stays on "home" with reduced motion.
  useEffect(() => {
    if (prefersReducedMotion()) return;
    const word = wordRef.current!;
    let i = 0, swap = 0;
    const timer = setInterval(() => {
      word.classList.add('out');
      swap = window.setTimeout(() => { i = (i + 1) % WORDS.length; word.textContent = WORDS[i]; word.classList.remove('out'); }, 420);
    }, 2800);
    return () => { clearInterval(timer); clearTimeout(swap); word.textContent = WORDS[0]; word.classList.remove('out'); };
  }, []);

  // The sun rises from the left horizon, follows the arc and sets off the right edge, every 24 s; each of the panel's
  // cells lights as the sun passes over it. It runs only while the hero or the next section is on screen.
  useEffect(() => {
    const path = pathRef.current!, sun = sunRef.current!, length = path.getTotalLength();
    const cells = [...slabRef.current!.querySelectorAll<HTMLElement>('.cells i')]
      .map((el, i) => [el, (i % 6) / 5 * .55 + Math.floor(i / 6) / 9 * .45] as const);
    const place = (t: number) => {
      const pt = path.getPointAtLength(t * length), up = Math.sin(Math.PI * t);
      sun.setAttribute('transform', `translate(${pt.x.toFixed(1)} ${pt.y.toFixed(1)})`);
      for (const [el, at] of cells) el.style.setProperty('--lit', (Math.max(0, 1 - Math.abs(at - t) * 4.5) * up ** .7).toFixed(3));
    };
    place(.36);
    if (prefersReducedMotion()) return;
    const t0 = performance.now() - .36 * SUN_PERIOD;
    let raf = 0, alive = true;
    const loop = (now: number) => { place(((now - t0) % SUN_PERIOD) / SUN_PERIOD); raf = requestAnimationFrame(loop); };
    const onScreen = new Set<Element>();
    const observer = new IntersectionObserver(entries => {
      for (const e of entries) { if (e.isIntersecting) onScreen.add(e.target); else onScreen.delete(e.target); }
      if (onScreen.size && !raf) raf = requestAnimationFrame(loop);
      if (!onScreen.size && raf) { cancelAnimationFrame(raf); raf = 0; }
    });
    introEnded.then(() => {
      if (!alive) return;
      observer.observe(sectionRef.current!);
      const idea = document.getElementById('idea');
      if (idea) observer.observe(idea);
    });
    return () => { alive = false; observer.disconnect(); cancelAnimationFrame(raf); };
  }, []);

  // The panel's flight, on wide landscape screens: it tilts with the pointer; as the idea section comes up it grows,
  // keeps its roof angle and dims, then lies behind the paragraph's right side, running off the screen's edge, and rides
  // up with the section. One fixed layer, under the idea's text and masked to the section; the panel's transform comes
  // from the scroll position.
  useEffect(() => {
    const slab = slabRef.current!, stage = stageRef.current!, hero = sectionRef.current!;
    const idea = document.getElementById('idea')!, lead = idea.querySelector<HTMLElement>('.lead')!;
    let layer: HTMLDivElement | null = null, mover: HTMLDivElement | null = null;
    let start = 0, end = 1, restX = 0, restY = 0, landX = 0, landY = 0, land = 1, ideaTop = 0, ideaBottom = 0;
    let tx = 0, ty = 0, gx = 0, gy = 0, raf = 0;
    const active = () => isWideLandscape() && !prefersReducedMotion();
    const angles = (rx: number, rz: number) => {
      slab.style.setProperty('--rx', `${rx.toFixed(2)}deg`);
      slab.style.setProperty('--rz', `${rz.toFixed(2)}deg`);
    };
    const putBack = () => {
      if (!layer) return;
      stage.appendChild(slab); layer.remove(); layer = mover = null; slab.classList.add('floaty');
      for (const v of ['--rx', '--rz', '--tx', '--ty', '--fade']) slab.style.removeProperty(v);
    };
    const measure = () => {
      if (!active()) { putBack(); return; }
      if (!layer || !mover) {
        layer = document.createElement('div');
        layer.className = 'flyer';
        mover = document.createElement('div');
        mover.className = 'mover';
        layer.appendChild(mover);
        document.body.appendChild(layer);
        mover.appendChild(slab);
        slab.classList.remove('floaty');
      }
      const w = slab.offsetWidth, h = slab.offsetHeight;
      // At rest: the centre of the hero's right column (the mover's corner, in page coordinates)
      const sr = stage.getBoundingClientRect();
      restX = sr.left + sr.width / 2 - w / 2;
      restY = sr.top + scrollY + sr.height / 2 - h / 2;
      // Landed: the panel as seen at its landing angle, at scale 1, with the mover in the screen's corner
      mover.style.transform = 'none';
      angles(LAND.rx, LAND.rz);
      slab.style.setProperty('--tx', '0deg');
      slab.style.setProperty('--ty', '0deg');
      const seen = slab.querySelector('.slab-top')!.getBoundingClientRect();
      const cx = seen.left + seen.width / 2, cy = seen.top + seen.height / 2;
      // A little taller than the screen, so it reads as part of a roof rather than an object: its left edge behind the
      // paragraph's last third, the rest running off the right edge; seen through the section, which cuts it at its edges
      land = innerHeight * 1.1 / seen.height;
      const lr = lead.getBoundingClientRect();
      landX = lr.left + lr.width * .62 + seen.width * land / 2 - (w / 2 + (cx - w / 2) * land);
      landY = innerHeight * .58 - (h / 2 + (cy - h / 2) * land);
      ideaTop = idea.getBoundingClientRect().top + scrollY;
      ideaBottom = ideaTop + idea.offsetHeight;
      start = hero.offsetHeight * .14;
      end = Math.max(start + 1, ideaTop - (innerHeight - idea.offsetHeight) / 2);   // it lands with the section centred on screen
    };
    const ease = (t: number) => t < .5 ? 4 * t ** 3 : 1 - (-2 * t + 2) ** 3 / 2;
    const frame = () => {
      if (!layer || !mover) return;
      const p = ease(Math.min(1, Math.max(0, (scrollY - start) / (end - start))));
      const over = Math.max(0, scrollY - end);
      // At rest it scrolls with the hero; it then travels to its landing place and, landed, rides up with the section
      const yRest = restY - scrollY;
      const x = restX + (landX - restX) * p, y = yRest + (landY - yRest) * p - over, s = 1 + (land - 1) * p;
      mover.style.transform = `translate3d(${x.toFixed(1)}px, ${y.toFixed(1)}px, 0) scale(${s.toFixed(3)})`;
      angles(REST.rx + (LAND.rx - REST.rx) * p, REST.rz + (LAND.rz - REST.rz) * p);
      slab.style.setProperty('--fade', (1 - p).toFixed(3));
      const k = 1 - p;
      slab.style.setProperty('--tx', `${(tx * k).toFixed(2)}deg`);
      slab.style.setProperty('--ty', `${(ty * k).toFixed(2)}deg`);
      // Kept to the section: cut at its bottom edge; above its top, faded out over the end of the flight, the last 60 px
      // easing in so the roof emerges from the section's top rather than being sliced
      const top = ideaTop - scrollY, bottom = ideaBottom - scrollY, above = Math.min(1, (1 - p) / .4);
      layer.style.maskImage = `linear-gradient(to bottom, rgb(0 0 0 / ${above.toFixed(3)}) ${top.toFixed(1)}px, #000 ${(top + 60).toFixed(1)}px, #000 ${bottom.toFixed(1)}px, transparent ${bottom.toFixed(1)}px)`;
    };
    const remeasure = () => { measure(); frame(); };
    const scroller = Object.assign(frame, { measure: remeasure });
    const tilt = () => {
      tx += (gx - tx) * .08; ty += (gy - ty) * .08; frame();
      raf = Math.abs(gx - tx) + Math.abs(gy - ty) > .02 ? requestAnimationFrame(tilt) : 0;
    };
    const onMove = (e: PointerEvent) => {
      gx = (e.clientX / innerWidth - .5) * 10; gy = (e.clientY / innerHeight - .5) * -7;
      if (!raf) raf = requestAnimationFrame(tilt);
    };
    if (!prefersReducedMotion()) addEventListener('pointermove', onMove);
    const first = requestAnimationFrame(remeasure);
    let alive = true;
    document.fonts.ready.then(() => { if (alive) remeasure(); });
    const removeScroller = addScroller(scroller);
    return () => {
      alive = false;
      removeScroller();
      removeEventListener('pointermove', onMove);
      cancelAnimationFrame(first); cancelAnimationFrame(raf);
      putBack();
    };
  }, []);

  return (
    <section className="band hero" data-theme="night" ref={sectionRef}>
      <svg className="sunpath" viewBox="0 0 1440 900" preserveAspectRatio="xMidYMid slice" aria-hidden="true">
        <path ref={pathRef} d="M-80 820 C 300 120, 1100 60, 1520 560" />
        <line x1="0" y1="820" x2="1440" y2="820" />
        <g ref={sunRef} transform="translate(920 237)"><circle className="sun-glow" r="46" /><circle className="sun-halo" r="16" /><circle r="7" /></g>
      </svg>
      <div className="hero-grid wrap">
        <div>
          <p className="label eyebrow">Solaris Energy — Solar for homes and small businesses across the US</p>
          <h1 className="display rise" ref={titleRef}>
            <span className="ln"><span>Solar for</span></span>
            <span className="ln"><span>your <em className="serif" ref={wordRef}>home</em><i className="dot" /></span></span>
          </h1>
          <div className="hero-copy">
            <p>We design and install solar systems across the United States. From the site survey to the day your system is switched on, you see every step before it starts.</p>
            <div className="btn-row stretch">
              <Link className="btn btn-primary btn-lg" to="/quote">Get a free quote</Link>
              <Link className="btn btn-secondary btn-lg" to="/how-it-works">See how it works</Link>
            </div>
          </div>
        </div>
        <div className="panel-stage" ref={stageRef} aria-hidden="true">
          <div className="slab floaty" ref={slabRef}>
            <div className="slab-edge b" />
            <div className="slab-edge l" />
            <div className="slab-top"><div className="cells">{Array.from({ length: CELLS }, (_, i) => <i key={i} />)}</div></div>
          </div>
        </div>
      </div>
      <div className="hero-foot wrap label"><span>Survey · Design · Permits · Installation · Inspection · Switch-on</span><span>Scroll</span></div>
    </section>
  );
}
