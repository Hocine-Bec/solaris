import type { LucideIcon, LucideProps } from 'lucide-react';

type Props = Omit<LucideProps, 'ref'> & { icon: LucideIcon };

/** A Lucide icon at the UI kit's 1.5 line, hidden from screen readers (the text next to it says what it means). */
export function Icon({ icon: Glyph, className, strokeWidth = 1.5, ...props }: Props) {
  return <Glyph className={className ? `ic ${className}` : 'ic'} strokeWidth={strokeWidth} aria-hidden="true" focusable="false" {...props} />;
}
