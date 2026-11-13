import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { Navigate, useLocation } from 'react-router-dom';
import { Shell } from '../shell/Shell';
import { Failed } from '../states/States';
import { CurrentUserContext, loginPath, REASON_KEY, useSession, type SignInReason } from './session';

/**
 * The screens behind the session check: the frame, once the API says who is signed in. With no session, the
 * sign-in page opens, remembering this page; if the session ended while the person was here, it says so. It is the
 * only place that sends a signed-in person to the sign-in page, so signing out and a session that ends can't race.
 */
export function RequireSession() {
  const session = useSession();
  const queryClient = useQueryClient();
  const { pathname, search } = useLocation();
  // Adjusted during rendering, so the render in which the session disappears already knows there was one.
  const [wasSignedIn, setWasSignedIn] = useState(false);
  if (session.data && !wasSignedIn) setWasSignedIn(true);

  if (session.data) {
    return (
      <CurrentUserContext value={session.data}>
        <Shell />
      </CurrentUserContext>
    );
  }
  if (session.data === null) {
    // Signed out on purpose: the sign-in page, saying so. Otherwise, back here after signing in, saying the session
    // ended if there was one.
    const reason = queryClient.getQueryData<SignInReason>(REASON_KEY) ?? (wasSignedIn ? 'ended' : null);
    const state: { reason: SignInReason } | null = reason ? { reason } : null;
    return <Navigate replace to={reason === 'out' ? '/login' : loginPath(pathname + search)} state={state} />;
  }
  if (session.isError) {
    return (
      <main className="login-page">
        <Failed what="the staff app" error={session.error} retrying={session.isFetching} onRetry={() => void session.refetch()} />
      </main>
    );
  }
  return <div className="session-wait" aria-busy="true"><p className="sr" role="status">Loading</p></div>;
}
