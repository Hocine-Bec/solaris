import { Navigate } from 'react-router-dom';
import { landingFor, useCurrentUser } from '../session/session';

/** The app's root opens the account's landing page: Leads for the office, My work for technicians. */
export function LandingRedirect() {
  return <Navigate to={landingFor(useCurrentUser().role)} replace />;
}
