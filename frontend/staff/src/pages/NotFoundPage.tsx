import { landingFor, useCurrentUser } from '../session/session';
import { NotFound } from '../states/States';

/** An address the app doesn't have, inside the frame, with the way back to the account's landing page. */
export function NotFoundPage() {
  const { role } = useCurrentUser();
  return <NotFound back={{ to: landingFor(role), label: role === 'Admin' ? 'Back to leads' : 'Back to my work' }} />;
}
