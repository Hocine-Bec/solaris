import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <section className="band lost" data-theme="night" style={{ minHeight: '100svh', display: 'grid', alignContent: 'center' }}>
      <div className="wrap">
        <p className="label eyebrow">Page not found</p>
        <h1 className="display">404<i className="dot" /></h1>
        <p className="lead" style={{ margin: '2rem 0', maxWidth: '30ch' }}>This page doesn't exist. The link may be wrong, or the page has moved.</p>
        <div className="btn-row">
          <Link className="btn btn-primary btn-lg" to="/">Go to the home page</Link>
          <Link className="btn btn-secondary btn-lg" to="/quote">Get a free quote</Link>
        </div>
      </div>
    </section>
  );
}
