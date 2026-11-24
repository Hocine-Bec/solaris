import { useState } from 'react';
import { flushSync } from 'react-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft, ArrowRight, Ban, Check, CircleCheck, ClipboardCheck, Copy, DraftingCompass, HardHat, KeyRound, Lock, Pencil, PowerOff, RefreshCw, ScanSearch, ShieldCheck, Stamp, TriangleAlert, UserPlus, X, Zap, type LucideIcon } from 'lucide-react';
import type { Tagged } from '../api/client';
import { CUSTOMERS_KEY } from '../api/customers';
import { ApiError, isStale } from '../api/errors';
import {
  assignTechnician, cancelInstallation, completeAssignment, deactivateInstallation, fetchInstallation, INSTALLATIONS_KEY,
  installationKey, moveInstallation, recordDesign, removeAssignment, replaceDeviceKey, setTimeZone, type Changed, type Installation,
} from '../api/installations';
import { MY_WORK_KEY } from '../api/myWork';
import { Icon } from '../components/Icon';
import { FormNotice, SubmitButton } from '../components/SubmitButton';
import { TextField } from '../components/TextField';
import { CustomerChip, StageChip } from '../customers/Chips';
import { useDialogForm } from '../forms/useDialogForm';
import { formatDay, formatMoment } from '../lib/dates';
import { idFrom } from '../lib/ids';
import { cityLine } from '../leads/view';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { ConflictNotice, NotAllowed, NotFound } from '../states/States';
import { QueryView } from '../states/QueryView';
import { designSchema } from './forms';
import { AssignDialog, CancelDialog, DeactivateDialog, ReplaceKeyDialog, SwitchOnDialog } from './InstallationDialogs';
import { historyBy, historyLine, isRefusal, kw, STAGES } from './view';

type Tab = 'overview' | 'assignments' | 'history' | 'production';
type Open = 'switchOn' | 'cancel' | 'deactivate' | 'assign' | 'replaceKey' | null;

const UNEXPECTED = 'Something went wrong in the app. Try again; if it happens again, report it.';
const TABS: [Tab, string][] = [['overview', 'Overview'], ['assignments', 'Assignments'], ['history', 'History'], ['production', 'Production']];

/**
 * One installation (FR-I1–I7), from the approved design: its stages and the next move with the API's reason while it
 * waits, its design and time zone (edited in place until it is switched on), its assignments, and its history. Every
 * change sends the version it was made on (`If-Match`): a change someone made meanwhile keeps this page as it is and
 * offers Reload (BR-I4).
 */
export function InstallationPage() {
  const me = useCurrentUser();
  const id = idFrom(useParams().id);
  const allowed = opensFor(me.role, '/installations');
  const query = useQuery({
    queryKey: installationKey(id ?? 0),
    queryFn: ({ signal }) => fetchInstallation(id!, signal),
    enabled: allowed && id !== null,
  });
  const back = { to: '/installations', label: 'Back to installations' };

  if (!allowed) return <NotAllowed />;
  if (id === null) return <NotFound noun="installation" back={back} />;
  return (
    <QueryView query={query} what="this installation" notFound={<NotFound noun="installation" back={back} />}>
      {tagged => <InstallationView key={tagged.data.id} tagged={tagged} />}
    </QueryView>
  );
}

