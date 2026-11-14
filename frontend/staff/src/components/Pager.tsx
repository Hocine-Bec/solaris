import { Link } from 'react-router-dom';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Icon } from './Icon';

type Props = {
  /** "Pages of users" */
  label: string;
  page: number;
  pageSize: number;
  totalCount: number;
  /** The address of another page */
  hrefFor: (page: number) => string;
};

/** "21–40 of 263", with Previous and Next; nothing when everything fits on one page. */
export function Pager({ label, page, pageSize, totalCount, hrefFor }: Props) {
  if (totalCount <= pageSize && page === 1) return null;
  const first = (page - 1) * pageSize + 1;
  const last = Math.min(totalCount, page * pageSize);
  const hasNext = page * pageSize < totalCount;
  // Past the end (a page that no longer exists): back to the last one
  const lastPage = Math.max(1, Math.ceil(totalCount / pageSize));
  const previous = first > totalCount ? lastPage : page - 1;
  return (
    <nav className="pager" aria-label={label}>
      <p className="a-data">{first > totalCount ? `Nothing on page ${page}` : `${first}–${last} of ${totalCount}`}</p>
      <div>
        {page > 1 && <Link className="btn btn-secondary btn-sm" to={hrefFor(previous)}><Icon icon={ChevronLeft} />Previous</Link>}
        {hasNext && <Link className="btn btn-secondary btn-sm" to={hrefFor(page + 1)}>Next<Icon icon={ChevronRight} /></Link>}
      </div>
    </nav>
  );
}
