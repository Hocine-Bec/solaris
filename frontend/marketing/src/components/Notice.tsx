import type { ReactNode } from 'react';
import { Info } from 'lucide-react';
import { Icon } from './Icon';

/** An information notice: the demo notices on About, the form, the confirmation and Privacy. */
export function InfoNotice({ title, children, className = '', style }: { title?: string; children: ReactNode; className?: string; style?: React.CSSProperties }) {
  return (
    <div className={`notice tone-info ${className}`.trim()} style={style}>
      <Icon icon={Info} />
      {title && <p className="notice-title">{title}</p>}
      <p>{children}</p>
    </div>
  );
}
