import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { CircleCheck, ClipboardCheck } from 'lucide-react';
import { fetchMyWork, MY_WORK_KEY } from '../api/myWork';
import { Icon } from '../components/Icon';
import { PageHead } from '../components/PageHead';
import { StageChip } from '../customers/Chips';
import { formatDay } from '../lib/dates';
import { cityLine } from '../leads/view';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { Empty, NotAllowed } from '../states/States';
import { QueryView } from '../states/QueryView';
import { RoleChip } from './RoleChip';

/**
 * My work (FR-I6), from the approved design: a technician's whole app, phone first. The installations they're assigned
 * to, open work first: the property, the customer, their role, the stage, or when their part was completed.
 */
export function MyWorkPage() {
  const me = useCurrentUser();
  const query = useQuery({ queryKey: MY_WORK_KEY, queryFn: ({ signal }) => fetchMyWork(signal), enabled: me.role === 'Technician' });

  if (!opensFor(me.role, '/my-work')) return <NotAllowed />;
  return (
    <div className="my-work">
      <PageHead title="My work" />
      <p className="page-intro">Installations you're assigned to, open work first.</p>
      <QueryView
        query={query} what="your work" isEmpty={items => items.length === 0}
        empty={<Empty icon={ClipboardCheck} title="Nothing assigned yet">The office assigns you to installations; they appear here.</Empty>}
      >
        {items => (
          <ul className="record-cards" style={{ display: 'grid', gap: '.75rem' }} aria-label="Your installations, open work first">
            {items.map(w => (
              <li key={w.assignmentId}>
                <Link className="record-card" to={`/my-work/${w.installationId}`}>
                  <b>{w.address.street}</b>
                  <small>{w.customerName} · {cityLine(w.address)}</small>
                  <span className="row">
                    <RoleChip role={w.role} />
                    {w.completedAt
                      ? <span className="chip tone-positive"><Icon icon={CircleCheck} strokeWidth={2} />Completed {formatDay(w.completedAt)}</span>
                      : <StageChip status={w.status} />}
                  </span>
                </Link>
              </li>
            ))}
          </ul>
        )}
      </QueryView>
    </div>
  );
}
