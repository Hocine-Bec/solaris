import { ClipboardCheck, HardHat, ScanSearch } from 'lucide-react';
import type { AssignmentRole } from '../api/installations';
import { Icon } from '../components/Icon';

const ICONS = { Surveyor: ScanSearch, Installer: HardHat, Inspector: ClipboardCheck };

/** A technician's role on an installation */
export function RoleChip({ role }: { role: AssignmentRole }) {
  return <span className="chip tone-neutral"><Icon icon={ICONS[role]} strokeWidth={2} />{role}</span>;
}
