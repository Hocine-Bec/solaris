import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { SolarPanel } from 'lucide-react';
import { fetchInstallations, INSTALLATIONS_PAGE_SIZE, installationsPageKey } from '../api/installations';
import { PageHead } from '../components/PageHead';
import { Pager } from '../components/Pager';
import { StageChip } from '../customers/Chips';
import { formatMoment } from '../lib/dates';
import { pageFrom } from '../lib/paging';
import { cityLine } from '../leads/view';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { Empty, NotAllowed } from '../states/States';
import { QueryView } from '../states/QueryView';
import { emptyFor, filterFrom, FILTERS, installationsHref } from './view';

/**
 * The installation list (FR-I1), from the approved design: the last change first, opening on All, with In progress,
 * each stage, Active and Closed beside it. The filter and the page are in the address. Below 768 px the rows become
 * cards.
 */
export function InstallationsPage() {
  const me = useCurrentUser();
  const [params] = useSearchParams();
  const filter = filterFrom(params.get('status'));
  const page = pageFrom(params.get('page'));
  const query = useQuery({
    queryKey: installationsPageKey(filter.statuses, page),
    queryFn: ({ signal }) => fetchInstallations(filter.statuses, page, signal),
    enabled: me.role === 'Admin',
  });

  if (!opensFor(me.role, '/installations')) return <NotAllowed />;

  const empty = emptyFor(filter);
  const caption = `Installations, ${filter.label.toLowerCase()}, last change first`;
  return (
    <>
      <PageHead title="Installations">
        <nav className="filters" aria-label="Filter installations">
          {FILTERS.map(f => (
            <Link key={f.key} to={installationsHref(f)} aria-current={f.key === filter.key ? 'true' : undefined}>{f.label}</Link>
          ))}
        </nav>
      </PageHead>
      <QueryView
        query={query} what="the installations" note="Your filter is kept." isEmpty={data => data.totalCount === 0}
        empty={<Empty icon={SolarPanel} title={empty.title}>{empty.text}</Empty>}
      >
        {data => (
          <>
            <div className="table-wrap list-table">
              <table className="table">
                <caption className="sr">{caption}</caption>
                <thead>
                  <tr><th scope="col">Property</th><th scope="col">Customer</th><th scope="col">Stage</th><th scope="col" className="r">Last change</th></tr>
                </thead>
                <tbody>
                  {data.items.map(i => (
                    <tr key={i.id}>
                      <td><span className="who"><Link to={`/installations/${i.id}`}>{i.address.street}</Link><small>{cityLine(i.address)}</small></span></td>
                      <td className="dim">{i.customer.name}</td>
                      <td><StageChip status={i.status} /></td>
                      <td className="num r">{formatMoment(i.statusChangedAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <ul className="record-cards" aria-label={caption}>
              {data.items.map(i => (
                <li key={i.id}>
                  <Link className="record-card" to={`/installations/${i.id}`}>
                    <b>{i.address.street}</b>
                    <small>{cityLine(i.address)} · {i.customer.name}<br />Last change {formatMoment(i.statusChangedAt)}</small>
                    <span className="row"><StageChip status={i.status} /></span>
                  </Link>
                </li>
              ))}
            </ul>
            <Pager label="Pages of installations" page={page} pageSize={INSTALLATIONS_PAGE_SIZE} totalCount={data.totalCount} hrefFor={p => installationsHref(filter, p)} />
          </>
        )}
      </QueryView>
    </>
  );
}
