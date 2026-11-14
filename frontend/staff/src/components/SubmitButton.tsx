import type { ReactNode } from 'react';
import { CircleAlert, LoaderCircle } from 'lucide-react';
import { Icon } from './Icon';

/** A form's main button: while its request is out, it shows a spinner and ignores presses (one press, one request). */
export function SubmitButton({ busy, danger = false, children }: { busy: boolean; danger?: boolean; children: ReactNode }) {
  return (
    <button className={`btn ${danger ? 'btn-danger' : 'btn-primary'}${busy ? ' is-loading' : ''}`} type="submit" aria-busy={busy || undefined}>
      <Icon icon={LoaderCircle} className="spin" />
      <span className="btn-text">{children}</span>
    </button>
  );
}

/** A problem with the whole form, read out at once */
export function FormNotice({ children }: { children: ReactNode }) {
  return <div className="notice tone-negative" role="alert"><Icon icon={CircleAlert} /><p>{children}</p></div>;
}
