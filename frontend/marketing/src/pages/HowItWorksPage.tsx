import { useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { Icon } from '../components/Icon';
import { STEPS } from '../content/site';
import { drawOnView, prepareSteps } from '../lib/draw';

const BEFORE = [
  ['You ask for a quote', 'Three short steps on this site. We email you a copy.'],
  ['We contact you', 'At the time of day you chose, to talk about your property.'],
  ['You decide', "If it's not for you, that's the end of it."],
  ['We book the survey', 'If you want to go ahead. The six steps start here.'],
];

/** How it works: each of the six steps in detail. Each step animates in once per page view. */
export function HowItWorksPage() {
  const rootRef = useRef<HTMLDivElement>(null);
  useEffect(() => drawOnView([...rootRef.current!.querySelectorAll<HTMLElement>('.step-row, .before')], prepareSteps, { once: true }), []);
  return (
    <div ref={rootRef}>
      <section className="band page-hero" data-theme="night">
        <div className="wrap">
          <p className="label eyebrow">How it works</p>
          <h1 className="display">Six steps<i className="dot" /></h1>
          <p className="lead">Who comes, what happens, and what you do, from the survey to the day your system is switched on.</p>
        </div>
      </section>
      <section className="band" data-theme="limestone">
        <div className="wrap">
          <div className="intro">
            <div><p className="label eyebrow">Before the steps</p><h2 className="headline" style={{ fontSize: 'clamp(1.8rem,3.6vw,3.2rem)' }}>Your request<i className="dot" /></h2></div>
            <p className="body">Nothing is booked until you say so.</p>
          </div>
          <ol className="before">
            {BEFORE.map(([title, text]) => <li key={title}><b>{title}</b><span>{text}</span></li>)}
          </ol>
          <div style={{ marginTop: 'clamp(3rem,8vh,5rem)' }}>
            {STEPS.map((step, i) => (
              <article className="step-row" key={step.name}>
                <span className="n">0{i + 1}</span>
                <h2><Icon icon={step.icon} />{step.name}</h2>
                <dl>
                  <dt>Who comes</dt><dd>{step.who}</dd>
                  <dt>What happens</dt><dd>{step.what}</dd>
                  <dt>What you do</dt><dd>{step.you}</dd>
                </dl>
              </article>
            ))}
          </div>
        </div>
      </section>
      <section className="band cta" data-theme="night">
        <div className="wrap">
          <h2 className="display" style={{ fontSize: 'clamp(2.4rem,6vw,5.5rem)' }}>Start with step zero<i className="dot" /></h2>
          <div className="cta-row"><p>The quote request takes three short steps.</p><Link className="btn btn-primary btn-lg" to="/quote">Get a free quote</Link></div>
        </div>
      </section>
    </div>
  );
}