function InstallationView({ tagged }: { tagged: Tagged<Installation> }) {
  const i = tagged.data;
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<Tab>('overview');
  const [open, setOpen] = useState<Open>(null);
  const [editing, setEditing] = useState<'design' | 'zone' | null>(null);
  const [saved, setSaved] = useState<string | null>(null);
  const [refused, setRefused] = useState<string | null>(null);
  const [failed, setFailed] = useState<string | null>(null);
  const [stale, setStale] = useState(false);
  // The device key just made: in this component's state only, so it is gone once the page is left (BR-T2)
  const [shownKey, setShownKey] = useState<{ key: string; replaced: boolean } | null>(null);
  const current = { id: i.id, etag: tagged.etag };

  /**
   * The installation as the API answered it: shown at once; the lists, the customer and My work load again when shown.
   * Only the installation and its version are cached, never a device key in the answer.
   */
  const keep = (changed: Tagged<Installation>) => {
    queryClient.setQueryData<Tagged<Installation>>(installationKey(i.id), { data: changed.data, etag: changed.etag });
    void queryClient.invalidateQueries({ queryKey: [...INSTALLATIONS_KEY, 'page'] });
    void queryClient.invalidateQueries({ queryKey: CUSTOMERS_KEY });
    void queryClient.invalidateQueries({ queryKey: MY_WORK_KEY });
    void queryClient.invalidateQueries({ queryKey: ['users'] });
  };
  const clear = () => { setSaved(null); setRefused(null); setFailed(null); setStale(false); };
  const reload = () => { clear(); void queryClient.invalidateQueries({ queryKey: installationKey(i.id) }); };

  /** Refused because of its state (a condition, a part not at its stage, a move made meanwhile): say why, load it again. */
  const onRefused = (error: ApiError) => {
    setOpen(null);
    setRefused(error.message);
    void queryClient.invalidateQueries({ queryKey: installationKey(i.id) });
    document.getElementById('page-title')?.focus();
  };
  /** A dialog's change refused by the state, or made on an old version: the dialog closes and the page says so. */
  const onDialogRefused = (error: ApiError) => {
    if (!isStale(error)) { onRefused(error); return; }
    flushSync(() => { setOpen(null); setStale(true); });
    document.getElementById('page-title')?.focus();
  };
  const onFailed = (error: unknown) => {
    if (isStale(error)) setStale(true);
    else if (isRefusal(error)) onRefused(error);
    else setFailed(error instanceof ApiError ? error.message : UNEXPECTED);
  };

  /** A change made at once, from a button: Saved beside it, or what went wrong. */
  const change = useMutation({ mutationFn: (call: () => Promise<Tagged<Installation>>) => call() });
  const run = async (call: () => Promise<Tagged<Installation>>, said: string, fallbackFocus: string) => {
    if (change.isPending) return;
    clear();
    try {
      const changed = await change.mutateAsync(call);
      flushSync(() => { keep(changed); setSaved(said); });
      // The button pressed may be gone (Mark complete) or now disabled (the next move waits): focus stays in the page
      const active = document.activeElement as HTMLButtonElement | null;
      if (!active || active === document.body || !active.isConnected || active.disabled) document.getElementById(fallbackFocus)?.focus();
    } catch (error) {
      onFailed(error);
    }
  };
  const done = (said: string, focusTitle = true) => (changed: Tagged<Installation>) => {
    keep(changed);
    setOpen(null);
    setSaved(said);
    if (focusTitle) document.getElementById('page-title')?.focus();
  };
  const show = (dialog: Open) => { clear(); setOpen(dialog); };
  /** Switched on, or its key replaced: the key, once, above the cards; focus goes to it. */
  const keyMade = (said: string, replaced: boolean) => (changed: Changed) => {
    flushSync(() => {
      keep(changed); setOpen(null); setSaved(said); setTab('overview');
      setShownKey(changed.issuedKey ? { key: changed.issuedKey, replaced } : null);
    });
    document.getElementById(changed.issuedKey ? 'key-shown' : 'page-title')?.focus();
  };
  const keyDone = () => {
    flushSync(() => setShownKey(null));
    document.getElementById('page-title')?.focus();
  };

  const onTabKey = (e: React.KeyboardEvent) => {
    const index = TABS.findIndex(([k]) => k === tab);
    const next = e.key === 'ArrowRight' ? (index + 1) % TABS.length : e.key === 'ArrowLeft' ? (index + TABS.length - 1) % TABS.length : -1;
    if (next < 0) return;
    e.preventDefault();
    setTab(TABS[next][0]);
    document.getElementById(`tab-${TABS[next][0]}`)?.focus();
  };

  return (
    <>
      <div className="page-head">
        <Link className="btn btn-quiet btn-sm back" to="/installations"><Icon icon={ArrowLeft} />Installations</Link>
        <div className="row">
          <h1 className="a-title" id="page-title" tabIndex={-1}>{i.address.street} <StageChip status={i.status} /></h1>
          <div className="page-actions">
            {i.canCancel && <button className="btn btn-danger-outline" type="button" onClick={() => show('cancel')}><Icon icon={Ban} />Cancel…</button>}
            {i.canDeactivate && <button className="btn btn-danger-outline" type="button" onClick={() => show('deactivate')}><Icon icon={PowerOff} />Deactivate…</button>}
          </div>
        </div>
        <p className="sub-line">
          <span>{cityLine(i.address)}</span><span aria-hidden="true">·</span>
          <Link to={`/customers/${i.customer.id}`}>{i.customer.name}</Link><CustomerChip status={i.customer.status} />
          <span aria-hidden="true">·</span><Link to={`/leads/${i.leadId}`}>the request</Link>
        </p>
      </div>

      {stale && <div className="page-notice"><ConflictNotice noun="installation" onReload={reload} /></div>}
      {refused && (
        <div className="notice tone-warning page-notice" role="alert">
          <Icon icon={TriangleAlert} />
          <p className="notice-title">{refused}</p>
          <p>The installation has been loaded again, as it is now.</p>
        </div>
      )}
      {failed && <div className="page-notice"><FormNotice>{failed}</FormNotice></div>}

      <div className="tabs" role="tablist" aria-label="Installation" onKeyDown={onTabKey}>
        {TABS.map(([key, label]) => (
          <button
            key={key} id={`tab-${key}`} className="tab" role="tab" type="button" aria-selected={tab === key} aria-controls="tabpanel"
            tabIndex={tab === key ? 0 : -1} onClick={() => setTab(key)}
          >
            {key === 'assignments' ? `${label} (${i.assignments.length})` : label}
          </button>
        ))}
      </div>
      <div className="tabpanel" role="tabpanel" id="tabpanel" aria-labelledby={`tab-${tab}`}>
        {tab === 'overview' && (
          <>
            <Tracker installation={i} />
            <div className="ov-main">
              <NextMove installation={i} saved={saved} busy={change.isPending}
                onMove={to => void run(() => moveInstallation(current, to), 'Saved', 'page-title')} onSwitchOn={() => show('switchOn')} />
              {shownKey && <KeyShown shown={shownKey} street={i.address.street} onDone={keyDone} />}
              <DesignCard installation={i} editing={editing === 'design'} onEdit={on => { clear(); setEditing(on ? 'design' : null); }}
                save={design => recordDesign(current, design)} onSaved={changed => { keep(changed); setEditing(null); setSaved('Saved'); }} onFailed={onFailed} />
              <ZoneCard installation={i} editing={editing === 'zone'} onEdit={on => { clear(); setEditing(on ? 'zone' : null); }}
                save={zone => setTimeZone(current, zone)} onSaved={changed => { keep(changed); setEditing(null); setSaved('Saved'); }} onFailed={onFailed} />
              <DeviceKeyCard installation={i} onReplace={() => show('replaceKey')} />
            </div>
          </>
        )}
        {tab === 'assignments' && (
          <Assignments installation={i} busy={change.isPending} saved={saved}
            onAssign={() => show('assign')}
            onComplete={id => void run(() => completeAssignment(current, id), 'Marked complete', 'tab-assignments')}
            onRemove={id => void run(() => removeAssignment(current, id), 'Removed', 'tab-assignments')} />
        )}
        {tab === 'history' && <History installation={i} />}
        {tab === 'production' && (
          <div className="state-inline">
            {i.status === 'Active' || i.status === 'Deactivated'
              ? 'Each day of this system, actual against expected, and each month\'s totals: built with the production screens.'
              : 'Once the system is switched on, its production appears here.'}
          </div>
        )}
      </div>

      {open === 'switchOn' && <SwitchOnDialog installation={i} send={() => moveInstallation(current, 'Active')} onClose={() => setOpen(null)} onDone={keyMade('Saved', false)} onRefused={onDialogRefused} />}
      {open === 'cancel' && <CancelDialog installation={i} send={reason => cancelInstallation(current, reason)} onClose={() => setOpen(null)} onDone={done('Cancelled')} onRefused={onDialogRefused} />}
      {open === 'deactivate' && <DeactivateDialog installation={i} send={note => deactivateInstallation(current, note)} onClose={() => setOpen(null)} onDone={changed => { setShownKey(null); done('Deactivated')(changed); }} onRefused={onDialogRefused} />}
      {open === 'assign' && <AssignDialog installation={i} send={(t, r) => assignTechnician(current, t, r)} onClose={() => setOpen(null)} onDone={done('Assigned', false)} onRefused={onDialogRefused} />}
      {open === 'replaceKey' && <ReplaceKeyDialog installation={i} send={() => replaceDeviceKey(current)} onClose={() => setOpen(null)} onDone={keyMade('Saved', i.deviceKey !== null)} onRefused={onDialogRefused} />}
    </>
  );
}

