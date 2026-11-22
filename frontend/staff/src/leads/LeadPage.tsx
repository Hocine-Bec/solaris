import { useRef, useState } from 'react';
import { flushSync } from 'react-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Ban, CircleCheck, CircleDashed, House, Info, LoaderCircle, Pencil, Phone, Quote, SolarPanel, TriangleAlert } from 'lucide-react';
import { ApiError } from '../api/errors';
import { CUSTOMERS_KEY } from '../api/customers';
import { fetchLead, LEADS_KEY, leadKey, moveLead, type Lead } from '../api/leads';
import { Icon } from '../components/Icon';
import { FormNotice } from '../components/SubmitButton';
import { formatDay, formatMoment } from '../lib/dates';
import { idFrom } from '../lib/ids';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { ConflictNotice, NotAllowed, NotFound } from '../states/States';
import { QueryView } from '../states/QueryView';
import { ConvertDialog, DisqualifyDialog, EditLeadDialog, LostDialog } from './LeadDialogs';
import { LeadChip } from './LeadChip';
import { actionsFor, billLabel, cityLine, firstNameOf, isChangedMeanwhile, propertyLabel } from './view';

type Open = 'edit' | 'disqualify' | 'lose' | 'convert' | null;

const UNEXPECTED = 'Something went wrong in the app. Try again; if it happens again, report it.';

/**
 * One lead (FR-L3–L5, FR-L7), from the approved design: the request as it was sent, its progress, and the moves the
 * API allows now. Mark as contacted happens at once; the moves that close the lead ask first.
 */
export function LeadPage() {
  const me = useCurrentUser();
  const id = idFrom(useParams().id);
  const allowed = opensFor(me.role, '/leads');
  const query = useQuery({
    queryKey: leadKey(id ?? 0),
    queryFn: ({ signal }) => fetchLead(id!, signal),
    enabled: allowed && id !== null,
  });
  const back = { to: '/leads', label: 'Back to leads' };

  if (!allowed) return <NotAllowed />;
  if (id === null) return <NotFound noun="lead" back={back} />;
  return (
    <QueryView query={query} what="this lead" notFound={<NotFound noun="lead" back={back} />}>
      {lead => <LeadView key={lead.id} lead={lead} />}
    </QueryView>
  );
}

