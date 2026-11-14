import { Ban, CircleCheck } from 'lucide-react';
import { Icon } from '../components/Icon';

/** Whether an account can sign in: Active, or Deactivated */
export function StatusChip({ isActive }: { isActive: boolean }) {
  return isActive
    ? <span className="chip tone-positive"><Icon icon={CircleCheck} strokeWidth={2} />Active</span>
    : <span className="chip tone-closed"><Icon icon={Ban} strokeWidth={2} />Deactivated</span>;
}
