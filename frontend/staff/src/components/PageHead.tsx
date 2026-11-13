import type { ReactNode } from 'react';

/**
 * The top of every screen: its title, then its actions (at most one of them yellow). The title is where focus goes
 * after moving to the screen, so a page has one PageHead, or a state that carries `#page-title` itself.
 */
export function PageHead({ title, children }: { title: ReactNode; children?: ReactNode }) {
  return (
    <div className="page-head">
      <div className="row">
        <h1 className="a-title" id="page-title" tabIndex={-1}>{title}</h1>
        {children}
      </div>
    </div>
  );
}
