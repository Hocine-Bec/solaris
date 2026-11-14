import type { ReactNode } from 'react';
import type { UseQueryResult } from '@tanstack/react-query';
import { Failed, Loading, NotAllowed } from './States';
import { viewOf } from './view';

type Props<T> = {
  query: UseQueryResult<T>;
  /** What the screen loads, for "We couldn't load …": "the leads", "this installation" */
  what: string;
  /** What a failure didn't touch: "Your filters are kept." */
  note?: string;
  isEmpty?: (data: T) => boolean;
  /** The screen's own Empty */
  empty?: ReactNode;
  /** A record's NotFound */
  notFound?: ReactNode;
  children: (data: T) => ReactNode;
};

/** One load's states, in one place: each screen gives its words and draws only its data. */
export function QueryView<T>({ query, what, note, isEmpty, empty, notFound, children }: Props<T>) {
  switch (viewOf(query, isEmpty)) {
    case 'loading': return <Loading />;
    case 'failed': return <Failed what={what} error={query.error} note={note} retrying={query.isFetching} onRetry={() => void query.refetch()} />;
    case 'notFound': return notFound;
    case 'forbidden': return <NotAllowed />;
    case 'empty': return empty;
    case 'ready': return children(query.data as T);
  }
}