const STAGE_ICONS: Record<(typeof STAGES)[number], LucideIcon> = { Survey: ScanSearch, Design: DraftingCompass, Permits: Stamp, Installation: HardHat, Inspection: ClipboardCheck, Active: Zap };

/** The stages, the current one marked; a closed installation shows where it stopped. */
function Tracker({ installation: i }: { installation: Installation }) {
  const closed = i.status === 'Cancelled' || i.status === 'Deactivated';
  const lastOpen = closed ? (i.history.find(h => h.kind === 'Status' && h.to === i.status)?.from ?? 'Survey') : i.status;
  const reached = STAGES.indexOf(lastOpen as (typeof STAGES)[number]);
  const reachedAt = (stage: string) => i.history.find(h => h.kind === 'Status' && h.to === stage)?.at;
  return (
    <ol className={`stages${closed ? ' is-closed' : ''}`} aria-label="Stages">
      {STAGES.map((stage, n) => {
        const isActive = stage === 'Active' && i.status === 'Active';
        const done = n < reached || isActive;
        const current = n === reached && !closed && !isActive;
        const at = reachedAt(stage);
        return (
          <li key={stage} className={done ? 'done' : undefined} aria-current={current ? 'step' : undefined}>
            <Icon icon={done ? Check : STAGE_ICONS[stage]} />
            <span className="st-name">{stage}</span>
            <span className="st-meta">{at && (done || current) ? `${current || isActive ? 'Since' : 'From'} ${formatMoment(at)}` : ''}</span>
          </li>
        );
      })}
    </ol>
  );
}

