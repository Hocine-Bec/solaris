import { useState, type ReactNode } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Ban, CircleAlert, Globe, Lock, Mail, PowerOff, UserPlus, Zap } from 'lucide-react';
import type { Tagged } from '../api/client';
import { ApiError, isStale } from '../api/errors';
import { ASSIGNMENT_ROLES, type AssignmentRole, type Installation } from '../api/installations';
import { fetchTechnicians, TECHNICIANS_KEY } from '../api/users';
import { Dialog } from '../components/Dialog';
import { Icon } from '../components/Icon';
import { FormNotice, SubmitButton } from '../components/SubmitButton';
import { isRefusal, kw } from './view';

/*
 * The installation's dialogs, from the approved design: switching on, cancelling and deactivating ask first; assigning
 * picks a technician and a role. A refusal because of the installation's state, or because it changed since the page
 * read it, closes the dialog and the page says why; any other failure stays in the dialog.
 */

const TITLE = 'dlg-title';
const UNEXPECTED = 'Something went wrong in the app. Try again; if it happens again, report it.';

type Props = {
  installation: Installation;
  onClose: () => void;
  onDone: (changed: Tagged<Installation>) => void;
  /** Refused by the installation's state, or made on an old version of it: the page says why */
  onRefused: (error: ApiError) => void;
};

function useSend(send: () => Promise<Tagged<Installation>>, { onDone, onRefused }: Pick<Props, 'onDone' | 'onRefused'>) {
  const [notice, setNotice] = useState<string | null>(null);
  const mutation = useMutation({ mutationFn: send });
  const submit = async () => {
    if (mutation.isPending) return;
    setNotice(null);
    try {
      onDone(await mutation.mutateAsync());
    } catch (error) {
      if (isRefusal(error) || isStale(error)) onRefused(error);
      else setNotice(error instanceof ApiError ? error.message : UNEXPECTED);
    }
  };
  return { notice, busy: mutation.isPending, submit };
}

function Frame({ title, children, notice, onClose, onSubmit }: { title: string; children: ReactNode; notice: string | null; onClose: () => void; onSubmit: () => void }) {
  return (
    <Dialog labelledBy={TITLE} onClose={onClose}>
      <form noValidate onSubmit={e => { e.preventDefault(); onSubmit(); }}>
        <p className="a-heading" id={TITLE}>{title}</p>
        {children}
        {notice && <FormNotice>{notice}</FormNotice>}
      </form>
    </Dialog>
  );
}

const Cancel = ({ onClose }: { onClose: () => void }) => <button className="btn btn-quiet" type="button" onClick={onClose}>Cancel</button>;

/** Switching on (FR-I7): it emails the customer and fixes the design and the time zone, so it asks, naming the zone. */
export function SwitchOnDialog({ installation: i, send, ...rest }: Props & { send: () => Promise<Tagged<Installation>> }) {
  const { notice, busy, submit } = useSend(send, rest);
  const design = i.design.systemSizeKw !== null ? `${kw(i.design.systemSizeKw)}, ${i.design.panelCount} panels` : 'not recorded';
  return (
    <Frame title={`Switch on the system at ${i.address.street}?`} notice={notice} onClose={rest.onClose} onSubmit={() => void submit()}>
      <ul className="made">
        <li><Icon icon={Zap} /><span>The installation becomes <b>Active</b>, and {i.customer.name} an active customer.</span></li>
        <li><Icon icon={Mail} /><span>{i.customer.name} gets the switch-on email.</span></li>
        <li><Icon icon={Lock} /><span>The design ({design}) and the time zone are fixed from now on.</span></li>
      </ul>
      <div className="tz">
        <Icon icon={Globe} />
        <span>Its production will be counted in <b>{i.timeZone.name}</b> time (<code>{i.timeZone.id}</code>). If that's wrong, cancel and change it first.</span>
      </div>
      <div className="actions"><Cancel onClose={rest.onClose} /><SubmitButton busy={busy}><Icon icon={Zap} />Switch on</SubmitButton></div>
    </Frame>
  );
}

