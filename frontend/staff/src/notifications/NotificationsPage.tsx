import { Fragment, useState } from 'react';
import { flushSync } from 'react-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { ChevronDown, ChevronUp, CircleCheck, Inbox, ListRestart, LoaderCircle, Mail, RotateCcw } from 'lucide-react';
import { ApiError } from '../api/errors';
import { FAILED_PAGE_SIZE, failedPageKey, fetchFailed, retryAllFailed, retryNotification, type FailedNotification } from '../api/notifications';
import { Dialog } from '../components/Dialog';
import { Icon } from '../components/Icon';
import { PageHead } from '../components/PageHead';
import { Pager } from '../components/Pager';
import { FormNotice, SubmitButton } from '../components/SubmitButton';
import { formatMoment } from '../lib/dates';
import { pageFrom } from '../lib/paging';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { Empty, NotAllowed } from '../states/States';
import { QueryView } from '../states/QueryView';
import { emailName, oneLine, stillFailed, toLine } from './view';

const UNEXPECTED = 'Something went wrong in the app. Try again; if it happens again, report it.';
const messageOf = (error: unknown) => (error instanceof ApiError ? error.message : UNEXPECTED);

/**
 * Failed notifications (FR-N2), from the approved design: the emails the outbox couldn't send in 24 hours of tries,
 * newest failure first. A click anywhere on a row opens its details (with the keyboard, the error's start is the
 * button). Retry sends one again without asking, and the row says so; Retry all asks first. A retried email leaves the
 * list at the next load. Below 768 px the rows become cards.
 */
