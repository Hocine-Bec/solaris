import { Ban, CircleCheck, CircleDashed, CircleDot, Phone, type LucideIcon } from 'lucide-react';
import type { LeadStatus } from '../api/leads';
import { Icon } from '../components/Icon';

const CHIPS: Record<LeadStatus, { tone: string; icon: LucideIcon }> = {
  New: { tone: 'info', icon: CircleDot },
  Contacted: { tone: 'neutral', icon: Phone },
  Converted: { tone: 'positive', icon: CircleCheck },
  Disqualified: { tone: 'closed', icon: Ban },
  Lost: { tone: 'closed', icon: CircleDashed },
};

/** A lead's status, from the UI kit: New, Contacted, then Converted, Disqualified or Lost */
export function LeadChip({ status }: { status: LeadStatus }) {
  const { tone, icon } = CHIPS[status];
  return <span className={`chip tone-${tone}`}><Icon icon={icon} strokeWidth={2} />{status}</span>;
}
