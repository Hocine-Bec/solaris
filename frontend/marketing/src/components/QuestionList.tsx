import { useRef } from 'react';
import { Plus } from 'lucide-react';
import type { Question } from '../content/questions';
import { prefersReducedMotion } from '../lib/motion';
import { Icon } from './Icon';

const EASE = 'cubic-bezier(.2, .8, .3, 1)';

/** A question that opens with a short height-and-fade animation: 0.3 s to open, 0.2 s to close, instant with reduced motion. */
function QuestionItem({ question }: { question: Question }) {
  const detailsRef = useRef<HTMLDetailsElement>(null);
  const answerRef = useRef<HTMLDivElement>(null);
  const animation = useRef<Animation | null>(null);

  const toggle = (e: React.MouseEvent) => {
    e.preventDefault();
    const details = detailsRef.current!, answer = answerRef.current!;
    animation.current?.cancel();
    const opening = details.dataset.open !== 'true';
    details.dataset.open = String(opening);
    if (prefersReducedMotion()) { details.open = opening; return; }
    if (opening) {
      details.open = true;
      const h = answer.offsetHeight;
      animation.current = answer.animate([{ height: '0px', opacity: 0 }, { height: `${h}px`, opacity: 1 }], { duration: 300, easing: EASE });
    } else {
      const h = answer.offsetHeight;
      const closing = answer.animate([{ height: `${h}px`, opacity: 1 }, { height: '0px', opacity: 0 }], { duration: 200, easing: EASE, fill: 'forwards' });
      animation.current = closing;
      closing.onfinish = () => { details.open = false; closing.cancel(); };
    }
  };

  return (
    <details ref={detailsRef}>
      <summary onClick={toggle}>{question.question}<Icon icon={Plus} /></summary>
      <div className="a" ref={answerRef}><div className="a-in"><p>{question.answer}</p></div></div>
    </details>
  );
}

export function QuestionList({ questions, className = '' }: { questions: Question[]; className?: string }) {
  return <div className={`qa ${className}`.trim()}>{questions.map(q => <QuestionItem key={q.question} question={q} />)}</div>;
}
