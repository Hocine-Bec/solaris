// The six steps, as approved in the design: the home page's list and the How it works page.
import type { LucideIcon } from 'lucide-react';
import { ClipboardCheck, DraftingCompass, HardHat, ScanSearch, Stamp, Zap } from 'lucide-react';

export type Step = {
  name: string;
  icon: LucideIcon;
  /** One line, for the home page's list */
  short: string;
  who: string;
  what: string;
  you: string;
};

export const STEPS: Step[] = [
  {
    name: 'Survey',
    icon: ScanSearch,
    short: 'A surveyor visits and checks your roof, the shade on it and your electrical panel.',
    who: 'A surveyor.',
    what: "They check the roof's condition, which way it faces, the shade on it, and your electrical panel.",
    you: 'Be there to let them in, and show them the electrical panel.',
  },
  {
    name: 'Design',
    icon: DraftingCompass,
    short: 'We work out how many panels, which inverter and the size of the system.',
    who: 'Nobody: it happens at our desk.',
    what: 'From the survey, we work out how many panels fit, which inverter, and the size of the system in kilowatts.',
    you: 'Nothing, unless we have a question.',
  },
  {
    name: 'Permits',
    icon: Stamp,
    short: 'Your city or county and the utility approve the plans. This step can take weeks.',
    who: 'Nobody.',
    what: 'Your city or county and the utility check the plans and approve them. We send the plans.',
    you: 'Nothing. This is the step that takes longest: it can take weeks.',
  },
  {
    name: 'Installation',
    icon: HardHat,
    short: 'A crew installs the panels and the inverter.',
    who: 'An installation crew.',
    what: 'They fix the rails to the roof, mount the panels, and install and wire the inverter.',
    you: 'Make sure they can reach the roof and the electrical panel.',
  },
  {
    name: 'Inspection',
    icon: ClipboardCheck,
    short: 'An inspector checks that the work is safe and compliant.',
    who: 'An inspector.',
    what: 'They check that the installation is safe and meets the rules.',
    you: 'Let them in if they need to see the panel or the inverter.',
  },
  {
    name: 'Switch-on',
    icon: Zap,
    short: 'The system is switched on, and we start monitoring it.',
    who: 'Us.',
    what: "We switch the system on and start comparing its production with what it should make, every day. You get an email when it's on.",
    you: 'Nothing. Your system is producing.',
  },
];
