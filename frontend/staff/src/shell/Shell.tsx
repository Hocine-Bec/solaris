import { useEffect, useRef } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Link, NavLink, Outlet, ScrollRestoration, useLocation, useMatches } from 'react-router-dom';
import { Menu, X } from 'lucide-react';
import { signOut } from '../api/auth';
import { Icon } from '../components/Icon';
import { landingFor, REASON_KEY, SESSION_KEY, useCurrentUser } from '../session/session';
import { AccountMenu } from './AccountMenu';
import { sectionsFor, type Section } from './sections';
import { useTheme } from './theme';

/** What a route tells the shell: the page's title, for the browser's tab */
export type RouteHandle = { title: string };

function Logo({ to }: { to: string }) {
  return <Link className="logo" to={to}>Solaris<i className="dot" aria-hidden="true" /></Link>;
}

function Nav({ sections, label, onPick }: { sections: readonly Section[]; label?: string; onPick?: () => void }) {
  return (
    <div>
      {label && <p className="nav-label" aria-hidden="true">{label}</p>}
      <nav className="nav" aria-label="Sections">
        {sections.map(s => (
          <NavLink key={s.path} to={s.path} onClick={onPick}><Icon icon={s.icon} />{s.label}</NavLink>
        ))}
      </nav>
    </div>
  );
}

/**
 * The frame around every screen: a side bar on a desktop; below 768 px, a top bar whose menu button opens the same
 * navigation in a drawer. The navigation follows the role: the office's six sections, or a technician's own work.
 * After moving to a new screen, focus goes to its title (`#page-title`), so a screen reader announces where you are.
 */
export function Shell() {
  const user = useCurrentUser();
  const [theme, chooseTheme] = useTheme();
  const queryClient = useQueryClient();
  const { pathname } = useLocation();
  const matches = useMatches();
  const drawer = useRef<HTMLDialogElement>(null);
  const shown = useRef(pathname);
  const title = (matches.at(-1)?.handle as RouteHandle | undefined)?.title;
  const sections = sectionsFor(user.role);
  const home = landingFor(user.role);
  const navLabel = user.role === 'Admin' ? 'Office' : undefined;

  useEffect(() => { document.title = title ? `${title} · Solaris` : 'Solaris'; }, [title]);

  useEffect(() => {
    if (shown.current === pathname) return;   // the first screen keeps the browser's own focus
    shown.current = pathname;
    drawer.current?.close();
    document.getElementById('page-title')?.focus({ preventScroll: true });
  }, [pathname]);

  const closeDrawer = () => drawer.current?.close();

  // Signing out at once, with no question asked: the API clears the cookie, the reason is noted, and the session is
  // forgotten; the session check then opens the sign-in page, saying "You're signed out." If the API didn't answer,
  // the cookie can't be cleared from here (the page's code can't reach it), so the person stays signed in.
  const signOutNow = async () => {
    try { await signOut(); } catch { return; }
    queryClient.setQueryData(REASON_KEY, 'out');
    queryClient.removeQueries({ predicate: q => q.queryKey[0] !== SESSION_KEY[0] && q.queryKey[0] !== REASON_KEY[0] });
    queryClient.setQueryData(SESSION_KEY, null);
  };

  const account = (id: string, compact?: boolean) => (
    <AccountMenu id={id} user={user} theme={theme} onTheme={chooseTheme} onSignOut={() => void signOutNow()} compact={compact} />
  );

  return (
    <div className="app">
      <a className="skip" href="#main">Skip to the content</a>

      <aside className="side">
        <Logo to={home} />
        <Nav sections={sections} label={navLabel} />
        {account('account-side')}
      </aside>

      <header className="top">
        <button className="btn btn-quiet btn-icon" type="button" aria-label="Open the menu" aria-haspopup="dialog" onClick={() => drawer.current?.showModal()}>
          <Icon icon={Menu} />
        </button>
        <Logo to={home} />
        {account('account-top', true)}
      </header>

      {/* A click on the backdrop (the dialog itself, outside its panel) closes it, as Escape does. */}
      <dialog ref={drawer} className="drawer" aria-label="Menu" onClick={e => { if (e.target === e.currentTarget) closeDrawer(); }}>
        <div className="drawer-panel">
          <div className="drawer-head">
            <Logo to={home} />
            <button className="btn btn-quiet btn-icon" type="button" aria-label="Close the menu" onClick={closeDrawer}><Icon icon={X} /></button>
          </div>
          <Nav sections={sections} label={navLabel} onPick={closeDrawer} />
        </div>
      </dialog>

      <main id="main" className="main" tabIndex={-1}>
        <Outlet />
      </main>
      <ScrollRestoration />
    </div>
  );
}
