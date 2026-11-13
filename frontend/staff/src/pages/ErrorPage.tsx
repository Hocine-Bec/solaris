import { CircleAlert, RefreshCw } from 'lucide-react';
import { Icon } from '../components/Icon';

/** A bug while drawing a page. Nothing is logged here: the browser's console already has the error in development. */
export function ErrorPage() {
  return (
    <div className="state-box">
      <div className="state tone-negative" role="alert">
        <span className="ring"><Icon icon={CircleAlert} /></span>
        <p className="a-heading" id="page-title" tabIndex={-1}>Something went wrong on this page</p>
        <p>Reload the page. If it happens again, report it.</p>
        <button className="btn btn-secondary btn-sm" type="button" onClick={() => location.reload()}><Icon icon={RefreshCw} />Reload</button>
      </div>
    </div>
  );
}
