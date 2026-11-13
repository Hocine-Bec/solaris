import { QueryClientProvider } from '@tanstack/react-query';
import { createBrowserRouter, Navigate, RouterProvider, type RouteObject } from 'react-router-dom';
import { createQueryClient } from './api/queryClient';
import { ErrorPage } from './pages/ErrorPage';
import { SectionPage } from './pages/SectionPage';
import { HOME, SECTIONS } from './shell/sections';
import { Shell, type RouteHandle } from './shell/Shell';
import { NotFound } from './states/States';

const handle = (title: string): RouteHandle => ({ title });

const routes: RouteObject[] = [
  {
    element: <Shell />,
    errorElement: <ErrorPage />,   // the shell itself failed
    children: [
      {
        errorElement: <ErrorPage />,   // a page failed: the shell stays
        children: [
          { index: true, element: <Navigate to={HOME} replace /> },
          ...SECTIONS.map(s => ({ path: s.path, element: <SectionPage section={s} />, handle: handle(s.title) })),
          { path: '*', element: <NotFound back={{ to: HOME, label: 'Back to leads' }} />, handle: handle('Page not found') },
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
