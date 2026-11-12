import { useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { Mail, MapPin, Phone } from 'lucide-react';
import { Icon } from '../components/Icon';
import { InfoNotice } from '../components/Notice';
import { CONTACT } from '../content/contact';
import { drawOnView, prepareSteps } from '../lib/draw';

const PROMISES = [
  ['Every step named', 'Six steps, the same for everyone, each explained before it starts.'],
  ['Your time, not ours', 'We contact you in the part of the day you chose on the form.'],
  ['Only numbers we stand behind', 'No invented reviews, no "500 happy homes". If we haven\'t measured it, we don\'t print it.'],
  ['We keep watching', "After switch-on, we compare your system's production with what it should make, every day."],
];

/** About: who Solaris is and where it works, with no invented team. */
export function AboutPage() {
  const promisesRef = useRef<HTMLDivElement>(null);
  useEffect(() => drawOnView([...promisesRef.current!.querySelectorAll<HTMLElement>('.numbered')], prepareSteps, { once: true }), []);
  return (
    <>
      <section className="band page-hero" data-theme="night">
        <div className="wrap">
          <p className="label eyebrow">About</p>
          <h1 className="display">Who we are<i className="dot" /></h1>
          <p className="lead">Solaris Energy sells, installs and looks after solar systems for homes and small businesses across the United States, from its base in Austin, Texas.</p>
        </div>
      </section>
      <section className="band" data-theme="limestone">
        <div className="wrap">
          <div className="intro">
            <div><p className="label eyebrow">How we work</p><h2 className="headline" style={{ fontSize: 'clamp(1.8rem,3.6vw,3.2rem)' }}>Four promises<i className="dot" /></h2></div>
            <p className="body">Things we can keep, so we write them down.</p>
          </div>
          <div className="commit" ref={promisesRef}>
            {PROMISES.map(([title, text], i) => <article className="numbered" key={title}><span className="n">0{i + 1}</span><h4>{title}</h4><p>{text}</p></article>)}
          </div>
        </div>
      </section>
      <section className="band" data-theme="night">
        <div className="wrap">
          <div className="intro">
            <div><p className="label eyebrow">Contact</p><h2 className="headline" style={{ fontSize: 'clamp(1.8rem,3.6vw,3.2rem)' }}>Talk to us<i className="dot" /></h2></div>
            <p className="body">For a quote, the form is quickest. For anything else, call or write.</p>
          </div>
          <div className="whom">
            <div className="contact-card">
              <a href={CONTACT.phoneHref}><Icon icon={Phone} />{CONTACT.phone}</a>
              <a href={`mailto:${CONTACT.email}`}><Icon icon={Mail} />{CONTACT.email}</a>
              <Link to="/about"><Icon icon={MapPin} />{CONTACT.base}</Link>
            </div>
            <InfoNotice title="A fictional company" style={{ alignSelf: 'start' }}>
              Solaris Energy doesn't exist. This site, and the staff app behind it, are a software project. Requests sent through the form are stored and confirmed by email, but nobody follows them up.
            </InfoNotice>
          </div>
        </div>
      </section>
    </>
  );
}