/** Cancelling (BR-I2): final, with a reason, which the history keeps. */
export function CancelDialog({ installation: i, send, ...rest }: Props & { send: (reason: string) => Promise<Tagged<Installation>> }) {
  const [reason, setReason] = useState('');
  const [missing, setMissing] = useState(false);
  const { notice, busy, submit } = useSend(() => send(reason.trim()), rest);
  const onSubmit = () => {
    if (!reason.trim()) { setMissing(true); document.getElementById('cn-reason')?.focus(); return; }
    void submit();
  };
  return (
    <Frame title={`Cancel the installation at ${i.address.street}?`} notice={notice} onClose={rest.onClose} onSubmit={onSubmit}>
      <p>It stops at {i.status} for good: no more moves, assignments or design changes. {i.customer.name}'s status follows. This can't be undone.</p>
      <div className={`field${missing ? ' has-error' : ''}`}>
        <label className="field-label" htmlFor="cn-reason">Reason</label>
        <div className="control control-area">
          <textarea id="cn-reason" rows={3} maxLength={500} value={reason} onChange={e => { setReason(e.target.value); setMissing(false); }}
            aria-invalid={missing || undefined} aria-describedby={missing ? 'cn-reason-err' : 'cn-reason-help'} />
        </div>
        {missing
          ? <p className="field-error" id="cn-reason-err"><Icon icon={CircleAlert} /><span>Give a reason: it stays in the history.</span></p>
          : <p className="field-help" id="cn-reason-help">Kept in the history. Up to 500 characters.</p>}
      </div>
      <div className="actions"><Cancel onClose={rest.onClose} /><SubmitButton busy={busy} danger><Icon icon={Ban} />Cancel the installation</SubmitButton></div>
    </Frame>
  );
}

/** Deactivating (BR-I2): for a system removed or decommissioned; final. */
export function DeactivateDialog({ installation: i, send, ...rest }: Props & { send: (note: string | null) => Promise<Tagged<Installation>> }) {
  const [note, setNote] = useState('');
  const { notice, busy, submit } = useSend(() => send(note.trim() || null), rest);
  return (
    <Frame title={`Deactivate the system at ${i.address.street}?`} notice={notice} onClose={rest.onClose} onSubmit={() => void submit()}>
      <p>For a system removed or decommissioned. It stops being monitored, for good; its history stays. {i.customer.name}'s status follows. This can't be undone.</p>
      <div className="field">
        <label className="field-label" htmlFor="dv-note">Note <span className="field-help">Optional</span></label>
        <div className="control control-area"><textarea id="dv-note" rows={2} maxLength={500} value={note} onChange={e => setNote(e.target.value)} /></div>
      </div>
      <div className="actions"><Cancel onClose={rest.onClose} /><SubmitButton busy={busy} danger><Icon icon={PowerOff} />Deactivate</SubmitButton></div>
    </Frame>
  );
}

/** The role a stage asks for: an installer at Permits and Installation, an inspector at Inspection, else a surveyor. */
const roleFor = (status: Installation['status']): AssignmentRole =>
  status === 'Permits' || status === 'Installation' ? 'Installer' : status === 'Inspection' ? 'Inspector' : 'Surveyor';

/** Assigning a technician (FR-I5): an active technician, in a role. */
export function AssignDialog({ installation: i, send, ...rest }: Props & { send: (technicianId: number, role: AssignmentRole) => Promise<Tagged<Installation>> }) {
  const technicians = useQuery({ queryKey: TECHNICIANS_KEY, queryFn: ({ signal }) => fetchTechnicians(signal) });
  const [technicianId, setTechnicianId] = useState<number | null>(null);
  const [role, setRole] = useState<AssignmentRole>(roleFor(i.status));
  const chosen = technicianId ?? technicians.data?.[0]?.id ?? null;
  const { notice, busy, submit } = useSend(() => send(chosen!, role), rest);
  const none = technicians.data?.length === 0;
  return (
    <Frame title="Assign a technician" notice={notice ?? (technicians.isError ? "We couldn't load the technicians. Close this and try again." : null)} onClose={rest.onClose} onSubmit={() => { if (chosen !== null) void submit(); }}>
      <p>To {i.address.street}. They see it in My work, with the customer's name, phone and address.</p>
      {none ? <p className="dim-note">There is no active technician yet: create one under Users.</p> : (
        <>
          <div className="field">
            <label className="field-label" htmlFor="as-tech">Technician</label>
            <div className="control">
              <select id="as-tech" value={chosen ?? ''} onChange={e => setTechnicianId(Number(e.target.value))} aria-busy={!technicians.data || undefined}>
                {technicians.data?.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}
              </select>
            </div>
          </div>
          <div className="field">
            <label className="field-label" htmlFor="as-role">Role</label>
            <div className="control">
              <select id="as-role" value={role} onChange={e => setRole(e.target.value as AssignmentRole)}>
                {ASSIGNMENT_ROLES.map(r => <option key={r} value={r}>{r}</option>)}
              </select>
            </div>
          </div>
        </>
      )}
      <div className="actions"><Cancel onClose={rest.onClose} />{!none && <SubmitButton busy={busy}><Icon icon={UserPlus} />Assign</SubmitButton>}</div>
    </Frame>
  );
}