function NextMove({ installation: i, saved, busy, onMove, onSwitchOn }: {
  installation: Installation; saved: string | null; busy: boolean; onMove: (to: Installation['status']) => void; onSwitchOn: () => void;
}) {
  const closing = i.history.find(h => h.kind === 'Status' && h.to === i.status);
  if (i.status === 'Cancelled' || i.status === 'Deactivated') {
    return (
      <div className="next-move">
        <div className="notice tone-info">
          <Icon icon={Ban} />
          <p className="notice-title">{i.status}{closing && ` ${formatMoment(closing.at)}`}{closing?.by && ` by ${closing.by.name}`}</p>
          {closing?.note && <p>{i.status === 'Cancelled' ? 'Reason' : 'Note'}: {closing.note}</p>}
        </div>
      </div>
    );
  }
  if (!i.next) {
    return (
      <div className="next-move">
        <p className="dim-note">{saved && <span className="saved" role="status"><Icon icon={CircleCheck} />{saved}</span>} Switched on {i.activatedAt ? formatDay(i.activatedAt) : ''}. Its production appears on the Production tab once its readings arrive.</p>
      </div>
    );
  }
  const n = i.next;
  return (
    <div className="next-move">
      <p className="a-label">Next</p>
      <div className="row">
        {n.status === 'Active' ? (
          <button className="btn btn-primary" type="button" disabled={!n.allowed} aria-describedby={n.allowed ? undefined : 'next-why'} onClick={onSwitchOn}>
            <Icon icon={Zap} />Switch on…
          </button>
        ) : (
          <button className={`btn btn-primary${busy ? ' is-loading' : ''}`} type="button" disabled={!n.allowed} aria-busy={busy || undefined}
            aria-describedby={n.allowed ? undefined : 'next-why'} onClick={() => onMove(n.status)}>
            <span className="btn-text"><Icon icon={ArrowRight} />Move to {n.status}</span>
          </button>
        )}
        {saved && <span className="saved" role="status"><Icon icon={CircleCheck} />{saved}</span>}
      </div>
      {!n.allowed && n.reason && (
        <div className="notice tone-info" id="next-why"><Icon icon={TriangleAlert} /><p>Can't move to {n.status} yet. {n.reason}</p></div>
      )}
    </div>
  );
}

