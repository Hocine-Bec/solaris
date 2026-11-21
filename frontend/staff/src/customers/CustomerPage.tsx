import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { customerKey, fetchCustomer, type Customer, type CustomerStatus } from '../api/customers';
import { Icon } from '../components/Icon';
import { formatDay, formatMoment } from '../lib/dates';
import { idFrom } from '../lib/ids';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { NotAllowed, NotFound } from '../states/States';
import { QueryView } from '../states/QueryView';
import { cityLine } from '../leads/view';
import { CustomerChip, StageChip } from './Chips';

/** Why a customer has their status (BR-C1): it follows their installations. */
const STATUS_WHY: Record<CustomerStatus, string> = {
  Prospect: 'No installation is switched on yet.',
  Active: 'At least one installation is switched on.',
  Inactive: 'Every installation is cancelled or deactivated.',
};

/** One customer (FR-C1, FR-C2): their contact, their status and why, and their installations, newest first. */
export function CustomerPage() {
  const me = useCurrentUser();
  const id = idFrom(useParams().id);
  const allowed = opensFor(me.role, '/customers');
  const query = useQuery({
    queryKey: customerKey(id ?? 0),
    queryFn: ({ signal }) => fetchCustomer(id!, signal),
    enabled: allowed && id !== null,
  });
  const back = { to: '/customers', label: 'Back to customers' };

  if (!allowed) return <NotAllowed />;
  if (id === null) return <NotFound noun="customer" back={back} />;
  return (
    <QueryView query={query} what="this customer" notFound={<NotFound noun="customer" back={back} />}>
      {customer => <CustomerView customer={customer} />}
    </QueryView>
  );
}

function CustomerView({ customer: c }: { customer: Customer }) {
  return (
    <>
      <div className="page-head">
        <Link className="btn btn-quiet btn-sm back" to="/customers"><Icon icon={ArrowLeft} />Customers</Link>
        <div className="row">
          <h1 className="a-title" id="page-title" tabIndex={-1}>{c.name} <CustomerChip status={c.status} /></h1>
        </div>
      </div>
      <p className="page-intro">{STATUS_WHY[c.status]} The status follows the installations; nobody sets it by hand.</p>

      <div className="rec-card">
        <dl className="kv">
          <dt>Email</dt><dd><a href={`mailto:${c.email}`}>{c.email}</a></dd>
          <dt>Phone</dt><dd><a href={`tel:${c.phone.replace(/[^\d+]/g, '')}`}>{c.phone}</a></dd>
          {c.address && <><dt>Address</dt><dd>{c.address.street}, {cityLine(c.address)}</dd></>}
          <dt>Customer since</dt><dd className="num">{formatDay(c.createdAt)}</dd>
        </dl>
      </div>

      <section aria-labelledby="installations-title">
        <div className="section-title">
          <h2 className="a-heading" id="installations-title">Installations</h2>
          <span className="a-label">Newest first</span>
        </div>
        <div className="table-wrap list-table">
          <table className="table">
            <caption className="sr">{c.name}'s installations, newest first</caption>
            <thead>
              <tr><th scope="col">Property</th><th scope="col">Stage</th><th scope="col">Request</th><th scope="col" className="r">Started</th></tr>
            </thead>
            <tbody>
              {c.installations.map(i => (
                <tr key={i.id}>
                  <td><span className="who"><Link to={`/installations/${i.id}`}>{i.address.street}</Link><small>{cityLine(i.address)}</small></span></td>
                  <td><StageChip status={i.status} /></td>
                  <td><Link to={`/leads/${i.leadId}`}>Received {formatDay(i.leadReceivedAt)}</Link></td>
                  <td className="num r">{formatMoment(i.createdAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <ul className="record-cards" aria-label={`${c.name}'s installations, newest first`}>
          {c.installations.map(i => (
            <li key={i.id}>
              <Link className="record-card" to={`/installations/${i.id}`}>
                <b>{i.address.street}</b>
                <small>{cityLine(i.address)}<br />Started {formatMoment(i.createdAt)}</small>
                <span className="row"><StageChip status={i.status} /></span>
              </Link>
            </li>
          ))}
        </ul>
      </section>
    </>
  );
}