function LeadView({ lead }: { lead: Lead }) {
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const [open, setOpen] = useState<Open>(null);
  const [saved, setSaved] = useState<string | null>(null);
  const [failed, setFailed] = useState<string | null>(null);
  const [movedMeanwhile, setMovedMeanwhile] = useState<string | null>(null);
  const [stale, setStale] = useState(false);
  // After Mark as contacted, focus goes to Convert, the next move, once the answer has drawn it
  const focusConvert = useRef(false);
  const convertButton = (button: HTMLButtonElement | null) => {
    if (button && focusConvert.current) {
      focusConvert.current = false;
      button.focus();
    }
  };
  const contact = useMutation({ mutationFn: () => moveLead(lead.id, 'Contacted') });
  const can = actionsFor(lead);

  const clear = () => { setSaved(null); setFailed(null); setMovedMeanwhile(null); setStale(false); };
  const show = (dialog: Open) => { clear(); setOpen(dialog); };
  const reload = () => { clear(); void queryClient.invalidateQueries({ queryKey: leadKey(lead.id) }); };
  const close = () => setOpen(null);

  /** The lead as the API answered it: this page shows it at once, and the lists load again when next shown. */
  const keep = (changed: Lead) => {
    queryClient.setQueryData(leadKey(changed.id), changed);
    void queryClient.invalidateQueries({ queryKey: [...LEADS_KEY, 'page'] });
  };

  /**
   * Another Admin changed it first. Moved before this change was tried: say so, and show the lead as it is now.
   * Saved at the same moment: say so, and offer to reload. Nothing was overwritten either way.
   */
  const onMovedMeanwhile = (error: ApiError) => {
    if (error.code === 'lead.concurrent_change') {
      flushSync(() => { setOpen(null); setStale(true); });
    } else {
      flushSync(() => { setOpen(null); setMovedMeanwhile(error.message); });
      void queryClient.invalidateQueries({ queryKey: leadKey(lead.id) });
    }
    document.getElementById('page-title')?.focus();
  };

  const doContact = async () => {
    if (contact.isPending) return;
    clear();
    try {
      const changed = await contact.mutateAsync();
      focusConvert.current = true;
      keep(changed);
      setSaved('Saved');
    } catch (error) {
      if (isChangedMeanwhile(error)) onMovedMeanwhile(error);
      else setFailed(error instanceof ApiError ? error.message : UNEXPECTED);
    }
  };

  /** A move that closed the lead: its buttons are gone, so focus goes to the title, which now shows its status. */
  const closed = (changed: Lead) => {
    flushSync(() => { keep(changed); setOpen(null); });
    document.getElementById('page-title')?.focus();
  };

  const converted = (changed: Lead) => {
    keep(changed);
    void queryClient.invalidateQueries({ queryKey: CUSTOMERS_KEY });
    setOpen(null);
    // The approved flow: on to the new installation
    if (changed.installationId) navigate(`/installations/${changed.installationId}`);
  };

  const edited = (changed: Lead) => {
    keep(changed);
    setOpen(null);
    setSaved('Saved');
  };

  return (
    <>
      <div className="page-head">
        <Link className="btn btn-quiet btn-sm back" to="/leads"><Icon icon={ArrowLeft} />Leads</Link>
        <div className="row">
          <h1 className="a-title" id="page-title" tabIndex={-1}>{lead.name} <LeadChip status={lead.status} /></h1>
          {can.edit && (
            <div className="page-actions">
              {saved && <span className="saved" role="status"><Icon icon={CircleCheck} />{saved}</span>}
              <button className="btn btn-secondary" type="button" onClick={() => show('edit')}><Icon icon={Pencil} />Edit</button>
              {can.disqualify && <button className="btn btn-secondary" type="button" onClick={() => show('disqualify')}><Icon icon={Ban} />Disqualify…</button>}
              {can.lose && <button className="btn btn-secondary" type="button" onClick={() => show('lose')}><Icon icon={CircleDashed} />Mark as lost…</button>}
              {can.contact && (
                <button className={`btn btn-primary${contact.isPending ? ' is-loading' : ''}`} type="button" aria-busy={contact.isPending || undefined} onClick={() => void doContact()}>
                  <Icon icon={LoaderCircle} className="spin" />
                  {!contact.isPending && <Icon icon={Phone} />}
                  <span className="btn-text">Mark as contacted</span>
                </button>
              )}
              {can.convert && (
                <button ref={convertButton} className="btn btn-primary" type="button" onClick={() => show('convert')}><Icon icon={CircleCheck} />Convert…</button>
              )}
            </div>
          )}
        </div>
      </div>

      {failed && <div className="page-notice"><FormNotice>{failed}</FormNotice></div>}
      {stale && <div className="page-notice"><ConflictNotice noun="lead" onReload={reload} /></div>}
      {movedMeanwhile && (
        <div className="notice tone-warning page-notice" role="alert">
          <Icon icon={TriangleAlert} />
          <p className="notice-title">{movedMeanwhile}</p>
          <p>Another Admin moved this lead while it was open here; it has been loaded again. Nothing was overwritten.</p>
        </div>
      )}
      <Outcome lead={lead} />

      <div className="rec-grid">
        <div className="rec-card">
          <p className="a-label">Contact</p>
          <dl className="kv">
            <dt>Email</dt><dd><a href={`mailto:${lead.email}`}>{lead.email}</a></dd>
            <dt>Phone</dt><dd><a href={`tel:${lead.phone.replace(/[^\d+]/g, '')}`}>{lead.phone}</a></dd>
            <dt>Best time</dt><dd>{lead.bestTimeToContact}</dd>
            {lead.customer && lead.status !== 'Converted' && (
              // While the lead is open, the API names the customer that already has its email
              <>
                <dt>Customer</dt>
                <dd>
                  <Link to={`/customers/${lead.customer.id}`}>{lead.customer.name}</Link>, since {formatDay(lead.customer.since)}:
                  converting adds to their account
                </dd>
              </>
            )}
          </dl>
        </div>
        <div className="rec-card">
          <p className="a-label">Property</p>
          <dl className="kv">
            <dt>Address</dt><dd>{lead.address.street}<br />{cityLine(lead.address)}</dd>
            <dt>Type</dt><dd>{propertyLabel(lead)}</dd>
            <dt>Bill</dt><dd>{billLabel(lead.monthlyBillRange)}</dd>
          </dl>
        </div>
      </div>

      {lead.notes && (
        <div className="note-quote">
          <Icon icon={Quote} />
          <p>{lead.notes}<small>Their note, from the form</small></p>
        </div>
      )}

      <Progress lead={lead} />

      {open === 'edit' && <EditLeadDialog lead={lead} onClose={close} onDone={edited} />}
      {open === 'disqualify' && <DisqualifyDialog lead={lead} onClose={close} onDone={closed} onMovedMeanwhile={onMovedMeanwhile} />}
      {open === 'lose' && <LostDialog lead={lead} onClose={close} onDone={closed} onMovedMeanwhile={onMovedMeanwhile} />}
      {open === 'convert' && <ConvertDialog lead={lead} onClose={close} onDone={converted} onMovedMeanwhile={onMovedMeanwhile} />}
    </>
  );
}

