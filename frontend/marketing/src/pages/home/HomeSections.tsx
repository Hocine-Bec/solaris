import { useEffect, useMemo, useRef } from 'react';
import { Link } from 'react-router-dom';
import { Building2, Check, House } from 'lucide-react';
import { Icon } from '../../components/Icon';
import { QuestionList } from '../../components/QuestionList';
import { QUESTIONS } from '../../content/questions';
import { STEPS } from '../../content/site';
import { DrawingSvg } from '../../drawings/DrawingSvg';
import { figure, house, type FigureName } from '../../drawings/iso';
import { drawable, drawOnView } from '../../lib/draw';
import { addScroller, isWide, prefersReducedMotion, progress } from '../../lib/motion';

const IDEA = "Going solar shouldn't feel like a leap of faith. An installation is a project of six steps, and you see each one before it starts: who comes, what they do, and what happens next.";

/** 01 The idea: its words light up as the paragraph crosses the screen; the hero's panel lands in the dock above it. */
export function Idea() {
  const leadRef = useRef<HTMLParagraphElement>(null);
  useEffect(() => {
    const lead = leadRef.current!, words = [...lead.querySelectorAll<HTMLElement>('.w')];
    return addScroller(() => {
      if (prefersReducedMotion()) { words.forEach(w => { w.style.opacity = '1'; }); return; }
      const r = lead.getBoundingClientRect();
      const p = Math.min(1, Math.max(0, (innerHeight * .78 - r.top) / (innerHeight * .5)));
      words.forEach((w, i) => { w.style.opacity = i / words.length < p ? '1' : '.16'; });
    });
  }, []);
  return (
    <section className="band idea" data-theme="night" id="idea">
      <div className="wrap">
        <div className="dock" id="dock" aria-hidden="true" />
        <p className="label eyebrow">01 — The idea</p>
        <p className="lead" ref={leadRef}>
          {IDEA.split(' ').map((word, i) => <span key={i}><span className="w">{word}</span>{' '}</span>)}
        </p>
        <div className="idea-foot label"><span>Today: quotes, then silence</span><span className="accent-word">Solaris: every step named</span></div>
      </div>
    </section>
  );
}

/**
 * 02 How it works: on wide screens the section holds still for about four screens while the six steps and the house
 * drawing change; the house draws itself when it arrives, then each step draws its own parts. Elsewhere, a list.
 */
export function HowItWorks() {
  const sectionRef = useRef<HTMLElement>(null);
  const houseRef = useRef<HTMLDivElement>(null);
  const listRef = useRef<HTMLOListElement>(null);
  const railRef = useRef<HTMLElement>(null);
  const drawing = useMemo(() => house(), []);

  useEffect(() => {
    const section = sectionRef.current!, box = houseRef.current!, rail = railRef.current!;
    const items = [...listRef.current!.querySelectorAll('li')];
    const removeScroller = addScroller(() => {
      const pinned = isWide() && !prefersReducedMotion();
      section.classList.toggle('pinned', pinned);
      if (!pinned) { box.dataset.step = '5'; items.forEach(li => li.classList.remove('on')); return; }
      const r = section.getBoundingClientRect(), total = section.offsetHeight - innerHeight;
      const p = Math.min(1, Math.max(0, -r.top / total)), step = Math.min(5, Math.floor(p * 6));
      box.dataset.step = String(step);
      items.forEach((li, i) => li.classList.toggle('on', i === step));
      rail.style.width = `${(p * 100).toFixed(1)}%`;
    });
    const stopDrawing = drawOnView([box], b => drawable(b.querySelector('svg')!, { stagger: .09, cap: 1.2, accExtra: .3, perGroup: true }));
    return () => { removeScroller(); stopDrawing(); };
  }, []);

  return (
    <section className="band how-pin" data-theme="limestone" id="how" ref={sectionRef}>
      <div className="how-sticky wrap">
        <div>
          <p className="label eyebrow">02 — How it works</p>
          <h2 className="headline" style={{ fontSize: 'clamp(2rem,4.2vw,4rem)', marginBottom: '2rem' }}>Six steps from survey to switch-on<i className="dot" /></h2>
          <ol className="steps-list" ref={listRef}>
            {STEPS.map((step, i) => <li key={step.name}><span className="n">0{i + 1}</span><b>{step.name}</b><p>{step.short}</p></li>)}
          </ol>
          <div className="rail"><i ref={railRef} /></div>
        </div>
        <div className="house" data-step="5" ref={houseRef}>
          <DrawingSvg drawing={drawing} role="img" aria-label="A house going through the six steps" />
        </div>
      </div>
    </section>
  );
}

const FIGURES: [FigureName, string, string][] = [
  ['panel', 'Panels', "Laid out for your roof's shape, its direction and the shade on it."],
  ['inverter', 'The inverter', "Turns the panels' power into the power your home uses, and reports what it produces."],
  ['papers', 'The paperwork', 'We send the plans to your city or county and to the utility, and wait for their approval.'],
  ['watch', 'Someone watching', "After switch-on, we compare each day's production with what your system should make."],
];

