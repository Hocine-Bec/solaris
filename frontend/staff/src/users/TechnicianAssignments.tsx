import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { CircleCheck, ClipboardCheck } from 'lucide-react';
import { fetchUserAssignments, userAssignmentsKey } from '../api/myWork';
import { Icon } from '../components/Icon';
import { StageChip } from '../customers/Chips';
import { formatDay } from '../lib/dates';
import { cityLine } from '../leads/view';
import { RoleChip } from '../my-work/RoleChip';
import { QueryView } from '../states/QueryView';

/** A technician's assignments, on their page under Users (FR-U4): each installation, their role, the stage, done or not. */
export function TechnicianAssignments({ userId, name }: { userId: number; name: string }) {
  const query = useQuery({ queryKey: userAssignmentsKey(userId), queryFn: ({ signal }) => fetchUserAssignments(userId, signal) });
  return (
    <section className="assignments" aria-labelledby="assignments-title">
      <div className="section-title">
        <h2 className="a-heading" id="assignments-title">Assignments</h2>
        <span className="a-label">Their part on each installation, open work first</span>
      </div>
      <QueryView
        query={query} what={`${name}'s assignments`} isEmpty={items => items.length === 0}
        empty={(
          <div className="state">
            <span className="ring"><Icon icon={ClipboardCheck} /></span>
            <p className="a-heading">No assignments yet</p>
            <p>Assign {name} from an installation's Assignments tab.</p>
          </div>
        )}
      >
        {items => (
          <>
            <div className="table-wrap list-table">
              <table className="table">
                <caption className="sr">{name}'s assignments, open work first</caption>
                <thead><tr><th scope="col">Installation</th><th scope="col">Role</th><th scope="col">Stage</th><th scope="col">Their part</th></tr></thead>
                <tbody>
                  {items.map((a, n) => (
                    <tr key={n}>
                      <td><span className="who"><Link to={`/installations/${a.installationId}`}>{a.address.street}</Link><small>{a.customerName} · {cityLine(a.address)}</small></span></td>
                      <td><RoleChip role={a.role} /></td>
                      <td><StageChip status={a.status} /></td>
                      <td>{a.completedAt ? <span className="chip tone-positive"><Icon icon={CircleCheck} strokeWidth={2} />Completed {formatDay(a.completedAt)}</span> : <span className="dim">Not yet</span>}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <ul className="record-cards" aria-label={`${name}'s assignments, open work first`}>
              {items.map((a, n) => (
                <li key={n}>
                  <Link className="record-card" to={`/installations/${a.installationId}`}>
                    <b>{a.address.street}</b>
                    <small>{a.customerName}</small>
                    <span className="row"><RoleChip role={a.role} /><StageChip status={a.status} />{a.completedAt && <span className="chip tone-positive"><Icon icon={CircleCheck} strokeWidth={2} />Completed</span>}</span>
                  </Link>
                </li>
              ))}
            </ul>
          </>
        )}
      </QueryView>
    </section>
  );
}
