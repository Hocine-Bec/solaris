import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ArrowLeft, Building2, ChevronDown, CircleAlert, House, LoaderCircle } from 'lucide-react';
import { Icon } from '../components/Icon';
import { InfoNotice } from '../components/Notice';
import { STATES } from '../content/states';
import { prefersReducedMotion } from '../lib/motion';
import { AddressField } from './AddressField';
import { buildLeadRequest } from './lead';
import { sendLead, type Problem } from './submit';
import { EMPTY_VALUES, STEP_FIELDS, stepOf, type FieldErrors, type FieldName, type PickedAddress, type QuoteValues } from './types';
import { firstError, validateStep } from './validation';

type Summary = { title: string; problems: Problem[] };
export type SentState = { email: string; time: string };

const ErrorText = ({ id, message }: { id: string; message?: string }) =>
  message ? <p className="field-error" id={id}><Icon icon={CircleAlert} /><span>{message}</span></p> : null;

type TextFieldProps = {
  name: FieldName; label: string; value: string; error?: string; help?: string; optional?: boolean;
  onChange: (name: FieldName, value: string) => void;
} & Omit<React.InputHTMLAttributes<HTMLInputElement>, 'name' | 'value' | 'onChange'>;

function TextField({ name, label, value, error, help, onChange, ...input }: TextFieldProps) {
  const id = `q-${name}`;
  const describedBy = [help && `${id}-help`, error && `${id}-err`].filter(Boolean).join(' ') || undefined;
  return (
    <div className={`field${error ? ' has-error' : ''}`}>
      <label className="field-label" htmlFor={id}>{label}</label>
      <div className="control control-lg">
        <input id={id} name={name} value={value} aria-describedby={describedBy} aria-invalid={error ? true : undefined}
          onChange={e => onChange(name, e.target.value)} {...input} />
      </div>
      {help && <p className="field-help" id={`${id}-help`}>{help}</p>}
      <ErrorText id={`${id}-err`} message={error} />
    </div>
  );
}

type ChoiceProps = {
  name: FieldName; legend: string; value: string; error?: string; help?: string; columns?: string; className: string;
  options: { value: string; label: ReactNode; icon?: typeof House }[];
  onChange: (name: FieldName, value: string) => void;
};

function Choice({ name, legend, value, error, help, columns, className, options, onChange }: ChoiceProps) {
  const id = `q-${name}`;
  const describedBy = [help && `${id}-help`, error && `${id}-err`].filter(Boolean).join(' ') || undefined;
  return (
    <fieldset className={`field${error ? ' has-error' : ''}`} id={`${id}-set`} aria-describedby={describedBy}>
      <legend className="field-label" style={{ marginBottom: '.5rem' }}>{legend}</legend>
      {help && <p className="field-help" id={`${id}-help`} style={{ margin: '-.2rem 0 .5rem' }}>{help}</p>}
      <div className={`cards-choice ${className}`} style={columns ? { gridTemplateColumns: columns } : undefined}>
        {options.map((option, i) => (
          <label className="card-opt" key={option.value}>
            <input type="radio" name={name} value={option.value} id={i === 0 ? id : undefined} checked={value === option.value}
              onChange={() => onChange(name, option.value)} />
            {option.icon && <Icon icon={option.icon} />}
            {option.label}
          </label>
        ))}
      </div>
      {error && <p className="fieldset-err" id={`${id}-err`}><Icon icon={CircleAlert} /><span>{error}</span></p>}
    </fieldset>
  );
}

const STEP_NAMES = ['Contact', 'Property', 'Last details'];
const STEP_TITLES = ['Who should we contact?', 'Where is the property?', 'Last details'];

