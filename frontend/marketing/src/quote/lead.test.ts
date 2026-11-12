import { describe, expect, it } from 'vitest';
import { buildLeadRequest } from './lead';
import { EMPTY_VALUES, type PickedAddress, type QuoteValues } from './types';

const values: QuoteValues = {
  ...EMPTY_VALUES,
  firstName: ' Jane ', lastName: 'Doe', email: ' jane.doe@example.com', phone: '(512) 555-0134',
  street: '2104 Oak Hollow Dr', city: 'Austin', zip: '78704', state: 'TX',
  type: 'House', own: 'yes', bill: '100-200', time: 'Morning', notes: '',
};

const picked: PickedAddress = {
  street: '2104 Oak Hollow Dr', city: 'Austin', state: 'TX', zip: '78704', latitude: 30.2451234567, longitude: -97.7712345678,
};

describe('buildLeadRequest', () => {
  it('sends the answers under the API names, trimmed, with the country and no notes when there are none', () => {
    expect(buildLeadRequest(values, null)).toEqual({
      firstName: 'Jane',
      lastName: 'Doe',
      email: 'jane.doe@example.com',
      phoneNumber: '(512) 555-0134',
      propertyStreet: '2104 Oak Hollow Dr',
      propertyCity: 'Austin',
      propertyState: 'TX',
      propertyZipCode: '78704',
      propertyCountry: 'US',
      propertyLatitude: null,
      propertyLongitude: null,
      propertyType: 'House',
      isPropertyOwner: true,
      monthlyBillRange: '100-200',
      bestTimeToContact: 'Morning',
      notes: null,
    });
  });

  it('sends a typed address without coordinates: none are made up', () => {
    const request = buildLeadRequest(values, null);
    expect(request.propertyLatitude).toBeNull();
    expect(request.propertyLongitude).toBeNull();
  });

  it("sends a picked address's coordinates, to the 7 decimals the API stores", () => {
    const request = buildLeadRequest(values, picked);
    expect(request.propertyLatitude).toBe(30.2451235);
    expect(request.propertyLongitude).toBe(-97.7712346);
  });

  it.each([
    ['street', { street: '2106 Oak Hollow Dr' }],
    ['city', { city: 'Round Rock' }],
    ['state', { state: 'CO' }],
    ['ZIP code', { zip: '78745' }],
  ])('drops the coordinates once the %s no longer matches the picked address', (_, change) => {
    const request = buildLeadRequest({ ...values, ...change }, picked);
    expect(request.propertyLatitude).toBeNull();
    expect(request.propertyLongitude).toBeNull();
  });

  it('sends the notes, trimmed', () => {
    expect(buildLeadRequest({ ...values, notes: '  A big oak shades the roof.\n' }, null).notes).toBe('A big oak shades the roof.');
  });

  it('sends a business that leases its premises as not the owner', () => {
    const request = buildLeadRequest({ ...values, type: 'Commercial', own: 'no' }, null);
    expect(request.propertyType).toBe('Commercial');
    expect(request.isPropertyOwner).toBe(false);
  });
});
