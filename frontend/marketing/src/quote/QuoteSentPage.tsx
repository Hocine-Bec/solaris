import { Link, useLocation } from 'react-router-dom';
import { InfoNotice } from '../components/Notice';

/** After a request: what happens next. The email and time come from the form; opened directly, the page says it generally. */
export function QuoteSentPage() {
  const sent = useLocation().state as { email?: string; time?: string } | null;
  return (
    <section className="band sent" data-theme="night" style={{ minHeight: '100svh', paddingTop: 'clamp(8rem,20vh,11rem)' }}>
      <div className="wrap sent-card">
        <p className="label eyebrow">Request sent</p>
        <h1 className="display">We have your request<i className="dot" /></h1>
        <ol className="next-list">
          <li><span className="n">01</span><span>{sent?.email ? `A copy is on its way to ${sent.email}.` : 'A copy is on its way to your inbox.'}</span></li>
          <li><span className="n">02</span><span>{sent?.time ? `We'll contact you in the ${sent.time.toLowerCase()}.` : "We'll contact you at the time of day you chose."}</span></li>
          <li><span className="n">03</span><span>If you want to go ahead, we book a site survey: the first of six steps.</span></li>
        </ol>
        <div className="btn-row">
          <Link className="btn btn-secondary btn-lg" to="/how-it-works">See the six steps</Link>
          <Link className="btn btn-quiet btn-lg" to="/">Back to the home page</Link>
        </div>
        <InfoNotice style={{ marginTop: '2.5rem' }}>Solaris Energy is fictional: nobody will contact you. This page shows what a real visitor would see.</InfoNotice>
      </div>
    </section>
  );
}