type CardProps<T> = {
  installation: Installation;
  editing: boolean;
  onEdit: (on: boolean) => void;
  save: (value: T) => Promise<Tagged<Installation>>;
  onSaved: (changed: Tagged<Installation>) => void;
  onFailed: (error: unknown) => void;
};

function FixedNote() {
  return <span className="fixed-note"><Icon icon={Lock} />Fixed at switch-on</span>;
}

/** The design (FR-I4), recorded in place on its card until the system is switched on. */
function DesignCard({ installation: i, editing, onEdit, save, onSaved, onFailed }: CardProps<{ systemSizeKw: number; panelCount: number; inverterType: string | null }>) {
  const d = i.design;
  const { form, notice, onSubmit } = useDialogForm({
    schema: designSchema,
    defaultValues: { systemSizeKw: d.systemSizeKw?.toString() ?? '', panelCount: d.panelCount?.toString() ?? '', inverterType: d.inverterType ?? '' },
    send: async values => {
      try {
        onSaved(await save({ systemSizeKw: Number(values.systemSizeKw), panelCount: Number(values.panelCount), inverterType: values.inverterType || null }));
      } catch (error) {
        if (isRefusal(error) || isStale(error)) { onFailed(error); return; }
        throw error;
      }
    },
  });
  const { errors, isSubmitting } = form.formState;
  if (editing) {
    return (
      <div className="rec-card">
        <div className="card-head"><p className="a-label">Design</p></div>
        <form className="edit-grid" noValidate onSubmit={onSubmit}>
          {notice && <FormNotice>{notice}</FormNotice>}
          <TextField id="ds-kw" label="System size (kW)" registration={form.register('systemSizeKw')} error={errors.systemSizeKw?.message} />
          <TextField id="ds-panels" label="Panels" registration={form.register('panelCount')} error={errors.panelCount?.message} />
          <TextField id="ds-inverter" label="Inverter" registration={form.register('inverterType')} error={errors.inverterType?.message} />
          <div className="edit-actions">
            <button className="btn btn-quiet" type="button" onClick={() => onEdit(false)}>Cancel</button>
            <SubmitButton busy={isSubmitting}>Save</SubmitButton>
          </div>
        </form>
      </div>
    );
  }
  return (
    <div className="rec-card">
      <div className="card-head">
        <p className="a-label">Design</p>
        {d.fixed ? <FixedNote /> : (
          <button className="btn btn-secondary btn-sm" type="button" onClick={() => onEdit(true)}><Icon icon={Pencil} />{d.systemSizeKw === null ? 'Record' : 'Edit'}</button>
        )}
      </div>
      <dl className="kv">
        <dt>System size</dt><dd>{d.systemSizeKw !== null ? kw(d.systemSizeKw) : <span className="not-set">Not recorded</span>}</dd>
        <dt>Panels</dt><dd>{d.panelCount ?? <span className="not-set">Not recorded</span>}</dd>
        <dt>Inverter</dt><dd>{d.inverterType ?? <span className="not-set">Not recorded</span>}</dd>
      </dl>
    </div>
  );
}

/** The time zone (BR-P4): proposed from the state, changed here until the system is switched on. */
function ZoneCard({ installation: i, editing, onEdit, save, onSaved, onFailed }: CardProps<string>) {
  const [zone, setZone] = useState(i.timeZone.id);
  const mutation = useMutation({ mutationFn: save });
  if (editing) {
    return (
      <div className="rec-card">
        <div className="card-head"><p className="a-label">Time zone</p></div>
        <form className="edit-grid" noValidate onSubmit={e => { e.preventDefault(); mutation.mutateAsync(zone).then(onSaved, onFailed); }}>
          <div className="field">
            <label className="field-label" htmlFor="tz-zone">Its days are counted in</label>
            <div className="control">
              <select id="tz-zone" value={zone} onChange={e => setZone(e.target.value)}>
                {i.timeZones.map(z => <option key={z.id} value={z.id}>{z.name} ({z.id})</option>)}
              </select>
            </div>
          </div>
          <div className="edit-actions">
            <button className="btn btn-quiet" type="button" onClick={() => onEdit(false)}>Cancel</button>
            <SubmitButton busy={mutation.isPending}>Save</SubmitButton>
          </div>
        </form>
      </div>
    );
  }
  return (
    <div className="rec-card">
      <div className="card-head">
        <p className="a-label">Time zone</p>
        {i.timeZone.fixed ? <FixedNote /> : (
          <button className="btn btn-secondary btn-sm" type="button" onClick={() => { setZone(i.timeZone.id); onEdit(true); }}><Icon icon={Pencil} />Change</button>
        )}
      </div>
      <p><b>{i.timeZone.name}</b> <code>{i.timeZone.id}</code></p>
      <p className="dim-note">
        {i.timeZone.fixed ? 'Its readings are counted per local day in this zone.' : `Proposed from ${i.address.state}. You confirm it when you switch the system on.`}
      </p>
    </div>
  );
}

