import type { ReactNode } from 'react';
import { QueryClientProvider } from '@tanstack/react-query';
import { createBrowserRouter, RouterProvider, type RouteObject } from 'react-router-dom';
import { createQueryClient } from './api/queryClient';
import { ErrorPage } from './pages/ErrorPage';
import { LandingRedirect } from './pages/LandingRedirect';
import { NotFoundPage } from './pages/NotFoundPage';
import { SectionPage } from './pages/SectionPage';
import { RequireSession } from './session/RequireSession';
import { MY_WORK, OFFICE, type Section } from './shell/sections';
import type { RouteHandle } from './shell/Shell';
import { CustomerPage } from './customers/CustomerPage';
import { CustomersPage } from './customers/CustomersPage';
import { LeadPage } from './leads/LeadPage';
import { LeadsPage } from './leads/LeadsPage';
import { NotificationsPage } from './notifications/NotificationsPage';
import { SignInPage } from './signin/SignInPage';
import { UserPage } from './users/UserPage';
import { UsersPage } from './users/UsersPage';

const handle = (title: string): RouteHandle => ({ title });

/** The screens built so far; the other sections show what they will hold. */
const BUILT: Record<string, ReactNode> = {
  '/leads': <LeadsPage />, '/customers': <CustomersPage />, '/notifications': <NotificationsPage />, '/users': <UsersPage />,
};

/** An installation's page comes with the installations; until then, what it will hold (a lead's and a customer's links reach it). */
const INSTALLATION: Section = {
  ...OFFICE.find(s => s.path === '/installations')!,
  title: 'Installation',
  holds: 'One installation: the stage tracker, the next move, the design, the time zone to confirm, the assignments and the history.',
};

const routes: RouteObject[] = [
  { path: '/login', element: <SignInPage />, errorElement: <ErrorPage /> },
  {
    // Everything else needs a session; without one, the sign-in page opens and remembers the address.
    element: <RequireSession />,
    errorElement: <ErrorPage />,   // the frame itself failed
    children: [
      {
        errorElement: <ErrorPage />,   // a page failed: the frame stays
        children: [
          { index: true, element: <LandingRedirect /> },
          ...[...OFFICE, MY_WORK].map(s => ({ path: s.path, element: BUILT[s.path] ?? <SectionPage section={s} />, handle: handle(s.title) })),
          { path: '/leads/:id', element: <LeadPage />, handle: handle('Leads') },
          { path: '/customers/:id', element: <CustomerPage />, handle: handle('Customers') },
          { path: '/installations/:id', element: <SectionPage section={INSTALLATION} />, handle: handle('Installations') },
          { path: '/users/:id', element: <UserPage />, handle: handle('Users') },
          { path: '*', element: <NotFoundPage />, handle: handle('Page not found') },
        ],
      },
    ],
  },
];

const router = createBrowserRouter(routes);
const queryClient = createQueryClient();

export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <RouterProvider router={router} />
    </QueryClientProvider>
  );
}
