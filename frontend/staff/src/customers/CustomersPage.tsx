import { useEffect, useState } from 'react';
import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { House, Search, SearchX, X } from 'lucide-react';
import { CUSTOMERS_PAGE_SIZE, customersPageKey, fetchCustomers } from '../api/customers';
import { Icon } from '../components/Icon';
import { PageHead } from '../components/PageHead';
import { Pager } from '../components/Pager';
import { formatDay } from '../lib/dates';
import { pageFrom } from '../lib/paging';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { Empty, NotAllowed } from '../states/States';
import { QueryView } from '../states/QueryView';
import { CustomerChip } from './Chips';

/** How long typing pauses before the list follows the search */
const SEARCH_DELAY_MS = 300;

const customersHref = (q: string, page = 1) => {
  const query = new URLSearchParams({ ...(q && { q }), ...(page > 1 && { page: String(page) }) }).toString();
  return query ? `/customers?${query}` : '/customers';
};

/**
 * The customer list (FR-C1), from the approved design: newest first, searched by part of a name or an email. The
 * search and the page are in the address; the list follows the search once typing pauses. Below 768 px the rows
 * become cards.
 */
export function CustomersPage() {
  const me = useCurrentUser();
  const [params, setParams] = useSearchParams();
  const q = (params.get('q') ?? '').trim();
  const page = pageFrom(params.get('page'));
  const [typed, setTyped] = useState(params.get('q') ?? '');
  // The address changed from elsewhere (Back, or Customers in the navigation): the box shows its search
  const [shownQ, setShownQ] = useState(q);
  if (shownQ !== q) {
    setShownQ(q);
    if (typed.trim() !== q) setTyped(q);
  }
  const query = useQuery({
    queryKey: customersPageKey(q, page),
    queryFn: ({ signal }) => fetchCustomers(q, page, signal),
    enabled: me.role === 'Admin',
    // While a new search loads, the last results stay, so the list doesn't flash at every pause in typing
    placeholderData: keepPreviousData,
  });

  // The address follows what is typed, once typing pauses; a new search starts at the first page
  useEffect(() => {
    if (typed.trim() === q) return;
    const timer = setTimeout(() => setParams(typed.trim() ? { q: typed.trim() } : {}, { replace: true }), SEARCH_DELAY_MS);
    return () => clearTimeout(timer);
  }, [typed, q, setParams]);

  if (!opensFor(me.role, '/customers')) return <NotAllowed />;

  const clear = () => { setTyped(''); setParams({}, { replace: true }); };
  return (
    <>
      <PageHead title="Customers">
        <div className="control list-search">
          <Icon icon={Search} />
          <label className="sr" htmlFor="customers-q">Search customers</label>
          <input id="customers-q" type="search" placeholder="Name or email" autoComplete="off" spellCheck={false} maxLength={100} value={typed} onChange={e => setTyped(e.target.value)} />
        </div>
      </PageHead>
      <QueryView
        query={query} what="the customers" note={q ? 'Your search is kept.' : undefined} isEmpty={data => data.totalCount === 0}
        empty={q ? (
          <div className="state-box">
            <div className="state">
              <span className="ring"><Icon icon={SearchX} /></span>
              <p className="a-heading">No customer matches “{q}”</p>
              <p>Search by part of a name or an email.</p>
              <button className="btn btn-secondary btn-sm" type="button" onClick={clear}><Icon icon={X} />Clear the search</button>
            </div>
          </div>
        ) : <Empty icon={House} title="No customers yet">A customer is created when a lead is converted.</Empty>}
      >
        {data => (
          <>
            <div className="table-wrap list-table">
              <table className="table">
                <caption className="sr">Customers, newest first</caption>
                <thead>
                  <tr>
                    <th scope="col">Name</th><th scope="col">Phone</th><th scope="col">Status</th>
                    <th scope="col" className="r">Installations</th><th scope="col" className="r">Customer since</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.map(c => (
                    <tr key={c.id}>
                      <td><span className="who"><Link to={`/customers/${c.id}`}>{c.name}</Link><small>{c.email}</small></span></td>
                      <td className="dim">{c.phone}</td>
                      <td><CustomerChip status={c.status} /></td>
                      <td className="num r">{c.installationCount}</td>
                      <td className="num r">{formatDay(c.createdAt)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <ul className="record-cards" aria-label="Customers, newest first">
              {data.items.map(c => (
                <li key={c.id}>
                  <Link className="record-card" to={`/customers/${c.id}`}>
                    <b>{c.name}</b>
                    <small>{c.email}<br />Customer since {formatDay(c.createdAt)} · {c.installationCount === 1 ? '1 installation' : `${c.installationCount} installations`}</small>
                    <span className="row"><CustomerChip status={c.status} /></span>
                  </Link>
                </li>
              ))}
            </ul>
            <Pager label="Pages of customers" page={page} pageSize={CUSTOMERS_PAGE_SIZE} totalCount={data.totalCount} hrefFor={p => customersHref(q, p)} />
          </>
        )}
      </QueryView>
    </>
  );
}
