import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, CircleCheck, ClipboardCheck, Info, KeyRound, LoaderCircle, Pencil, RotateCcw, UserX } from 'lucide-react';
import { fetchUser, reactivateUser, userKey, USERS_KEY, type User } from '../api/users';
import { Icon } from '../components/Icon';
import { FormNotice } from '../components/SubmitButton';
import { formatDay, formatMoment } from '../lib/dates';
import { idFrom } from '../lib/ids';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { NotAllowed, NotFound } from '../states/States';
import { QueryView } from '../states/QueryView';
import { ChangePasswordDialog, DeactivateDialog, EditDialog, SetPasswordDialog } from './AccountDialogs';
import { StatusChip } from './StatusChip';

type Open = 'edit' | 'password' | 'deactivate' | 'change' | null;

const firstName = (name: string) => name.trim().split(/\s+/)[0];

/**
 * One account (FR-U4): its details and what the Admin can do with it. A technician's assignments join this page with
 * the installations. Your own page has Change password and nothing that could lock you out.
 */
export function UserPage() {
  const me = useCurrentUser();
  const id = idFrom(useParams().id);
  const allowed = opensFor(me.role, '/users');
  const query = useQuery({
    queryKey: userKey(id ?? 0),
    queryFn: ({ signal }) => fetchUser(id!, signal),
    enabled: allowed && id !== null,
  });
  const back = { to: '/users', label: 'Back to users' };

  if (!allowed) return <NotAllowed />;
  if (id === null) return <NotFound noun="account" back={back} />;
  return (
    <QueryView query={query} what="this account" notFound={<NotFound noun="account" back={back} />}>
      {user => <Account key={user.id} user={user} isYou={user.id === me.id} />}
    </QueryView>
  );
}

function Account({ user, isYou }: { user: User; isYou: boolean }) {
  const queryClient = useQueryClient();
  const [open, setOpen] = useState<Open>(null);
  const [saved, setSaved] = useState<string | null>(null);
  const [failed, setFailed] = useState<string | null>(null);
  const reactivate = useMutation({ mutationFn: () => reactivateUser(user.id) });
  const close = () => setOpen(null);
  const show = (dialog: Open) => { setSaved(null); setFailed(null); setOpen(dialog); };
  const savedAs = (text: string) => () => { setOpen(null); setSaved(text); };

  const doReactivate = async () => {
    if (reactivate.isPending) return;
    setSaved(null);
    setFailed(null);
    try {
      await reactivate.mutateAsync();
      await queryClient.invalidateQueries({ queryKey: USERS_KEY });
      setSaved('Saved');
    } catch (error) {
      setFailed(error instanceof Error ? error.message : 'Something went wrong in the app. Try again; if it happens again, report it.');
    }
  };

  const actions = isYou ? (
    <button className="btn btn-secondary" type="button" onClick={() => show('change')}><Icon icon={KeyRound} />Change password</button>
  ) : user.isActive ? (
    <>
      <button className="btn btn-secondary" type="button" onClick={() => show('edit')}><Icon icon={Pencil} />Edit</button>
      <button className="btn btn-secondary" type="button" onClick={() => show('password')}><Icon icon={KeyRound} />Set a new password</button>
      <button className="btn btn-danger-outline" type="button" onClick={() => show('deactivate')}><Icon icon={UserX} />Deactivate</button>
    </>
  ) : (
    <button className={`btn btn-secondary${reactivate.isPending ? ' is-loading' : ''}`} type="button" aria-busy={reactivate.isPending || undefined} onClick={() => void doReactivate()}>
      <Icon icon={LoaderCircle} className="spin" />
      {!reactivate.isPending && <Icon icon={RotateCcw} />}
      <span className="btn-text">Reactivate</span>
    </button>
  );

  return (
    <>
      <div className="page-head">
        <Link className="btn btn-quiet btn-sm back" to="/users"><Icon icon={ArrowLeft} />Users</Link>
        <div className="row">
          <h1 className="a-title" id="page-title" tabIndex={-1}>{user.name} <StatusChip isActive={user.isActive} /></h1>
          <div className="page-actions">
            {saved && <span className="saved" role="status"><Icon icon={CircleCheck} />{saved}</span>}
            {actions}
          </div>
        </div>
      </div>

      {failed && <div className="page-notice"><FormNotice>{failed}</FormNotice></div>}
      {!user.isActive && (
        <div className="notice tone-info page-notice">
          <Icon icon={Info} />
          <p>{firstName(user.name)} can't sign in. Their assignments and history stay; reactivating lets them sign in again with their password.</p>
        </div>
      )}

      <div className="acct-card">
        <dl className="kv">
          <dt>Email</dt><dd>{user.email}</dd>
          <dt>Role</dt><dd>{user.role}{isYou && ' · this is you'}</dd>
          <dt>Account created</dt><dd className="num">{formatDay(user.createdAt)}</dd>
          <dt>Last signed in</dt><dd className="num">{user.lastLoginAt ? formatMoment(user.lastLoginAt) : <span className="never">Never</span>}</dd>
        </dl>
      </div>

      {user.role === 'Technician' && (
        <section className="assignments" aria-labelledby="assignments-title">
          <div className="section-title">
            <h2 className="a-heading" id="assignments-title">Assignments</h2>
            <span className="a-label">Their part on each installation</span>
          </div>
          <div className="state">
            <span className="ring"><Icon icon={ClipboardCheck} /></span>
            <p className="a-heading">Not built yet</p>
            <p>The installations {firstName(user.name)} is assigned to, their role on each, and whether their part is done.</p>
          </div>
        </section>
      )}

      {open === 'edit' && <EditDialog user={user} onClose={close} onSaved={savedAs('Saved')} />}
      {open === 'password' && <SetPasswordDialog user={user} onClose={close} onSaved={savedAs('Password set')} />}
      {open === 'deactivate' && <DeactivateDialog user={user} onClose={close} onDone={close} />}
      {open === 'change' && <ChangePasswordDialog onClose={close} />}
    </>
  );
}
