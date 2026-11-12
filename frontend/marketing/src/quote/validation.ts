// The form's own checks, step by step, with the words the visitor sees. The API checks everything again; its
// messages are shown too (see submit.ts).
import { STATE_CODES } from '../content/states';
import { STEP_FIELDS, type FieldErrors, type QuoteValues } from './types';

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const PHONE = /^\+?[\d\s\-()]{7,}$/;
const ZIP = /^\d{5}(-\d{4})?$/;

export const OWNER_RULE = 'The owner has to agree to work on the roof, so the owner should send this request. Business premises are the exception.';

/** The errors of one step (0, 1 or 2); an empty object when the step is complete. */
export function validateStep(step: number, values: QuoteValues): FieldErrors {
  const v = (name: keyof QuoteValues) => values[name].trim();
  const errors: FieldErrors = {};
  const check = (name: keyof QuoteValues, message: string | false) => { if (message) errors[name] = message; };
  if (step === 0) {
    check('firstName', v('firstName').length < 2 && 'Enter your first name.');
    check('lastName', v('lastName').length < 2 && 'Enter your last name.');
    check('email', !EMAIL.test(v('email')) && 'Enter an email address, like name@example.com.');
    check('phone', !PHONE.test(v('phone')) && 'Enter a phone number, like (512) 555-0134.');
  } else if (step === 1) {
    check('street', !v('street') && 'Enter the street address.');
    check('city', !v('city') && 'Enter the city.');
    check('zip', !ZIP.test(v('zip')) && 'Enter a 5-digit ZIP code, like 78704.');
    check('state', !STATE_CODES.has(values.state) && 'Choose the state.');
    check('type', !values.type && 'Choose the type of property.');
    // The API's rule: only business premises may be leased.
    check('own', !values.own ? 'Tell us whether you own it.' : values.own === 'no' && values.type !== 'Commercial' && OWNER_RULE);
  } else {
    check('bill', !values.bill && 'Choose the closest amount.');
    check('time', !values.time && 'Choose a time of day.');
  }
  return errors;
}

/** The first field with an error, in the order the step shows them */
export const firstError = (step: number, errors: FieldErrors) => STEP_FIELDS[step].find(name => errors[name]);
