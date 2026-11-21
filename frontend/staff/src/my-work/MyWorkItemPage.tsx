import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, Check, CircleCheck, TriangleAlert } from 'lucide-react';
import type { Tagged } from '../api/client';
import { ApiError, isStale } from '../api/errors';
import { completeMine, fetchMyInstallation, MY_WORK_KEY, myInstallationKey, type MyInstallation } from '../api/myWork';
import { Icon } from '../components/Icon';
import { FormNotice } from '../components/SubmitButton';
import { StageChip } from '../customers/Chips';
import { PART, isRefusal } from '../installations/view';
import { formatMoment } from '../lib/dates';
import { idFrom } from '../lib/ids';
import { cityLine } from '../leads/view';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { ConflictNotice, NotAllowed, NotFound } from '../states/States';
import { QueryView } from '../states/QueryView';

const UNEXPECTED = 'Something went wrong in the app. Try again; if it happens again, report it.';

/**
 * One installation as its technician sees it (FR-I6): the property, the customer to call, their part and what it
 * means, and a 48 px button to mark it complete, at its stage; otherwise, why not yet.
 */
export function MyWorkItemPage() {
  const me = useCurrentUser();
  const id = idFrom(useParams().id);
  const allowed = opensFor(me.role, '/my-work');
  const query = useQuery({
    queryKey: myInstallationKey(id ?? 0),
    queryFn: ({ signal }) => fetchMyInstallation(id!, signal),
    enabled: allowed && id !== null,
  });
  const back = { to: '/my-work', label: 'Back to My work' };

  if (!allowed) return <NotAllowed />;
  if (id === null) return <NotFound noun="installation" back={back} />;
  return (
    <QueryView query={query} what="this installation" notFound={<NotFound noun="installation" back={back} />}>
      {tagged => <Part key={tagged.data.installationId} tagged={tagged} />}
    </QueryView>
  );
}

function Part({ tagged }: { tagged: Tagged<MyInstallation> }) {
  const w = tagged.data;
  const queryClient = useQueryClient();
  const [notice, setNotice] = useState<{ tone: 'warning' | 'negative'; text: string } | null>(null);
  const [stale, setStale] = useState(false);
  const [saved, setSaved] = useState(false);
  const complete = useMutation({ mutationFn: (assignmentId: number) => completeMine(w.installationId, assignmentId, tagged.etag) });

  const doComplete = async (assignmentId: number) => {
    if (complete.isPending) return;
    setNotice(null); setStale(false); setSaved(false);
    try {
      queryClient.setQueryData(myInstallationKey(w.installationId), await complete.mutateAsync(assignmentId));
      void queryClient.invalidateQueries({ queryKey: MY_WORK_KEY, exact: true });
      setSaved(true);
      document.getElementById('page-title')?.focus();
    } catch (error) {
      if (isStale(error)) setStale(true);
      else if (isRefusal(error)) {
        setNotice({ tone: 'warning', text: error.message });
        void queryClient.invalidateQueries({ queryKey: myInstallationKey(w.installationId) });
      } else setNotice({ tone: 'negative', text: error instanceof ApiError ? error.message : UNEXPECTED });
    }
  };

  return (
    <div className="my-work">
      <div className="page-head">
        <Link className="btn btn-quiet btn-sm back" to="/my-work"><Icon icon={ArrowLeft} />My work</Link>
        <h1 className="a-title" id="page-title" tabIndex={-1}>{w.address.street}</h1>
        <p className="sub-line">{cityLine(w.address)}</p>
      </div>
      {stale && <ConflictNotice noun="installation" onReload={() => { setStale(false); void queryClient.invalidateQueries({ queryKey: myInstallationKey(w.installationId) }); }} />}
      {notice?.tone === 'warning' && <div className="notice tone-warning" role="alert"><Icon icon={TriangleAlert} /><p>{notice.text}</p></div>}
      {notice?.tone === 'negative' && <FormNotice>{notice.text}</FormNotice>}
      {saved && <span className="saved" role="status"><Icon icon={CircleCheck} />Marked complete</span>}

      <div className="rec-card">
        <dl className="kv">
          <dt>Customer</dt><dd>{w.customer.name}</dd>
          <dt>Phone</dt><dd><a href={`tel:${w.customer.phone.replace(/[^\d+]/g, '')}`}>{w.customer.phone}</a></dd>
          <dt>Stage</dt><dd><StageChip status={w.status} /></dd>
        </dl>
      </div>

      {w.myAssignments.map(a => (
        <section key={a.id} className="part-box" aria-labelledby={`part-${a.id}`}>
          <b id={`part-${a.id}`}>Your part: {a.role}</b>
          <p>{PART[a.role].does}</p>
          {/* The others in the same role: their parts make the stage's together */}
          {w.team.some(t => t.role === a.role) && (
            <p>{w.team.filter(t => t.role === a.role).map(t => `${t.name} ${t.completedAt ? 'has completed their part' : "hasn't completed their part yet"}`).join('; ')}.</p>
          )}
          <p>{PART[a.role].then}</p>
          {a.completedAt
            ? <p><span className="chip tone-positive"><Icon icon={CircleCheck} strokeWidth={2} />Completed {formatMoment(a.completedAt)}</span></p>
            : a.canComplete
              ? (
                <button className={`btn btn-primary big-btn${complete.isPending ? ' is-loading' : ''}`} type="button" aria-busy={complete.isPending || undefined} onClick={() => void doComplete(a.id)}>
                  <span className="btn-text"><Icon icon={Check} />Mark my {PART[a.role].noun} complete</span>
                </button>
              )
              : <p className="dim-note">{a.why}</p>}
        </section>
      ))}
    </div>
  );
}
