import { QuestionList } from '../components/QuestionList';
import { QUESTIONS } from '../content/questions';

const GROUPS = [...new Set(QUESTIONS.map(q => q.group))];

export function QuestionsPage() {
  return (
    <>
      <section className="band page-hero" data-theme="night">
        <div className="wrap">
          <p className="label eyebrow">Questions</p>
          <h1 className="display">Questions<i className="dot" /></h1>
          <p className="lead">What people ask before, during and after. If yours isn't here, call or email us.</p>
        </div>
      </section>
      <section className="band" data-theme="limestone">
        <div className="wrap qa-groups">
          {GROUPS.map((group, i) => (
            <div className="qa-group" key={group}>
              <h2 className="label eyebrow">{String(i + 1).padStart(2, '0')} — {group}</h2>
              <QuestionList questions={QUESTIONS.filter(q => q.group === group)} />
            </div>
          ))}
        </div>
      </section>
    </>
  );
}
