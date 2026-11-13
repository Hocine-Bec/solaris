import { ChartColumn, ClipboardCheck, House, Inbox, Mail, SolarPanel, Users, type LucideIcon } from 'lucide-react';
import type { Role } from '../api/auth';

export type Section = {
  path: string;
  /** In the navigation */
  label: string;
  /** The page's title */
  title: string;
  icon: LucideIcon;
  /** What the screen will hold, shown until it is built */
  holds: string;
};

/** The office's sections, in the navigation's order */
export const OFFICE: readonly Section[] = [
  { path: '/leads', label: 'Leads', title: 'Leads', icon: Inbox, holds: 'Every lead, newest first, filtered by status, a page at a time.' },
  { path: '/customers', label: 'Customers', title: 'Customers', icon: House, holds: 'Every customer, searched by name or email.' },
  { path: '/installations', label: 'Installations', title: 'Installations', icon: SolarPanel, holds: 'Every installation, filtered by stage.' },
  { path: '/production', label: 'Production', title: 'Production', icon: ChartColumn, holds: 'For the last complete day: how many installations are at each health level, and the 20 doing worst.' },
  { path: '/notifications', label: 'Notifications', title: 'Failed notifications', icon: Mail, holds: 'The emails that still failed after 24 hours of retries, each ready to retry.' },
  { path: '/users', label: 'Users', title: 'Users', icon: Users, holds: 'Every staff account, its role, and whether it is active.' },
];

/** A technician's whole app: the installations they are assigned to */
export const MY_WORK: Section = {
  path: '/my-work', label: 'My work', title: 'My work', icon: ClipboardCheck,
  holds: "The installations you're assigned to: the address, your role, and whether your part is done.",
};

/** The sections an account sees in the navigation. The API checks the role on every request; this only hides links. */
export const sectionsFor = (role: Role): readonly Section[] => (role === 'Admin' ? OFFICE : [MY_WORK]);

/**
 * Whether an address belongs to a section this role sees: after a session ends on a shared computer, the next person
 * to sign in may be someone else, and the page the last one left isn't theirs to return to.
 */
export const opensFor = (role: Role, path: string) =>
  sectionsFor(role).some(s => path === s.path || path.startsWith(`${s.path}/`) || path.startsWith(`${s.path}?`));
