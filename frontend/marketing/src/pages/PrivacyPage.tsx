import { InfoNotice } from '../components/Notice';
import { CONTACT } from '../content/contact';

export function PrivacyPage() {
  return (
    <>
      <section className="band page-hero" data-theme="night">
        <div className="wrap">
          <p className="label eyebrow">Privacy</p>
          <h1 className="display">Your details<i className="dot" /></h1>
          <p className="lead">What the quote form collects, why, and who sees it.</p>
        </div>
      </section>
      <section className="band" data-theme="limestone">
        <div className="wrap legal">
          <div>
            <h2>What we collect</h2>
            <ul>
              <li>Your name, email address and phone number</li>
              <li>The property's address, its type, and whether you own it</li>
              <li>Your usual monthly electricity bill, the time of day to contact you, and anything you add in the last field</li>
            </ul>
          </div>
          <div><h2>Why</h2><p>To contact you about your request and, if you go ahead, to plan and run your installation. We don't use your details for anything else, and we don't sell them.</p></div>
          <div><h2>Who sees them</h2><p>Solaris staff. The confirmation email is sent through Google's Gmail service. While you type the street address, what you've typed is sent to Geoapify, our address provider, so it can suggest complete addresses.</p></div>
          <div><h2>How long we keep them</h2><p>We keep your request until you ask us to delete it.</p></div>
          <div><h2>Your choices</h2><p>Write to <a className="link" href={`mailto:${CONTACT.email}`}>{CONTACT.email}</a> to correct your details or to have them deleted, and we delete them.</p></div>
          <InfoNotice>Solaris Energy is fictional; this site is a software demo. Requests are stored and confirmed by email, but nobody follows them up.</InfoNotice>
        </div>
      </section>
    </>
  );
}