/** How a closed lead ended: a converted one leads to its customer and installation. */
function Outcome({ lead }: { lead: Lead }) {
  if (lead.status === 'Converted' && lead.customer) {
    return (
      <div className="notice tone-positive page-notice">
        <Icon icon={CircleCheck} />
        <p className="notice-title">Converted {lead.closedAt && formatMoment(lead.closedAt)}</p>
        <p>{lead.customer.name} is a customer, with an installation at {lead.address.street}.</p>
        <div className="actions">
          {lead.installationId && <Link className="btn btn-secondary btn-sm" to={`/installations/${lead.installationId}`}><Icon icon={SolarPanel} />Open the installation</Link>}
          <Link className="btn btn-quiet btn-sm" to={`/customers/${lead.customer.id}`}><Icon icon={House} />Open the customer</Link>
        </div>
      </div>
    );
  }
  if (lead.status === 'Disqualified' || lead.status === 'Lost') {
    return (
      <div className="notice tone-info page-notice">
        <Icon icon={Info} />
        <p className="notice-title">Closed as {lead.status}{lead.closedAt && ` ${formatMoment(lead.closedAt)}`}</p>
        <p>If {firstNameOf(lead)} sends a new request, it becomes a new lead.</p>
      </div>
    );
  }
  return null;
}

/** Received, contacted, then converted or closed: when each happened */
function Progress({ lead }: { lead: Lead }) {
  const isClosed = lead.next.length === 0;
  return (
    <section className="lead-progress" aria-labelledby="progress-title">
      <h2 className="a-label" id="progress-title">Progress</h2>
      <ol className="lead-steps">
        <li className="done">
          <span>Received</span><b>From the website</b><small className="num">{formatMoment(lead.createdAt)}</small>
        </li>
        {lead.contactedAt ? (
          <li className="done" aria-current={lead.status === 'Contacted' ? 'step' : undefined}>
            <span>Contacted</span><b>Done</b><small className="num">{formatMoment(lead.contactedAt)}</small>
          </li>
        ) : (
          <li className={isClosed ? 'closed' : undefined}>
            <span>Contacted</span>
            <b>{isClosed ? 'Skipped' : 'Not yet'}</b>
            <small>{isClosed ? 'Closed while New' : 'Mark it after a call or an email'}</small>
          </li>
        )}
        {isClosed ? (
          <li className={lead.status === 'Converted' ? 'done' : 'closed'} aria-current="step">
            <span>{lead.status}</span><b>Closed</b><small className="num">{lead.closedAt ? formatMoment(lead.closedAt) : '—'}</small>
          </li>
        ) : (
          <li>
            <span>Converted or closed</span><b>Not yet</b>
            <small>{lead.status === 'Contacted' ? 'Convert it, or close it' : 'After it is contacted'}</small>
          </li>
        )}
      </ol>
    </section>
  );
}
