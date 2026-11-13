import { ChartColumn, House, Inbox, Mail, SolarPanel, Users, type LucideIcon } from 'lucide-react';

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
export const SECTIONS: readonly Section[] = [
  { path: '/leads', label: 'Leads', title: 'Leads', icon: Inbox, holds: 'Every lead, newest first, filtered by status, a page at a time.' },
  { path: '/customers', label: 'Customers', title: 'Customers', icon: House, holds: 'Every customer, searched by name or email.' },
  { path: '/installations', label: 'Installations', title: 'Installations', icon: SolarPanel, holds: 'Every installation, filtered by stage.' },
  { path: '/production', label: 'Production', title: 'Production', icon: ChartColumn, holds: 'For the last complete day: how many installations are at each health level, and the 20 doing worst.' },
  { path: '/notifications', label: 'Notifications', title: 'Failed notifications', icon: Mail, holds: 'The emails that still failed after 24 hours of retries, each ready to retry.' },
  { path: '/users', label: 'Users', title: 'Users', icon: Users, holds: 'Every staff account, its role, and whether it is active.' },
];

/** Where the app opens: an office's day starts with the new leads, as a quick reply wins the customer. */
export const HOME = '/leads';
