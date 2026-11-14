import { useLayoutEffect, useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { Icon } from './Icon';

type Props = {
  /** The id of its heading, which names the dialog */
  labelledBy: string;
  /** Escape, the close button, or Cancel: the parent stops rendering the dialog */
  onClose: () => void;
  children: ReactNode;
};

/**
 * A native modal dialog, open while it is rendered: the page behind can't be used, Escape closes it, and closing gives
 * focus back to what opened it (the browser does both). Opening focuses its first field, or its first button (Cancel,
 * where there is one): the close button comes last for that reason. A click outside doesn't close it, so a form being
 * filled in isn't lost to a stray click.
 */
export function Dialog({ labelledBy, onClose, children }: Props) {
  const dialog = useRef<HTMLDialogElement>(null);
  const close = useRef(onClose);
  useLayoutEffect(() => { close.current = onClose; });

  // A layout effect, so the dialog is closed before React removes it, and the browser returns focus.
  useLayoutEffect(() => {
    const d = dialog.current;
    if (!d) return;
    // The browser sends close later, as a task: one from an earlier close() can arrive after the dialog was shown
    // again (React's development mode mounts it twice), and must not close it.
    const onNativeClose = () => { if (!d.open) close.current(); };
    d.addEventListener('close', onNativeClose);
    d.showModal();
    return () => {
      d.removeEventListener('close', onNativeClose);
      if (d.open) d.close();
    };
  }, []);

  return (
    <dialog ref={dialog} className="dialog" aria-labelledby={labelledBy}>
      {children}
      <button className="btn btn-quiet btn-icon btn-sm close" type="button" aria-label="Close" onClick={() => close.current()}>
        <Icon icon={X} />
      </button>
    </dialog>
  );
}
