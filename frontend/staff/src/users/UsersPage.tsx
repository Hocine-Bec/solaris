import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link, useSearchParams } from 'react-router-dom';
import { HardHat, Lock, UserPlus, Users } from 'lucide-react';
import { fetchUsers, USERS_PAGE_SIZE, usersPageKey, type User, type UsersPage as Page } from '../api/users';
import { Icon } from '../components/Icon';
import { PageHead } from '../components/PageHead';
import { Pager } from '../components/Pager';
import { formatMoment } from '../lib/dates';
import { pageFrom } from '../lib/paging';
import { useCurrentUser } from '../session/session';
import { opensFor } from '../shell/sections';
import { Empty, NotAllowed } from '../states/States';
import { QueryView } from '../states/QueryView';
import { NewTechnicianDialog } from './AccountDialogs';
import { StatusChip } from './StatusChip';

const lastSignIn = (user: User) => (user.lastLoginAt ? formatMoment(user.lastLoginAt) : null);

/**
 * The Users list (FR-U2): every staff account, active ones first, by name, with its role, its status and when it last
 * signed in. The Admin creates technicians here; a name opens the account. Below 768 px the rows become cards.
 */
export function UsersPage() {
  const me = useCurrentUser();
  const [params] = useSearchParams();
  const page = pageFrom(params.get('page'));
  const [creating, setCreating] = useState(false);
  const query = useQuery({ queryKey: usersPageKey(page), queryFn: ({ signal }) => fetchUsers(page, signal), enabled: me.role === 'Admin' });

  if (!opensFor(me.role, '/users')) return <NotAllowed />;

  const newTechnician = (
    <button className="btn btn-primary" type="button" onClick={() => setCreating(true)}><Icon icon={UserPlus} />New technician</button>
  );
  // Only the signed-in Admin: nobody else to list yet
  const onlyYou = (data: Page) => data.totalCount === 1 && data.items[0]?.id === me.id;

  return (
    <>
      <PageHead title="Users">{newTechnician}</PageHead>
      <QueryView
        query={query} what="the users" isEmpty={onlyYou}
        empty={
          <Empty icon={Users} title="No technicians yet">
            You're the only account. Create one for each technician; they sign in with the email and password you give them.
          </Empty>
        }
      >
        {data => (
          <>
            <div className="table-wrap users-table">
              <table className="table">
                <caption className="sr">Staff accounts, active first</caption>
                <thead>
                  <tr><th scope="col">Name</th><th scope="col">Role</th><th scope="col">Status</th><th scope="col" className="r">Last signed in</th></tr>
                </thead>
                <tbody>
                  {data.items.map(u => (
                    <tr key={u.id}>
                      <td>
                        <span className="who">
                          <Link to={`/users/${u.id}`}>{u.name}</Link>
                          <small>{u.email}</small>
                        </span>
                      </td>
                      <td>{u.role}{u.id === me.id && <span className="you">You</span>}</td>
                      <td><StatusChip isActive={u.isActive} /></td>
                      <td className="num r">{lastSignIn(u) ?? <span className="never">Never</span>}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <ul className="user-cards" aria-label="Staff accounts, active first">
              {data.items.map(u => (
                <li key={u.id}>
                  <Link className="user-card" to={`/users/${u.id}`}>
                    <b>{u.name}{u.id === me.id && <span className="you">You</span>}</b>
                    <small>{u.email}<br />Last signed in: {lastSignIn(u) ?? 'never'}</small>
                    <span className="row">
                      <span className="chip tone-neutral"><Icon icon={u.role === 'Admin' ? Lock : HardHat} strokeWidth={2} />{u.role}</span>
                      <StatusChip isActive={u.isActive} />
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
            <Pager label="Pages of users" page={page} pageSize={USERS_PAGE_SIZE} totalCount={data.totalCount} hrefFor={p => `/users?page=${p}`} />
          </>
        )}
      </QueryView>
      {creating && <NewTechnicianDialog onClose={() => setCreating(false)} />}
    </>
  );
}
