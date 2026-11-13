import { QueryClientProvider } from '@tanstack/react-query';
import { createBrowserRouter, RouterProvider, type RouteObject } from 'react-router-dom';
import { createQueryClient } from './api/queryClient';
import { ErrorPage } from './pages/ErrorPage';
import { LandingRedirect } from './pages/LandingRedirect';
import { NotFoundPage } from './pages/NotFoundPage';
import { SectionPage } from './pages/SectionPage';
import { RequireSession } from './session/RequireSession';
import { MY_WORK, OFFICE } from './shell/sections';
import type { RouteHandle } from './shell/Shell';
import { SignInPage } from './signin/SignInPage';

const handle = (title: string): RouteHandle => ({ title });

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
          ...[...OFFICE, MY_WORK].map(s => ({ path: s.path, element: <SectionPage section={s} />, handle: handle(s.title) })),
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
