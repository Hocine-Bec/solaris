import { useEffect, useRef, type Ref, type RefObject } from 'react';
import { Link, NavLink, useLocation } from 'react-router-dom';
import { Menu, X } from 'lucide-react';
import { addScroller } from '../lib/motion';
import { Icon } from './Icon';
import { Wordmark } from './Wordmark';

const NAV = [
  ['/how-it-works', 'How it works'],
  ['/questions', 'Questions'],
  ['/about', 'About'],
] as const;

type Props = { logoRef: Ref<SVGSVGElement>; mainRef: RefObject<HTMLElement | null> };

/**
 * The header: the logo, the pages and the quote button. After the first 40 px of scroll it becomes a pill, and its
 * colours follow the section under it. On phones the pages open in a full-screen menu.
 */
export function Header({ logoRef, mainRef }: Props) {
  const headerRef = useRef<HTMLElement>(null);
  const sheetRef = useRef<HTMLDialogElement>(null);
  const { pathname } = useLocation();

  useEffect(() => addScroller(() => {
    const header = headerRef.current;
    if (!header) return;
    header.classList.toggle('is-pill', scrollY > 40);
    const under = document.elementsFromPoint(innerWidth / 2, 36)
      .find(el => el.closest('[data-theme]') && !el.closest('.site-header'))
      ?.closest<HTMLElement>('[data-theme]');
    if (under?.dataset.theme) header.dataset.theme = under.dataset.theme;
  }), []);

  useEffect(() => { if (sheetRef.current?.open) sheetRef.current.close(); }, [pathname]);

  const closeOnLink = (e: React.MouseEvent) => { if ((e.target as Element).closest('a')) sheetRef.current?.close(); };

  return (
    <>
      <a className="btn btn-primary btn-sm skip" href="#main" onClick={e => { e.preventDefault(); mainRef.current?.focus(); }}>Skip to content</a>
      <header className="site-header" data-theme="night" ref={headerRef}>
        <div className="hdr">
          <Link className="logo" to="/" aria-label="Solaris Energy, home"><Wordmark ref={logoRef} /></Link>
          <nav className="label" aria-label="Main">
            {NAV.map(([to, name]) => <NavLink key={to} to={to} end>{name}</NavLink>)}
          </nav>
          <Link className="btn btn-primary btn-sm" to="/quote">Get a quote</Link>
          <button className="btn btn-quiet btn-icon menu-btn" type="button" aria-label="Open the menu" aria-haspopup="dialog" onClick={() => sheetRef.current?.showModal()}>
            <Icon icon={Menu} />
          </button>
        </div>
      </header>
      <dialog className="sheet" data-theme="night" aria-label="Menu" ref={sheetRef} onClick={closeOnLink}>
        <div className="top-row">
          <Link className="logo" to="/">Solaris<i className="dot" /></Link>
          <button className="btn btn-quiet btn-icon" type="button" aria-label="Close the menu" onClick={() => sheetRef.current?.close()}><Icon icon={X} /></button>
        </div>
        <nav aria-label="Main">
          <NavLink to="/" end>Home</NavLink>
          {NAV.map(([to, name]) => <NavLink key={to} to={to} end>{name}</NavLink>)}
        </nav>
        <Link className="btn btn-primary btn-lg" to="/quote">Get a free quote</Link>
      </dialog>
    </>
  );
}
