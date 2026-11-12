import type { ReactNode } from 'react';
import { Link } from 'react-router-dom';

export type Question = {
  group: string;
  question: string;
  answer: ReactNode;
  /** Also on the home page */
  top?: boolean;
};

export const QUESTIONS: Question[] = [
  {
    group: 'Before you ask',
    question: 'Who can get solar with you?',
    answer: 'Homes and small businesses anywhere in the United States. For a house or an apartment, the owner has to ask; a business can ask whether it owns or leases its premises.',
    top: true,
  },
  {
    group: 'Before you ask',
    question: 'I rent my home. Can I still ask?',
    answer: 'The owner has to agree to work on the roof, so the owner should send the request. Business premises are the exception: a business that leases can ask.',
  },
  {
    group: 'Before you ask',
    question: 'How much does it cost?',
    answer: 'It depends on your roof, your electricity use, and the system that fits both, so there is no price list. The design step works out the system, and you know its price before any work on your roof starts.',
    top: true,
  },
  {
    group: 'Before you ask',
    question: 'What happens after I send the form?',
    answer: 'We email you a copy of your request, then contact you at the time of day you chose. If you want to go ahead, we book a site survey. Nothing is booked until you say so.',
    top: true,
  },
  {
    group: 'The steps',
    question: 'How long does it take?',
    answer: 'Most of the time goes to Permits: your city or county and the utility approve the plans, and that can take weeks. The other steps are quicker. You can see each one on the How it works page.',
    top: true,
  },
  {
    group: 'The steps',
    question: 'Do I need to be home for the survey?',
    answer: 'Yes, or someone who can let the surveyor in. They need to see the roof and your electrical panel.',
  },
  {
    group: 'The steps',
    question: "What if my roof isn't suitable?",
    answer: "The survey checks its condition, which way it faces and the shade on it. If solar isn't a good fit, we tell you, and that's the end of it.",
  },
  {
    group: 'After switch-on',
    question: "How do I know it's working?",
    answer: "We compare your system's production with what it should make, every day, using your state's sunlight for the time of year.",
  },
  {
    group: 'Your details',
    question: 'What do you do with my details?',
    answer: (
      <>
        We use them to contact you about your request and, if you go ahead, to run your installation.{' '}
        <Link className="link" to="/privacy">Privacy</Link> has the details.
      </>
    ),
  },
];
