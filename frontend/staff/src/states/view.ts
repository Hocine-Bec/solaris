import type { UseQueryResult } from '@tanstack/react-query';
import { ApiError } from '../api/errors';

export type View = 'loading' | 'failed' | 'notFound' | 'forbidden' | 'empty' | 'ready';

/**
 * What a screen shows for a load. Data already shown stays when a later refresh fails, so a list doesn't vanish
 * because the network blinked; a 404 is "not found", a 403 "not allowed", any other failure "failed", with a way to
 * try again.
 */
export function viewOf<T>(query: Pick<UseQueryResult<T>, 'status' | 'error' | 'data'>, isEmpty?: (data: T) => boolean): View {
  if (query.data !== undefined) return isEmpty?.(query.data) ? 'empty' : 'ready';
  if (query.status !== 'error') return 'loading';
  const kind = query.error instanceof ApiError ? query.error.kind : null;
  return kind === 'notFound' ? 'notFound' : kind === 'forbidden' ? 'forbidden' : 'failed';
}
