import { describe, expect, it } from 'vitest';
import { ApiError } from '../api/errors';
import { actionsFor, billLabel, cityLine, filterFrom, FILTERS, isChangedMeanwhile, isMovedMeanwhile, leadsHref, propertyLabel } from './view';

describe('the lead list filters', () => {
  it('opens on All, also for an unknown filter', () => {
    expect(filterFrom(null).key).toBe('all');
    expect(filterFrom('everything').key).toBe('all');
    expect(FILTERS.map(f => f.label)).toEqual(['All', 'New', 'Contacted', 'Closed']);
  });

  it('asks the API for the statuses each filter gathers', () => {
    expect(Object.fromEntries(FILTERS.map(f => [f.key, f.statuses]))).toEqual({
      all: [], new: ['New'], contacted: ['Contacted'], closed: ['Converted', 'Disqualified', 'Lost'],
    });
  });

  it('keeps the filter and the page in the address', () => {
    expect(leadsHref(filterFrom('closed'))).toBe('/leads?status=closed');
    expect(leadsHref(filterFrom('all'), 3)).toBe('/leads?status=all&page=3');
  });
});

describe('a lead in words', () => {
  it("reads the quote form's bill ranges, and shows an older value as it is", () => {
    expect(billLabel('under100')).toBe('Under $100 a month');
    expect(billLabel('300plus')).toBe('$300 or more a month');
    expect(billLabel('$100-$200')).toBe('$100-$200');
  });

  it('writes the address and the property as the office reads them', () => {
    expect(cityLine({ street: '2104 Oak Hollow Dr', city: 'Round Rock', state: 'TX', zipCode: '78681' })).toBe('Round Rock, TX 78681');
    expect(propertyLabel({ propertyType: 'House', isPropertyOwner: true })).toBe('House, they own it');
    expect(propertyLabel({ propertyType: 'Commercial', isPropertyOwner: false })).toBe("Commercial, they don't own it");
  });
});

describe("a lead's actions", () => {
  it("offers only what the API's next allows", () => {
    expect(actionsFor({ next: ['Contacted', 'Disqualified', 'Lost'] }))
      .toEqual({ contact: true, convert: false, disqualify: true, lose: true, edit: true });
    expect(actionsFor({ next: ['Converted', 'Disqualified', 'Lost'] }))
      .toEqual({ contact: false, convert: true, disqualify: true, lose: true, edit: true });
    expect(actionsFor({ next: [] })).toEqual({ contact: false, convert: false, disqualify: false, lose: false, edit: false });
  });
});

describe('a move refused because the lead moved meanwhile', () => {
  it("is the API's transition_not_allowed, not any other 409", () => {
    expect(isMovedMeanwhile(new ApiError('conflict', 'This lead is Lost: it can\'t become Contacted.', { status: 409, code: 'lead.transition_not_allowed' }))).toBe(true);
    expect(isMovedMeanwhile(new ApiError('conflict', 'Another open lead has this email.', { status: 409, code: 'lead.email_taken' }))).toBe(false);
    expect(isMovedMeanwhile(new Error('boom'))).toBe(false);
  });

  it('counts a save made at the same moment as changed meanwhile too, but not a taken email', () => {
    const concurrent = new ApiError('conflict', 'This lead was changed by someone else meanwhile.', { status: 409, code: 'lead.concurrent_change' });
    expect(isMovedMeanwhile(concurrent)).toBe(false);
    expect(isChangedMeanwhile(concurrent)).toBe(true);
    expect(isChangedMeanwhile(new ApiError('conflict', 'This lead is Lost.', { status: 409, code: 'lead.transition_not_allowed' }))).toBe(true);
    expect(isChangedMeanwhile(new ApiError('conflict', 'Another open lead has this email.', { status: 409, code: 'lead.email_taken' }))).toBe(false);
  });
});
