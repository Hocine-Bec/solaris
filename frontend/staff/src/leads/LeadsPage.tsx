import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { Inbox } from 'lucide-react';
import { fetchLeads, LEADS_PAGE_SIZE, leadsPageKey } from '../api/leads';
import { PageHead } from '../components/PageHead';
import { Pager } from '../components/Pager';
import { formatMoment } from '../lib/dates';
import { pageFrom } from '../lib/paging';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { Empty, NotAllowed } from '../states/States';
import { QueryView } from '../states/QueryView';
import { LeadChip } from './LeadChip';
import { cityLine, EMPTY, filterFrom, FILTERS, leadsHref } from './view';

/**
 * The lead list (FR-L3), from the approved design: newest first, opening on All, with New, Contacted and Closed beside it.
 * The filter and the page are in the address. A name opens the lead, where its actions are. Below 768 px the rows
 * become cards.
 */
export function LeadsPage() {
  const me = useCurrentUser();
  const [params] = useSearchParams();
  const filter = filterFrom(params.get('status'));
  const page = pageFrom(params.get('page'));
  const query = useQuery({
    queryKey: leadsPageKey(filter.statuses, page),
    queryFn: ({ signal }) => fetchLeads(filter.statuses, page, signal),
    enabled: me.role === 'Admin',
  });

  if (!opensFor(me.role, '/leads')) return <NotAllowed />;

  const empty = EMPTY[filter.key];
  const caption = `Leads, ${filter.label.toLowerCase()}, newest first`;
  return (
    <>
      <PageHead title="Leads">
        <nav className="filters" aria-label="Filter leads">
          {FILTERS.map(f => (
            <Link key={f.key} to={leadsHref(f)} aria-current={f.key === filter.key ? 'true' : undefined}>{f.label}</Link>
          ))}
        </nav>
      </PageHead>
      <QueryView
        query={query} what="the leads" note="Your filter is kept." isEmpty={data => data.totalCount === 0}
        empty={<Empty icon={Inbox} title={empty.title}>{empty.text}</Empty>}
      >
        {data => (
          <>
            <div className="table-wrap list-table">
              <table className="table">
                <caption className="sr">{caption}</caption>
                <thead>
                  <tr>
                    <th scope="col">Name</th><th scope="col">Property</th><th scope="col">Type</th>
                    <th scope="col">Status</th><th scope="col" className="r">Received</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map(l => (
                    <tr key={l.id}>
                      <td><span className="who"><Link to={`/leads/${l.id}`}>{l.name}</Link><small>{l.email}</small></span></td>
                      <td><span className="who">{l.address.street}<small>{cityLine(l.address)}</small></span></td>
                      <td className="dim">{l.propertyType}</td>
                      <td><LeadChip status={l.status} /></td>
                      <td className="num r">{formatMoment(l.createdAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <ul className="record-cards" aria-label={caption}>
              {data.items.map(l => (
                <li key={l.id}>
                  <Link className="record-card" to={`/leads/${l.id}`}>
                    <b>{l.name}</b>
                    <small>{l.address.street}, {l.address.city}, {l.address.state}<br />Received {formatMoment(l.createdAt)}</small>
                    <span className="row"><LeadChip status={l.status} /></span>
                  </Link>
                </li>
              ))}
            </ul>
            <Pager label="Pages of leads" page={page} pageSize={LEADS_PAGE_SIZE} totalCount={data.totalCount} hrefFor={p => leadsHref(filter, p)} />
          </>
        )}
      </QueryView>
    </>
  );
}
