import { useEffect, useRef } from 'react';
import { Link, NavLink, Outlet, ScrollRestoration, useLocation, useMatches } from 'react-router-dom';
import { Menu, X } from 'lucide-react';
import { Icon } from '../components/Icon';
import { HOME, SECTIONS } from './sections';
import { useTheme } from './theme';
import { ThemeMenu } from './ThemeMenu';

/** What a route tells the shell: the page's title, for the browser's tab */
export type RouteHandle = { title: string };

function Logo() {
  return <Link className="logo" to={HOME}>Solaris<i className="dot" aria-hidden="true" /></Link>;
}

function Nav({ onPick }: { onPick?: () => void }) {
  return (
    <div>
      <p className="nav-label" aria-hidden="true">Office</p>
      <nav className="nav" aria-label="Sections">
        {SECTIONS.map(s => (
          <NavLink key={s.path} to={s.path} onClick={onPick}><Icon icon={s.icon} />{s.label}</NavLink>
        ))}
      </nav>
    </div>
  );
}

/**
 * The frame around every screen: a side bar on a desktop; below 768 px, a top bar whose menu button opens the same
 * navigation in a drawer. After moving to a new screen, focus goes to its title (`#page-title`), so a screen reader
 * announces where you are.
 */
export function Shell() {
  const [theme, chooseTheme] = useTheme();
  const { pathname } = useLocation();
  const matches = useMatches();
  const drawer = useRef<HTMLDialogElement>(null);
  const shown = useRef(pathname);
  const title = (matches.at(-1)?.handle as RouteHandle | undefined)?.title;

  useEffect(() => { document.title = title ? `${title} · Solaris` : 'Solaris'; }, [title]);

  useEffect(() => {
    if (shown.current === pathname) return;   // the first screen keeps the browser's own focus
    shown.current = pathname;
    drawer.current?.close();
    document.getElementById('page-title')?.focus({ preventScroll: true });
  }, [pathname]);

  const closeDrawer = () => drawer.current?.close();

  return (
    <div className="app">
      <a className="skip" href="#main">Skip to the content</a>

      <aside className="side">
        <Logo />
        <Nav />
        <ThemeMenu id="theme-side" choice={theme} onChoose={chooseTheme} />
      </aside>

      <header className="top">
        <button className="btn btn-quiet btn-icon" type="button" aria-label="Open the menu" aria-haspopup="dialog" onClick={() => drawer.current?.showModal()}>
          <Icon icon={Menu} />
        </button>
        <Logo />
        <ThemeMenu id="theme-top" choice={theme} onChoose={chooseTheme} compact />
      </header>

      {/* A click on the backdrop (the dialog itself, outside its panel) closes it, as Escape does. */}
      <dialog ref={drawer} className="drawer" aria-label="Menu" onClick={e => { if (e.target === e.currentTarget) closeDrawer(); }}>
        <div className="drawer-panel">
          <div className="drawer-head">
            <Logo />
            <button className="btn btn-quiet btn-icon" type="button" aria-label="Close the menu" onClick={closeDrawer}><Icon icon={X} /></button>
          </div>
          <Nav onPick={closeDrawer} />
        </div>
      </dialog>

      <main id="main" className="main" tabIndex={-1}>
        <Outlet />
      </main>
      <ScrollRestoration />
    </div>
  );
}
