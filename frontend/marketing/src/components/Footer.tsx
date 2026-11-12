import { Link } from 'react-router-dom';
import { CONTACT } from '../content/contact';

export function Footer() {
  return (
    <footer className="footer" data-theme="night">
      <div className="wrap">
        <div className="f-grid">
          <div className="f-brand">
            <Link className="logo" to="/" style={{ fontSize: '1.3rem' }}>Solaris<i className="dot" /></Link>
            <p>Solar for homes and small businesses across the United States. Solar, step by step.</p>
          </div>
          <div>
            <h3>The site</h3>
            <ul>
              <li><Link to="/how-it-works">How it works</Link></li>
              <li><Link to="/questions">Questions</Link></li>
              <li><Link to="/about">About</Link></li>
              <li><Link to="/quote">Get a free quote</Link></li>
            </ul>
          </div>
          <div>
            <h3>Contact</h3>
            <ul>
              <li><a href={CONTACT.phoneHref}>{CONTACT.phone}</a></li>
              <li><a href={`mailto:${CONTACT.email}`}>{CONTACT.email}</a></li>
              <li><Link to="/about">{CONTACT.base}</Link></li>
            </ul>
          </div>
          <div>
            <h3>Legal</h3>
            <ul><li><Link to="/privacy">Privacy</Link></li></ul>
          </div>
        </div>
        <p className="giant" aria-hidden="true">Solaris<i className="dot" /></p>
        <div className="f-note">
          <span>Solaris Energy is a fictional company. This site is a software demo: requests sent through it are not followed up.</span>
          <span>© 2026 Solaris Energy</span>
        </div>
      </div>
    </footer>
  );
}
