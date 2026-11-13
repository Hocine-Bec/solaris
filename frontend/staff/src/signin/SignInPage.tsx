import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQueryClient } from '@tanstack/react-query';
import { Navigate, useLocation, useSearchParams } from 'react-router-dom';
import { CircleAlert, CircleCheck, Clock, CloudOff, LoaderCircle, LogIn, type LucideIcon } from 'lucide-react';
import { signIn } from '../api/auth';
import { Icon } from '../components/Icon';
import { landingFor, REASON_KEY, safeReturnTo, SESSION_KEY, useSession, type SignInReason } from '../session/session';
import { opensFor } from '../shell/sections';
import { noticeFor, signInSchema, type Notice, type SignInValues } from './form';
import { PasswordField } from './PasswordField';

const NOTICES: Record<Exclude<Notice['kind'], 'error'>, { tone: string; icon: LucideIcon; title?: string; text: string }> = {
  ended: { tone: 'info', icon: LogIn, title: 'Your session ended', text: "Sign in again to go back to the page you were on. Anything you hadn't saved is lost." },
  out: { tone: 'positive', icon: CircleCheck, text: "You're signed out." },
  refused: { tone: 'negative', icon: CircleAlert, text: 'The email or password is incorrect.' },
  limited: { tone: 'warning', icon: Clock, text: 'Too many sign-in attempts. Wait a minute, then try again.' },
  down: { tone: 'negative', icon: CloudOff, text: "The server didn't answer. Check your connection, then try again." },
};

function SignInNotice({ notice }: { notice: Notice }) {
  const n = notice.kind === 'error' ? { tone: 'negative', icon: CircleAlert, title: undefined, text: notice.text } : NOTICES[notice.kind];
  // A refusal or a failure is read out at once; the others are a quiet status.
  const urgent = n.tone === 'negative' || n.tone === 'warning';
  return (
    <div className={`notice tone-${n.tone}`} role={urgent ? 'alert' : 'status'}>
      <Icon icon={n.icon} />
      {n.title && <p className="notice-title">{n.title}</p>}
      <p>{n.text}</p>
    </div>
  );
}

/**
 * The sign-in page, from the approved design: one card, in the chosen theme. After a refusal the email stays, the
 * password is cleared and hidden, and focus moves to it. Signing in goes back to the page the session ended on, or to
 * the account's landing page.
 */
export function SignInPage() {
  const session = useSession();
  const queryClient = useQueryClient();
  const location = useLocation();
  const [params] = useSearchParams();
  const reason = (location.state as { reason?: SignInReason } | null)?.reason;
  const [notice, setNotice] = useState<Notice | null>(reason ? { kind: reason } : null);
  const [passwordShown, setPasswordShown] = useState(false);
  const sending = useRef(false);
  const { register, handleSubmit, setValue, setFocus, formState: { errors, isSubmitting } } = useForm<SignInValues>({
    resolver: zodResolver(signInSchema),
    defaultValues: { email: '', password: '' },
  });

  useEffect(() => { document.title = 'Sign in · Solaris'; }, []);
  // The reason for signing out has been shown: a later session that ends must say it ended.
  useEffect(() => { queryClient.removeQueries({ queryKey: REASON_KEY }); }, [queryClient]);

  if (session.data) {
    // Back to the page the session ended on, if it is one this account sees; otherwise its landing page.
    const back = safeReturnTo(params.get('returnTo'));
    const role = session.data.role;
    return <Navigate replace to={back && opensFor(role, back) ? back : landingFor(role)} />;
  }
  if (session.isPending) return <div className="session-wait" aria-busy="true"><p className="sr" role="status">Loading</p></div>;

  const submit = async ({ email, password }: SignInValues) => {
    if (sending.current) return;   // one press, one request
    sending.current = true;
    setNotice(null);
    try {
      // The account becomes the session; this page then sends the person on (above).
      queryClient.setQueryData(SESSION_KEY, await signIn(email, password));
    } catch (error) {
      const next = noticeFor(error);
      setNotice(next);
      if (next.kind === 'refused') {
        setValue('password', '');
        setPasswordShown(false);
        setFocus('password');
      }
    } finally {
      sending.current = false;
    }
  };

  return (
    <main className="login-page">
      <div className="signin">
        <span className="logo">Solaris<i className="dot" aria-hidden="true" /></span>
        <div className="signin-card">
          <div className="signin-head">
            <h1 className="a-title">Sign in</h1>
            <p className="a-small">With your staff account's email and password.</p>
          </div>
          {notice && <SignInNotice notice={notice} />}
          <form className="signin-form" noValidate onSubmit={e => void handleSubmit(submit)(e)}>
            <div className={`field${errors.email ? ' has-error' : ''}`}>
              <label className="field-label" htmlFor="si-email">Email</label>
              <div className="control">
                <input
                  id="si-email" type="email" autoComplete="username" autoCapitalize="none" spellCheck={false} autoFocus
                  aria-invalid={errors.email ? true : undefined} aria-describedby="si-email-err" {...register('email')}
                />
              </div>
              {errors.email && <p className="field-error" id="si-email-err"><Icon icon={CircleAlert} /><span>{errors.email.message}</span></p>}
            </div>
            <PasswordField
              id="si-pass" registration={register('password')} error={errors.password?.message}
              shown={passwordShown} onShownChange={setPasswordShown}
            />
            <button className={`btn btn-primary btn-lg signin-btn${isSubmitting ? ' is-loading' : ''}`} type="submit" aria-busy={isSubmitting || undefined}>
              <Icon icon={LoaderCircle} className="spin" />
              <span className="btn-text">Sign in</span>
            </button>
          </form>
          <p className="a-small signin-help">Forgot your password? Ask an admin to set a new one.</p>
        </div>
      </div>
    </main>
  );
}
