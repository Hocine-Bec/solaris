import { describe, expect, it } from 'vitest';
import { editLeadSchema } from './forms';

const dana = { firstName: 'Dana', lastName: 'Whitfield', email: 'dana.whitfield@example.com', phone: '(512) 555-0187' };
const messages = (values: object) => {
  const result = editLeadSchema.safeParse({ ...dana, ...values });
  return result.success ? [] : result.error.issues.map(i => `${String(i.path[0])}: ${i.message}`);
};

describe('the Edit dialog', () => {
  it('accepts the contact details as the quote form takes them, trimmed', () => {
    expect(editLeadSchema.parse({ ...dana, firstName: ' Dana ' })).toEqual(dana);
  });

  it("says what each field needs, once each", () => {
    expect(messages({ firstName: '', lastName: 'W', email: 'dana', phone: 'call me' })).toEqual([
      'firstName: Enter their first name.',
      'lastName: The last name needs at least 2 characters.',
      'email: Enter an email, such as name@example.com.',
      'phone: Enter a phone number: digits, spaces, dashes and brackets, with + first if needed.',
    ]);
  });

  it('keeps the phone within its 20 characters', () => {
    expect(messages({ phone: '+1 (512) 555-0187 0000' })).toEqual(["phone: The phone number can't be longer than 20 characters."]);
  });
});