export function NotificationsPage() {
  const me = useCurrentUser();
  const [params] = useSearchParams();
  const page = pageFrom(params.get('page'));
  const query = useQuery({ queryKey: failedPageKey(page), queryFn: ({ signal }) => fetchFailed(page, signal), enabled: me.role === 'Admin' });
  const [open, setOpen] = useState<ReadonlySet<string>>(new Set());
  const [sending, setSending] = useState<ReadonlySet<string>>(new Set());
  const [retryingAll, setRetryingAll] = useState(false);
  const [allRetried, setAllRetried] = useState<number | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const retry = useMutation({
    mutationFn: retryNotification,
    onSuccess: (_, id) => {
      // The row's button gives way to "Sending again": focus goes to the next Retry, or to the title
      flushSync(() => setSending(s => new Set(s).add(id)));
      focusNextRetry(id);
    },
    onError: error => {
      // Retried meanwhile (another Admin, another tab) or sent: the list is out of date
      if (error instanceof ApiError && error.code === 'notification.not_failed') void query.refetch();
      else setNotice(`We couldn't retry the email. ${messageOf(error)}`);
    },
  });

  if (!opensFor(me.role, '/notifications')) return <NotAllowed />;

  const toggle = (id: string) => setOpen(o => {
    const next = new Set(o);
    if (!next.delete(id)) next.add(id);
    return next;
  });
  const retryOne = (id: string) => {
    if (retry.isPending) return;
    setNotice(null);
    setAllRetried(null);
    setOpen(o => { const next = new Set(o); next.delete(id); return next; });
    retry.mutate(id);
  };
  const remaining = query.data ? stillFailed(query.data.totalCount, query.data.items, sending) : 0;

  return (
    <>
      <PageHead title="Failed notifications">
        <div className="page-actions">
          {allRetried !== null && (
            <span className="saved" role="status"><Icon icon={CircleCheck} />{allRetried === 1 ? '1 email sending again' : `${allRetried} emails sending again`}</span>
          )}
          {remaining > 1 && (
            <button className="btn btn-secondary" type="button" onClick={() => { setNotice(null); setRetryingAll(true); }}>
              <Icon icon={ListRestart} />Retry all ({remaining})
            </button>
          )}
        </div>
      </PageHead>
      <p className="page-intro">Emails that still couldn't be sent after 24 hours of tries. A retry sends one again within seconds, and tries it for another 24 hours if it fails.</p>
      {notice && <div className="page-notice"><FormNotice>{notice}</FormNotice></div>}
      <QueryView
        query={query} what="the failed notifications" isEmpty={data => data.totalCount === 0}
        empty={<Empty icon={Inbox} title="Nothing has failed">Emails that still can't be sent after 24 hours of tries appear here, ready to retry.</Empty>}
      >
        {data => (
          <div className="fail-list">
            <div className="table-wrap fail-table">
              <table className="table">
                <caption className="sr">Failed notifications, newest failure first</caption>
                <thead>
                  <tr>
                    <th scope="col">Email</th><th scope="col">Failed</th>
                    <th scope="col">Why <span className="sr">(opens the details)</span></th><th scope="col"><span className="sr">Retry</span></th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map(n => {
                    const isOpen = open.has(n.id) && !sending.has(n.id);
                    const isSending = sending.has(n.id);
                    return (
                      <Fragment key={n.id}>
                        <tr
                          className={[isOpen && 'open', isSending && 'is-retried'].filter(Boolean).join(' ') || undefined}
                          // A click anywhere on the row, but not on its buttons, opens it; the keyboard uses the error's button
                          onClick={isSending ? undefined : e => { if (!(e.target as Element).closest('button, a')) toggle(n.id); }}
                          data-row={isSending ? undefined : true}
                        >
                          <td>
                            <span className="two">
                              <span className="email"><Icon icon={n.type === 'LeadWelcome' ? Mail : Inbox} />{emailName(n)}</span>
                              <small>{toLine(n)}</small>
                            </span>
                          </td>
                          <td>
                            <span className="two">
                              <span className="num">{n.lastAttemptAt ? formatMoment(n.lastAttemptAt) : '—'}</span>
                              <small>{n.retriedAt ? 'Again, after a retry' : `${n.attempts} tries over 24 h`}</small>
                            </span>
                          </td>
                          <td>
                            {isSending ? <span className="err-line">{oneLine(n.lastError)}</span> : (
                              <button
                                className="err-toggle" type="button" aria-expanded={isOpen} aria-controls={`why-${n.id}`}
                                aria-label={`Details: ${oneLine(n.lastError)}`} onClick={() => toggle(n.id)}
                              >
                                <span className="err-line">{oneLine(n.lastError)}</span>
                                <Icon icon={isOpen ? ChevronUp : ChevronDown} />
                              </button>
                            )}
                          </td>
                          <td className="act">
                            <RetryCell id={n.id} sending={isSending} busy={retry.isPending && retry.variables === n.id} onRetry={() => retryOne(n.id)} />
                          </td>
                        </tr>
                        {isOpen && (
                          <tr className="details" id={`why-${n.id}`}>
                            <td colSpan={4}><Details n={n} /></td>
                          </tr>
                        )}
                      </Fragment>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <ul className="fail-cards" aria-label="Failed notifications, newest failure first">
              {data.items.map(n => (
                <li key={n.id} className="fail-card">
                  <b>{emailName(n)}</b>
                  <small>{toLine(n)}<br />Failed {n.lastAttemptAt ? formatMoment(n.lastAttemptAt) : '—'}</small>
                  <span className="err-line">{oneLine(n.lastError)}</span>
                  <RetryCell id={n.id} sending={sending.has(n.id)} busy={retry.isPending && retry.variables === n.id} onRetry={() => retryOne(n.id)} wide />
                </li>
              ))}
            </ul>
            <Pager label="Pages of failed notifications" page={page} pageSize={FAILED_PAGE_SIZE} totalCount={data.totalCount} hrefFor={p => `/notifications?page=${p}`} />
          </div>
        )}
      </QueryView>
      {retryingAll && (
        <RetryAllDialog
          count={remaining}
          onClose={() => setRetryingAll(false)}
          onDone={retried => {
            flushSync(() => {
              setSending(new Set(query.data?.items.map(n => n.id)));
              setOpen(new Set());
              setAllRetried(retried);
              setRetryingAll(false);
            });
            // The button that opened the dialog is gone: focus goes to the title, and the status reads the count
            document.getElementById('page-title')?.focus();
          }}
        />
      )}
    </>
  );
}

/** After a retry: the Retry of the next row still failed, in the table or the cards (whichever shows), or the title */
function focusNextRetry(retriedId: string) {
  const shown = (e: HTMLElement) => e.offsetParent !== null;
  const marker = [...document.querySelectorAll<HTMLElement>(`[data-retried="${retriedId}"]`)].find(shown);
  const next = marker && [...document.querySelectorAll<HTMLButtonElement>('[data-retry]')]
    .find(b => shown(b) && marker.compareDocumentPosition(b) & Node.DOCUMENT_POSITION_FOLLOWING);
  (next ?? document.getElementById('page-title'))?.focus();
}

function RetryCell({ id, sending, busy, onRetry, wide = false }: { id: string; sending: boolean; busy: boolean; onRetry: () => void; wide?: boolean }) {
  if (sending) return <span className="saved" role="status" data-retried={id}><Icon icon={CircleCheck} />Sending again</span>;
  return (
    <button className={`btn btn-secondary btn-sm${wide ? ' wide' : ''}${busy ? ' is-loading' : ''}`} type="button" aria-busy={busy || undefined} data-retry={id} onClick={onRetry}>
      <Icon icon={LoaderCircle} className="spin" />
      {!busy && <Icon icon={RotateCcw} />}
      <span className="btn-text">Retry</span>
    </button>
  );
}

/** The row opened: the whole error, the lead, when it was tried, how many times, and who retried it */
function Details({ n }: { n: FailedNotification }) {
  return (
    <dl className="why">
      <dt>Why it failed</dt><dd><code>{n.lastError ?? 'No error was recorded.'}</code></dd>
      <dt>Lead</dt><dd><Link to={`/leads/${n.lead.id}`}>{n.lead.name}</Link>, <span className="num">#{n.lead.id}</span></dd>
      <dt>First tried</dt><dd className="num">{formatMoment(n.retriedAt ?? n.createdAt)}{n.retriedAt && ', when it was retried'}</dd>
      <dt>Last tried</dt><dd className="num">{n.lastAttemptAt ? formatMoment(n.lastAttemptAt) : '—'}</dd>
      <dt>Tries</dt><dd className="num">{n.attempts}{n.retriedAt && ' since the retry'}, over 24 hours</dd>
      {n.retriedAt
        ? <><dt>Retried</dt><dd>By {n.retriedBy ?? 'an account that no longer exists'}, {formatMoment(n.retriedAt)}. First saved {formatMoment(n.createdAt)}.</dd></>
        : <><dt>Saved</dt><dd className="num">{formatMoment(n.createdAt)}, with the lead</dd></>}
    </dl>
  );
}

/** Retry all: many emails at once, so it asks first. */
function RetryAllDialog({ count, onClose, onDone }: { count: number; onClose: () => void; onDone: (retried: number) => void }) {
  const [error, setError] = useState<string | null>(null);
  const mutation = useMutation({ mutationFn: retryAllFailed });

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (mutation.isPending) return;
    setError(null);
    try {
      onDone((await mutation.mutateAsync()).retried);
    } catch (failure) {
      setError(messageOf(failure));
    }
  };

  return (
    <Dialog labelledBy="retry-all-title" onClose={onClose}>
      <form noValidate onSubmit={e => void submit(e)}>
        <p className="a-heading" id="retry-all-title">Retry all {count} emails?</p>
        <p>Each is sent again within seconds and tried for another 24 hours if it fails. If the cause is still there, for example Gmail's sign-in hasn't been renewed, they come back here.</p>
        {error && <FormNotice>{error}</FormNotice>}
        <div className="actions">
          <button className="btn btn-quiet" type="button" onClick={onClose}>Cancel</button>
          <SubmitButton busy={mutation.isPending}><Icon icon={ListRestart} />Retry all</SubmitButton>
        </div>
      </form>
    </Dialog>
  );
}
