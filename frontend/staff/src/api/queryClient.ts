import { QueryClient } from '@tanstack/react-query';
import { ApiError } from './errors';

/**
 * A failed load is tried once more when the network or the server failed, since that may pass; an answer such as
 * 404 or 403 is final. Changes are never retried (TanStack Query's default): repeating one could apply it twice.
 */
export const shouldRetry = (failures: number, error: unknown) =>
  failures < 1 && error instanceof ApiError && (error.kind === 'unreachable' || error.kind === 'server');

export function createQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: shouldRetry },
    },
  });
}
