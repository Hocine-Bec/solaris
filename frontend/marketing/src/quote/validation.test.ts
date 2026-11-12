import { describe, expect, it } from 'vitest';
import { EMPTY_VALUES, type QuoteValues } from './types';
import { firstError, OWNER_RULE, validateStep } from './validation';

const complete: QuoteValues = {
  firstName: 'Jane', lastName: 'Doe', email: 'jane.doe@example.com', phone: '(512) 555-0134',
  street: '2104 Oak Hollow Dr', city: 'Austin', zip: '78704', state: 'TX',
  type: 'House', own: 'yes', bill: '100-200', time: 'Morning', notes: '',
};

describe('validateStep', () => {
  it.each([0, 1, 2])('passes a complete step %i', step => {
    expect(validateStep(step, complete)).toEqual({});
  });

  it('asks for every contact detail on an empty first step', () => {
    expect(validateStep(0, EMPTY_VALUES)).toEqual({
      firstName: 'Enter your first name.',
      lastName: 'Enter your last name.',
      email: 'Enter an email address, like name@example.com.',
      phone: 'Enter a phone number, like (512) 555-0134.',
    });
  });

  it.each(['7870', '787040', 'ABCDE', '78704-12'])('refuses the ZIP code %s', zip => {
    expect(validateStep(1, { ...complete, zip }).zip).toBe('Enter a 5-digit ZIP code, like 78704.');
  });

  it.each(['78704', '78704-1234'])('accepts the ZIP code %s', zip => {
    expect(validateStep(1, { ...complete, zip }).zip).toBeUndefined();
  });

  it('refuses a state that is not on the list (FR-L1)', () => {
    expect(validateStep(1, { ...complete, state: 'PR' }).state).toBe('Choose the state.');
  });

  it.each(['House', 'Apartment'] as const)('tells the renter of a %s that the owner has to ask', type => {
    expect(validateStep(1, { ...complete, type, own: 'no' }).own).toBe(OWNER_RULE);
  });

  it('lets a business that leases its premises ask', () => {
    expect(validateStep(1, { ...complete, type: 'Commercial', own: 'no' })).toEqual({});
  });

  it('leaves the notes optional', () => {
    expect(validateStep(2, { ...complete, notes: '' })).toEqual({});
  });

  it('names the first field to fix in the order the step shows them', () => {
    expect(firstError(1, validateStep(1, { ...complete, city: '', type: '' }))).toBe('city');
  });
});