/** The quote form: three steps, checked one at a time, then sent to the API as a new lead. */
export function QuotePage() {
  const navigate = useNavigate();
  const [step, setStep] = useState(0);
  const [values, setValues] = useState<QuoteValues>(EMPTY_VALUES);
  const [errors, setErrors] = useState<FieldErrors>({});
  const [picked, setPicked] = useState<PickedAddress | null>(null);
  const [sending, setSending] = useState(false);
  const [summary, setSummary] = useState<Summary | null>(null);
  const formRef = useRef<HTMLFormElement>(null);
  const summaryRef = useRef<HTMLDivElement>(null);
  const headingRefs = useRef<(HTMLHeadingElement | null)[]>([]);
  const userMoved = useRef(false);
  const focusField = useRef<FieldName | null>(null);

  // After a step change: its heading takes the focus, and the form scrolls into view if the visitor moved.
  useEffect(() => {
    headingRefs.current[step]?.focus({ preventScroll: true });
    if (!userMoved.current) return;
    const top = formRef.current!.getBoundingClientRect().top + scrollY - 110;
    scrollTo({ top, behavior: prefersReducedMotion() ? 'auto' : 'smooth' });
  }, [step]);

  // After a check or a refused answer: the field to fix takes the focus.
  useEffect(() => {
    const name = focusField.current;
    if (!name) return;
    focusField.current = null;
    document.getElementById(`q-${name}`)?.focus();
  });

  useEffect(() => {
    if (!summary) return;
    summaryRef.current?.focus({ preventScroll: true });
    summaryRef.current?.scrollIntoView({ block: 'center' });
  }, [summary]);

  const change = (name: FieldName, value: string) => {
    const next = { ...values, [name]: value };
    setValues(next);
    // A field shown with an error is checked again as it changes.
    if (errors[name]) {
      const fresh = validateStep(step, next);
      setErrors(current => {
        const kept = { ...current };
        for (const field of STEP_FIELDS[step]) delete kept[field];
        return { ...kept, ...fresh };
      });
    }
  };

  const pickAddress = (address: PickedAddress) => {
    setPicked(address);
    setValues(current => ({ ...current, street: address.street, city: address.city, state: address.state, zip: address.zip }));
    setErrors(current => {
      const rest = { ...current };
      for (const field of ['street', 'city', 'state', 'zip'] as const) delete rest[field];
      return rest;
    });
  };

  const show = (next: number) => {
    userMoved.current = true;
    setSummary(null);
    setStep(next);
  };

  const jumpTo = (problem: Problem) => {
    if (!problem.field) return;
    const target = stepOf(problem.field);
    setErrors(current => ({ ...current, [problem.field!]: problem.message }));
    focusField.current = problem.field;
    if (target !== step) show(target); else setSummary(null);
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    const found = validateStep(step, values);
    setErrors(current => {
      const kept = { ...current };
      for (const field of STEP_FIELDS[step]) delete kept[field];
      return { ...kept, ...found };
    });
    const first = firstError(step, found);
    if (first) { focusField.current = first; return; }
    if (step < 2) { show(step + 1); return; }

    setSending(true);
    const outcome = await sendLead(buildLeadRequest(values, picked));
    setSending(false);
    if (outcome.kind === 'sent') {
      navigate('/quote/sent', { state: { email: values.email.trim(), time: values.time } satisfies SentState });
    } else if (outcome.kind === 'invalid') {
      setErrors(current => ({ ...current, ...Object.fromEntries(outcome.problems.filter(p => p.field).map(p => [p.field, p.message])) }));
      const count = outcome.problems.length;
      setSummary({ title: count === 1 ? 'One answer needs a change' : `${count} answers need a change`, problems: outcome.problems });
    } else {
      setSummary({ title: outcome.title, problems: [{ message: outcome.message }] });
    }
  };

  const err = (name: FieldName) => errors[name];

  return (
    <section className="band" data-theme="limestone" style={{ paddingTop: 'clamp(7.5rem,18vh,10rem)' }}>
      <div className="wrap quote-grid">
        <div className="quote-side">
          <p className="label eyebrow">Get a free quote</p>
          <h1 className="display">Three short steps<i className="dot" /></h1>
          <p className="body">Tell us who you are, where the property is, and a little about your electricity bill.</p>
          <ol className="next-list">
            <li><span className="n">01</span><span>We email you a copy of your request.</span></li>
            <li><span className="n">02</span><span>We contact you at the time of day you choose.</span></li>
            <li><span className="n">03</span><span>If you want to go ahead, we book a site survey.</span></li>
          </ol>
          <InfoNotice title="This is a demo">
            Solaris Energy is fictional. Your request is stored and you get a confirmation email, but nobody contacts you. Use an address you're happy to receive that email at.
          </InfoNotice>
        </div>

        <form className="form-card" ref={formRef} noValidate onSubmit={submit}>
          <ol className="steps" aria-label="Your progress">
            {STEP_NAMES.map((name, i) => (
              <li key={name} className={i < step ? 'done' : undefined} aria-current={i === step ? 'step' : undefined}>
                <span>0{i + 1}</span>{name}
              </li>
            ))}
          </ol>

          {summary && (
            <div className="notice tone-negative" role="alert" tabIndex={-1} ref={summaryRef}>
              <Icon icon={CircleAlert} />
              <p className="notice-title">{summary.title}</p>
              <ul>
                {summary.problems.map((problem, i) => (
                  <li key={i}>
                    {problem.field
                      ? <a href={`#q-${problem.field}`} onClick={e => { e.preventDefault(); jumpTo(problem); }}>{problem.message}</a>
                      : problem.message}
                  </li>
                ))}
              </ul>
            </div>
          )}

          <div className="form-step" hidden={step !== 0}>
            <h2 tabIndex={-1} ref={el => { headingRefs.current[0] = el; }}>{STEP_TITLES[0]}</h2>
            <div className="two">
              <TextField name="firstName" label="First name" autoComplete="given-name" value={values.firstName} error={err('firstName')} onChange={change} />
              <TextField name="lastName" label="Last name" autoComplete="family-name" value={values.lastName} error={err('lastName')} onChange={change} />
            </div>
            <TextField name="email" label="Email" type="email" autoComplete="email" inputMode="email" help="We send a copy of your request here." value={values.email} error={err('email')} onChange={change} />
            <TextField name="phone" label="Phone" type="tel" autoComplete="tel" inputMode="tel" value={values.phone} error={err('phone')} onChange={change} />
          </div>

          <div className="form-step" hidden={step !== 1}>
            <h2 tabIndex={-1} ref={el => { headingRefs.current[1] = el; }}>{STEP_TITLES[1]}</h2>
            <AddressField value={values.street} error={err('street')} onChange={street => change('street', street)} onPick={pickAddress} />
            <div className="two">
              <TextField name="city" label="City" autoComplete="address-level2" value={values.city} error={err('city')} onChange={change} />
              <TextField name="zip" label="ZIP code" autoComplete="postal-code" inputMode="numeric" value={values.zip} error={err('zip')} onChange={change} />
            </div>
            <div className={`field${err('state') ? ' has-error' : ''}`}>
              <label className="field-label" htmlFor="q-state">State</label>
              <div className="control control-lg">
                <select id="q-state" name="state" autoComplete="address-level1" value={values.state}
                  aria-describedby={err('state') ? 'q-state-err' : undefined} aria-invalid={err('state') ? true : undefined}
                  onChange={e => change('state', e.target.value)}>
                  <option value="">Choose a state</option>
                  {STATES.map(([code, name]) => <option key={code} value={code}>{name}</option>)}
                </select>
                <Icon icon={ChevronDown} className="chev" />
              </div>
              <ErrorText id="q-state-err" message={err('state')} />
            </div>
            <Choice name="type" legend="Type of property" className="c3" value={values.type} error={err('type')} onChange={change}
              options={[
                { value: 'House', label: 'House', icon: House },
                { value: 'Apartment', label: 'Apartment', icon: Building2 },
                { value: 'Commercial', label: 'Business premises', icon: Building2 },
              ]} />
            <Choice name="own" legend="Do you own it?" className="c3" columns="repeat(2,1fr)" value={values.own} error={err('own')} onChange={change}
              options={[{ value: 'yes', label: 'I own it' }, { value: 'no', label: 'I rent or lease it' }]} />
          </div>

          <div className="form-step" hidden={step !== 2}>
            <h2 tabIndex={-1} ref={el => { headingRefs.current[2] = el; }}>{STEP_TITLES[2]}</h2>
            <Choice name="bill" legend="Your usual monthly electricity bill" className="c4" value={values.bill} error={err('bill')} onChange={change}
              help="A rough idea is enough. It tells us how big a system to look at."
              options={[
                { value: 'under100', label: 'Under $100' },
                { value: '100-200', label: '$100 to $200' },
                { value: '200-300', label: '$200 to $300' },
                { value: '300plus', label: 'Over $300' },
              ]} />
            <Choice name="time" legend="When should we contact you?" className="c3" value={values.time} error={err('time')} onChange={change}
              options={[
                { value: 'Morning', label: <span>Morning<small>8 am to 12 pm</small></span> },
                { value: 'Afternoon', label: <span>Afternoon<small>12 to 5 pm</small></span> },
                { value: 'Evening', label: <span>Evening<small>5 to 8 pm</small></span> },
              ]} />
            <div className="field">
              <label className="field-label" htmlFor="q-notes">Anything we should know? <span className="opt">Optional</span></label>
              <div className="control control-area">
                <textarea id="q-notes" name="notes" value={values.notes} onChange={e => change('notes', e.target.value)}
                  placeholder="Shade from a big tree, a roof that's due for repair, a question…" />
              </div>
            </div>
            <p className="a-small">We use these details only to contact you about this request. <Link className="link" to="/privacy">Privacy</Link></p>
          </div>

          <div className="form-nav">
            <button className="btn btn-secondary btn-lg" type="button" hidden={step === 0} onClick={() => show(step - 1)}><Icon icon={ArrowLeft} />Back</button>
            <span />
            <button className={`btn btn-primary btn-lg${sending ? ' is-loading' : ''}`} type="submit" aria-busy={sending || undefined}>
              <Icon icon={LoaderCircle} className="spin" />
              <span className="btn-text">{step === 2 ? 'Send my request' : 'Continue'}</span>
            </button>
          </div>
        </form>
      </div>
    </section>
  );
}
