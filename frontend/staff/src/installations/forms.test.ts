import { describe, expect, it } from 'vitest';
import { designSchema } from './forms';

const messages = (values: object) => {
  const result = designSchema.safeParse({ systemSizeKw: '8.4', panelCount: '21', inverterType: 'Enphase IQ8M', ...values });
  return result.success ? [] : result.error.issues.map(i => `${String(i.path[0])}: ${i.message}`);
};

describe('the design card', () => {
  it('accepts a design within the limits, trimmed', () => {
    expect(designSchema.parse({ systemSizeKw: ' 10.25 ', panelCount: '26', inverterType: ' ' }))
      .toEqual({ systemSizeKw: '10.25', panelCount: '26', inverterType: '' });
  });

  it('catches a typo, once per field', () => {
    expect(messages({ systemSizeKw: '840', panelCount: '0' })).toEqual([
      'systemSizeKw: The size is between 0.5 and 100 kW.',
      'panelCount: The count is between 1 and 400 panels.',
    ]);
    expect(messages({ systemSizeKw: '8,4', panelCount: '2.5' })).toEqual([
      'systemSizeKw: Enter the size in kW, such as 8.4.',
      'panelCount: Enter a whole number of panels, such as 21.',
    ]);
  });
});