/**
 * The device key, once (BR-T2): just made, at switch-on or by a replacement. Nothing else ever shows it: Solaris keeps
 * only its hash. Copy puts it on the clipboard; where the browser refuses, the key is selected for copying by hand.
 */
function KeyShown({ shown, street, onDone }: { shown: { key: string; replaced: boolean }; street: string; onDone: () => void }) {
  const [copied, setCopied] = useState<'yes' | 'no' | null>(null);
  const copy = async () => {
    try {
      await navigator.clipboard.writeText(shown.key);
      setCopied('yes');
      setTimeout(() => setCopied(c => (c === 'yes' ? null : c)), 2000);
    } catch {
      setCopied('no');
      const range = document.createRange();
      const code = document.getElementById('key-value');
      if (code) { range.selectNodeContents(code); getSelection()?.removeAllRanges(); getSelection()?.addRange(range); }
    }
  };
  return (
    <section className="key-shown" id="key-shown" tabIndex={-1} aria-labelledby="key-shown-title">
      <div className="kp-top">
        <Icon icon={KeyRound} />
        <p>
          <b id="key-shown-title">{shown.replaced ? 'The new device key' : 'Its device key'}, shown only now</b>
          The system at {street} sends its readings with it{shown.replaced && '; the old key no longer works'}. Copy it into the system's monitoring settings.
        </p>
        <button className="btn btn-quiet btn-sm" type="button" onClick={onDone}>Done</button>
      </div>
      <div className="key-row">
        <code className="key-value" id="key-value">{shown.key}</code>
        <button className={`btn btn-secondary${copied === 'yes' ? ' copied' : ''}`} type="button" onClick={() => void copy()}>
          <Icon icon={copied === 'yes' ? Check : Copy} />{copied === 'yes' ? 'Copied' : 'Copy'}
        </button>
      </div>
      <p className="sr-status" role="status">{copied === 'yes' ? 'Copied' : ''}</p>
      {copied === 'no' && <p className="field-error"><Icon icon={TriangleAlert} /><span>This browser didn't allow copying: the key is selected, copy it with the keyboard.</span></p>}
      <p className="after">Once this panel is closed, nobody can show it again, the office included. If it's lost, use Replace key… on the Device key card.</p>
    </section>
  );
}

/** The device key on the overview (BR-T2): how it starts and when it was made; deleted once deactivated. */
function DeviceKeyCard({ installation: i, onReplace }: { installation: Installation; onReplace: () => void }) {
  if (i.status === 'Deactivated') {
    const at = i.history.find(h => h.kind === 'Status' && h.to === 'Deactivated')?.at;
    return (
      <div className="rec-card key-card">
        <div className="card-head"><p className="a-label">Device key</p><span className="fixed-note"><Icon icon={Ban} />Deleted</span></div>
        <p className="revoked">Deleted when the system was deactivated{at && `, ${formatDay(at)}`}. A device still sending it is refused.</p>
      </div>
    );
  }
  if (i.status !== 'Active') return null;
  const key = i.deviceKey;
  return (
    <div className="rec-card key-card">
      <div className="card-head">
        <p className="a-label">Device key</p>
        {key && <span className="fixed-note"><Icon icon={ShieldCheck} />Valid</span>}
      </div>
      {key ? (
        <>
          <dl className="kv"><dt>Starts with</dt><dd><code>{key.prefix}.</code></dd><dt>Made</dt><dd>{formatMoment(key.issuedAt)}</dd></dl>
          <p className="dim-note">The system sends its readings with it. Solaris keeps only a fingerprint: the key itself was shown once.</p>
        </>
      ) : (
        <p className="dim-note">This system has no key yet: it was switched on before device keys existed. Make one for it to send its readings.</p>
      )}
      <div className="key-actions">
        <button className="btn btn-secondary btn-sm" type="button" onClick={onReplace}><Icon icon={RefreshCw} />{key ? 'Replace key…' : 'Make a key…'}</button>
      </div>
    </div>
  );
}

function Assignments({ installation: i, busy, saved, onAssign, onComplete, onRemove }: {
  installation: Installation; busy: boolean; saved: string | null; onAssign: () => void; onComplete: (id: number) => void; onRemove: (id: number) => void;
}) {
  const canAssign = i.status !== 'Active' && i.status !== 'Cancelled' && i.status !== 'Deactivated';
  return (
    <>
      <div className="asg-head">
        <p className="dim-note">Each technician completes their part at its stage: a survey at Survey, an installation at Installation, an inspection at Inspection. You can mark it for them.</p>
        {saved && <span className="saved" role="status"><Icon icon={CircleCheck} />{saved}</span>}
        {canAssign && <button className="btn btn-secondary" type="button" onClick={onAssign}><Icon icon={UserPlus} />Assign a technician</button>}
      </div>
      {i.assignments.length === 0 ? <div className="state-inline">Nobody is assigned yet.</div> : (
        <div className="table-wrap list-table">
          <table className="table asg-table">
            <caption className="sr">Assignments</caption>
            <thead><tr><th scope="col">Technician</th><th scope="col">Assigned</th><th scope="col">Their part</th><th scope="col"><span className="sr">Actions</span></th></tr></thead>
            <tbody>
              {i.assignments.map(a => (
                <tr key={a.id}>
                  <td><span className="who"><Link to={`/users/${a.technician.id}`}>{a.technician.name}</Link><small>{a.role}</small></span></td>
                  <td className="num">{formatMoment(a.assignedAt)}</td>
                  <td>
                    {a.completedAt
                      ? <span className="chip tone-positive"><Icon icon={CircleCheck} strokeWidth={2} />Completed</span>
                      : <span className="chip tone-neutral">Assigned</span>}
                    {a.completedAt && (
                      <div className="done-by">
                        {formatMoment(a.completedAt)}{a.completedBy && a.completedBy.id !== a.technician.id ? `, by ${a.completedBy.name} for them` : ''}
                      </div>
                    )}
                  </td>
                  <td className="act">
                    <span className="row-acts">
                      {a.canComplete && <button className="btn btn-secondary btn-sm" type="button" disabled={busy} onClick={() => onComplete(a.id)}><Icon icon={Check} />Mark complete</button>}
                      {a.canRemove && (
                        <button className="btn btn-quiet btn-sm btn-icon" type="button" disabled={busy} aria-label={`Remove ${a.technician.name} as ${a.role}`} onClick={() => onRemove(a.id)}>
                          <Icon icon={X} />
                        </button>
                      )}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      {i.assignments.length > 0 && (
        <ul className="record-cards" aria-label="Assignments">
          {i.assignments.map(a => (
            <li key={a.id} className="record-card">
              <b>{a.technician.name}</b>
              <small>{a.role} · assigned {formatMoment(a.assignedAt)}{a.completedAt && <><br />Completed {formatMoment(a.completedAt)}</>}</small>
              {a.canComplete && <button className="btn btn-secondary btn-sm" type="button" disabled={busy} onClick={() => onComplete(a.id)}><Icon icon={Check} />Mark complete</button>}
            </li>
          ))}
        </ul>
      )}
    </>
  );
}

function History({ installation: i }: { installation: Installation }) {
  return (
    <ol className="timeline" aria-label="History, newest first">
      {i.history.map((h, n) => (
        <li key={n}>
          <span className="when">{formatMoment(h.at)}</span>
          <span className="what">
            <b>{historyLine(h)}</b>
            {historyBy(h) && <small>{historyBy(h)}</small>}
            {h.note && <small className="note">{h.note}</small>}
          </span>
        </li>
      ))}
    </ol>
  );
}
