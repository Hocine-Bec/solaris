// The request the form sends to the API's public endpoint, POST /api/leads (CreateLeadRequest).
import type { PickedAddress, QuoteValues } from './types';

export type CreateLeadRequest = {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  propertyStreet: string;
  propertyCity: string;
  propertyState: string;
  propertyZipCode: string;
  propertyCountry: 'US';
  /** Only when the address was picked from the suggestions and not changed since; never guessed */
  propertyLatitude: number | null;
  propertyLongitude: number | null;
  propertyType: string;
  isPropertyOwner: boolean;
  monthlyBillRange: string;
  bestTimeToContact: string;
  notes: string | null;
};

/** True when the address fields still hold the picked suggestion. */
export function isStillPicked(values: QuoteValues, picked: PickedAddress | null): picked is PickedAddress {
  return !!picked
    && values.street.trim() === picked.street
    && values.city.trim() === picked.city
    && values.state === picked.state
    && values.zip.trim() === picked.zip;
}

/** The API stores coordinates with 7 decimals. */
const coordinate = (n: number) => Number(n.toFixed(7));

export function buildLeadRequest(values: QuoteValues, picked: PickedAddress | null): CreateLeadRequest {
  const located = isStillPicked(values, picked);
  return {
    firstName: values.firstName.trim(),
    lastName: values.lastName.trim(),
    email: values.email.trim(),
    phoneNumber: values.phone.trim(),
    propertyStreet: values.street.trim(),
    propertyCity: values.city.trim(),
    propertyState: values.state,
    propertyZipCode: values.zip.trim(),
    propertyCountry: 'US',
    propertyLatitude: located ? coordinate(picked.latitude) : null,
    propertyLongitude: located ? coordinate(picked.longitude) : null,
    propertyType: values.type,
    isPropertyOwner: values.own === 'yes',
    monthlyBillRange: values.bill,
    bestTimeToContact: values.time,
    notes: values.notes.trim() || null,
  };
}