/** 03 What you get: each line drawing draws itself as it scrolls into view, every time. */
export function WhatYouGet() {
  const gridRef = useRef<HTMLDivElement>(null);
  const drawings = useMemo(() => FIGURES.map(([name]) => figure(name)), []);
  useEffect(() => {
    const figs = [...gridRef.current!.querySelectorAll<HTMLElement>('.fig')];
    return drawOnView(figs, f => drawable(f.querySelector('svg')!));
  }, []);
  return (
    <section className="band" data-theme="night" id="get">
      <div className="wrap">
        <div className="intro reveal">
          <div><p className="label eyebrow">03 — What you get</p><h2 className="headline">On the roof, and after it<i className="dot" /></h2></div>
          <p className="body">The system, the paperwork, and someone watching it once it runs.</p>
        </div>
        <div className="figs" ref={gridRef}>
          {FIGURES.map(([name, title, text], i) => (
            <figure className="fig" key={name}>
              <DrawingSvg drawing={drawings[i]} aria-hidden="true" />
              <span className="n">0{i + 1}</span><h3>{title}</h3><p>{text}</p>
            </figure>
          ))}
        </div>
      </div>
    </section>
  );
}

/** 04 Who it's for */
export function WhoItsFor() {
  return (
    <section className="band" data-theme="limestone">
      <div className="wrap">
        <div className="intro reveal">
          <div><p className="label eyebrow">04 — Who it's for</p><h2 className="headline">Homes and small businesses<i className="dot" /></h2></div>
          <p className="body">Anywhere in the United States. The same six steps either way.</p>
        </div>
        <div className="whom reveal">
          <article>
            <h3><Icon icon={House} />Homes</h3>
            <p className="body">Houses and apartments, when you own the property.</p>
            <ul>
              <li><Icon icon={Check} />Renting? The owner can ask for the quote.</li>
              <li><Icon icon={Check} />Your bill tells us roughly how big a system to design.</li>
              <li><Icon icon={Check} />The survey decides whether your roof is a good fit.</li>
            </ul>
          </article>
          <article>
            <h3><Icon icon={Building2} />Small businesses</h3>
            <p className="body">Shops, offices and other premises.</p>
            <ul>
              <li><Icon icon={Check} />Your business can ask, whether you own or lease the building.</li>
              <li><Icon icon={Check} />The steps are the same as for a home.</li>
              <li><Icon icon={Check} />One request covers one address.</li>
            </ul>
          </article>
        </div>
      </div>
    </section>
  );
}

/** The statement: held for about one screen while a yellow line strikes the first sentence and the second replaces it. */
export function Statement() {
  const sectionRef = useRef<HTMLElement>(null);
  const stageRef = useRef<HTMLDivElement>(null);
  useEffect(() => {
    const section = sectionRef.current!, stage = stageRef.current!;
    const set = (name: string, value: string) => stage.style.setProperty(name, value);
    return addScroller(() => {
      const pinned = isWide() && !prefersReducedMotion();
      section.classList.toggle('pinned', pinned);
      if (!pinned) { set('--strike', '100%'); set('--s1o', '1'); set('--s1y', '0px'); set('--s2o', '1'); set('--s2y', '0px'); return; }
      const r = section.getBoundingClientRect(), p = Math.min(1, Math.max(0, -r.top / (section.offsetHeight - innerHeight)));
      set('--strike', `${(progress(p, .05, .4) * 100).toFixed(1)}%`);
      set('--s1o', (1 - progress(p, .5, .65) * .65).toFixed(3));
      set('--s1y', `${(-progress(p, .5, .65) * 20).toFixed(1)}px`);
      set('--s2o', progress(p, .55, .75).toFixed(3));
      set('--s2y', `${((1 - progress(p, .55, .75)) * 60).toFixed(1)}px`);
    });
  }, []);
  return (
    <section className="band statement-pin" data-theme="night" id="statement" ref={sectionRef}>
      <div className="statement wrap" ref={stageRef} role="group" aria-label="Solar is complicated. Solar, step by step.">
        <p className="s1" aria-hidden="true">Solar is complicated.</p>
        <p className="s2" aria-hidden="true">Solar, <em className="serif">step by step</em><i className="dot" /></p>
      </div>
    </section>
  );
}

/** 05 The questions asked most often */
export function TopQuestions() {
  return (
    <section className="band" data-theme="limestone">
      <div className="wrap">
        <div className="intro reveal">
          <div><p className="label eyebrow">05 — Questions</p><h2 className="headline">Asked most often<i className="dot" /></h2></div>
          <p className="body"><Link className="link" to="/questions">All questions</Link></p>
        </div>
        <QuestionList className="reveal" questions={QUESTIONS.filter(q => q.top)} />
      </div>
    </section>
  );
}

/** 06 Get started */
export function GetStarted() {
  return (
    <section className="band cta" data-theme="night">
      <div className="wrap">
        <p className="label eyebrow">06 — Get started</p>
        <h2 className="display">Get a free quote<i className="dot" /></h2>
        <div className="cta-row">
          <p>Tell us about your property in three short steps. We'll contact you at the time you choose.</p>
          <div className="btn-row stretch">
            <Link className="btn btn-primary btn-lg" to="/quote">Get a free quote</Link>
            <Link className="btn btn-secondary btn-lg" to="/questions">Read the questions</Link>
          </div>
        </div>
      </div>
    </section>
  );
}
