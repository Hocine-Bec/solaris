import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, CircleAlert, CloudOff, LoaderCircle, RefreshCw, SearchX, type LucideIcon } from 'lucide-react';
import { ApiError, MESSAGES } from '../api/errors';
import { Icon } from '../components/Icon';

/*
 * The states every screen shares, from the approved staff app design. Each screen supplies only its own words: what
 * it loads, its empty message, the noun of its record. Signed out comes with signing in, Not allowed with the
 * roles.
 */

const SKELETON_WIDTHS = [8, 6.5, 7.5, 5.5, 7];

/** Rows in the shape of what is coming; no spinner over the page. */
export function Loading() {
  return (
    <div aria-busy="true">
      <p className="sr" role="status">Loading</p>
      <div className="table-wrap" aria-hidden="true">
        <table className="table">
          <tbody>
            {SKELETON_WIDTHS.map(w => (
              <tr key={w}>
                <td><span className="skel" style={{ width: `${w}rem` }} /></td>
                <td><span className="skel" style={{ width: `${w + 2}rem` }} /></td>
                <td><span className="skel" style={{ width: '4.5rem' }} /></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function StateBox({ icon, tone, heading, headingId, children }: { icon: LucideIcon; tone?: 'negative' | 'warning'; heading: string; headingId?: string; children?: ReactNode }) {
  return (
    <div className="state-box">
      <div className={tone ? `state tone-${tone}` : 'state'}>
        <span className="ring"><Icon icon={icon} /></span>
        <p className="a-heading" id={headingId} tabIndex={headingId ? -1 : undefined}>{heading}</p>
        {children}
      </div>
    </div>
  );
}

/** Says why the list is empty and what fills it. */
export function Empty({ icon, title, children }: { icon: LucideIcon; title: string; children: ReactNode }) {
  return <StateBox icon={icon} heading={title}><p>{children}</p></StateBox>;
}

type FailedProps = {
  /** What the screen loads: "the leads", "this installation" */
  what: string;
  error: unknown;
  /** What the failure didn't touch: "Your filters are kept." */
  note?: string;
  retrying: boolean;
  onRetry: () => void;
};

/** The server or the network failed; trying again keeps the screen's filters. */
export function Failed({ what, error, note, retrying, onRetry }: FailedProps) {
  const message = error instanceof ApiError ? error.message : MESSAGES.unexpected;
  return (
    <StateBox icon={CloudOff} tone="negative" heading={`We couldn't load ${what}`}>
      <p>{message}{note && ` ${note}`}</p>
      <button className={`btn btn-secondary btn-sm${retrying ? ' is-loading' : ''}`} type="button" onClick={retrying ? undefined : onRetry}>
        <Icon icon={LoaderCircle} className="spin" />
        {!retrying && <Icon icon={RefreshCw} />}
        <span className="btn-text">Try again</span>
      </button>
    </StateBox>
  );
}

type NotFoundProps = {
  /** "page", "lead", "installation" */
  noun?: string;
  back: { to: string; label: string };
};

/** A wrong link, or a record that no longer exists. Its heading is the page's title, where focus goes. */
export function NotFound({ noun = 'page', back }: NotFoundProps) {
  return (
    <StateBox icon={SearchX} heading={`This ${noun} doesn't exist`} headingId="page-title">
      <p>The link may be wrong, or the {noun} was removed.</p>
      <Link className="btn btn-secondary btn-sm" to={back.to}><Icon icon={ArrowLeft} />{back.label}</Link>
    </StateBox>
  );
}

/** A 409 on a record (BR-I4): the screen stays as it was, nothing was overwritten, and a reload shows the latest. */
export function ConflictNotice({ noun, onReload }: { noun: string; onReload: () => void }) {
  return (
    <div className="notice tone-negative" role="alert">
      <Icon icon={CircleAlert} />
      <p className="notice-title">Someone else changed this {noun}</p>
      <p>Your change wasn't saved, so nothing was overwritten. Reload to see the latest version, then make your change again.</p>
      <div className="actions">
        <button className="btn btn-secondary btn-sm" type="button" onClick={onReload}><Icon icon={RefreshCw} />Reload</button>
      </div>
    </div>
  );
}
