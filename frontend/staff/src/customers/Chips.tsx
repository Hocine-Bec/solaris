import { Ban, CircleCheck, CircleDashed, CircleDot, ClipboardCheck, DraftingCompass, HardHat, PowerOff, ScanSearch, Stamp, Zap, type LucideIcon } from 'lucide-react';
import type { CustomerStatus, InstallationStatus } from '../api/customers';
import { Icon } from '../components/Icon';

const CUSTOMER: Record<CustomerStatus, { tone: string; icon: LucideIcon }> = {
  Prospect: { tone: 'info', icon: CircleDot },
  Active: { tone: 'positive', icon: CircleCheck },
  Inactive: { tone: 'closed', icon: CircleDashed },
};

const STAGE: Record<InstallationStatus, { tone: string; icon: LucideIcon }> = {
  Survey: { tone: 'info', icon: ScanSearch },
  Design: { tone: 'info', icon: DraftingCompass },
  Permits: { tone: 'info', icon: Stamp },
  Installation: { tone: 'info', icon: HardHat },
  Inspection: { tone: 'info', icon: ClipboardCheck },
  Active: { tone: 'positive', icon: Zap },
  Cancelled: { tone: 'closed', icon: Ban },
  Deactivated: { tone: 'closed', icon: PowerOff },
};

/** A customer's status, set by their installations (BR-C1): Prospect, Active or Inactive */
export function CustomerChip({ status }: { status: CustomerStatus }) {
  const { tone, icon } = CUSTOMER[status];
  return <span className={`chip tone-${tone}`}><Icon icon={icon} strokeWidth={2} />{status}</span>;
}

/** An installation's stage, from the UI kit */
export function StageChip({ status }: { status: InstallationStatus }) {
  const { tone, icon } = STAGE[status];
  return <span className={`chip tone-${tone}`}><Icon icon={icon} strokeWidth={2} />{status}</span>;
}
