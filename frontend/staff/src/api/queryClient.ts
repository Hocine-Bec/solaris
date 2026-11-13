import { MutationCache, QueryCache, QueryClient } from '@tanstack/react-query';
import { SESSION_KEY } from '../session/session';
import { ApiError } from './errors';

/**
 * A failed load is tried once more when the network or the server failed, since that may pass; an answer such as
 * 404 or 403 is final. Changes are never retried (TanStack Query's default): repeating one could apply it twice.
 */
export const shouldRetry = (failures: number, error: unknown) =>
  failures < 1 && error instanceof ApiError && (error.kind === 'unreachable' || error.kind === 'server');

const endsSession = (error: unknown) => error instanceof ApiError && error.kind === 'signedOut';

/**
 * Any request answered 401 means the session is over (ended, or the account deactivated): the session is
 * forgotten, and the screens behind the session check open the sign-in page, saying so. Signing in itself answers
 * 401 to wrong credentials, which says nothing about a session, so the sign-in form handles its own answer.
 */
export function createQueryClient() {
  const client: QueryClient = new QueryClient({
    queryCache: new QueryCache({
      onError: (error, query) => {
        if (endsSession(error) && query.queryKey[0] !== SESSION_KEY[0]) client.setQueryData(SESSION_KEY, null);
      },
    }),
    mutationCache: new MutationCache({
      onError: error => {
        if (endsSession(error)) client.setQueryData(SESSION_KEY, null);
      },
    }),
    defaultOptions: {
      queries: { retry: shouldRetry },
    },
  });
  return client;
}
