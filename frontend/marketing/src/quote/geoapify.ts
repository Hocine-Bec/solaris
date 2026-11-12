// Address suggestions from Geoapify's address autocomplete (free plan: 3,000 requests a day, its attribution shown
// with the suggestions). Only complete US addresses are offered: a house number, a street, a city, one of the states
// and a ZIP code. Any failure is the caller's cue to leave the field as a plain one. An answer can take a second or
// more (0.5 to 4.4 s in a test of eight lookups), so the field never waits on one: see AddressField.tsx.
import { STATE_CODES } from '../content/states';
import type { PickedAddress } from './types';

export const GEOAPIFY_KEY: string = import.meta.env.VITE_GEOAPIFY_API_KEY ?? '';

type GeoapifyResult = {
  housenumber?: string;
  street?: string;
  city?: string;
  state_code?: string;
  postcode?: string;
  lat?: number;
  lon?: number;
};

/** Complete US addresses from Geoapify's JSON answer (format=json), without duplicates, at most `limit`. */
export function parseSuggestions(json: unknown, limit = 5): PickedAddress[] {
  const results = (json as { results?: GeoapifyResult[] } | null)?.results ?? [];
  const seen = new Set<string>();
  const out: PickedAddress[] = [];
  for (const r of results) {
    const state = r.state_code?.toUpperCase() ?? '';
    const zip = r.postcode?.match(/^\d{5}(-\d{4})?/)?.[0];
    if (!r.housenumber || !r.street || !r.city || !STATE_CODES.has(state) || !zip
      || typeof r.lat !== 'number' || typeof r.lon !== 'number') continue;
    const address = { street: `${r.housenumber} ${r.street}`, city: r.city, state, zip, latitude: r.lat, longitude: r.lon };
    const key = `${address.street}|${address.city}|${state}|${zip}`.toLowerCase();
    if (seen.has(key)) continue;
    seen.add(key);
    out.push(address);
    if (out.length === limit) break;
  }
  return out;
}

/** Lookups start once the text begins with a house number: a street or a city alone returns no complete address. */
export const startsWithNumber = (text: string) => /^\d/.test(text.trim());

/** The text as a cache key: lower case, single spaces. */
export const normalizeQuery = (text: string) => text.trim().toLowerCase().replace(/\s+/g, ' ');

/**
 * True when every word typed starts a word of the address ("100 cong ave aus" fits 100 Congress Avenue, Austin):
 * the list already shown narrows at once while the next answer is on its way.
 */
export function matchesTyped(address: PickedAddress, text: string): boolean {
  const words = `${address.street} ${address.city} ${address.state} ${address.zip}`.toLowerCase().split(/[\s,.]+/);
  return normalizeQuery(text).split(/[\s,.]+/).filter(Boolean).every(typed => words.some(word => word.startsWith(typed)));
}

/**
 * An earlier answer, narrowed to what has been typed since it was asked for. The service has already matched the
 * words it was asked about (it knows "fifth" is "5th"), so only the words typed after them are checked; a word still
 * being finished is let through. If the text no longer extends the earlier one, every word is checked.
 */
export function narrowAnswer(addresses: PickedAddress[], answeredText: string, typedText: string): PickedAddress[] {
  const answered = normalizeQuery(answeredText), typed = normalizeQuery(typedText);
  if (!typed.startsWith(answered)) return addresses.filter(address => matchesTyped(address, typed));
  const extra = typed.slice(answered.length);
  const words = extra.split(/[\s,.]+/).filter(Boolean);
  const newWords = /^[\s,.]/.test(extra) ? words : words.slice(1);
  return addresses.filter(address => matchesTyped(address, newWords.join(' ')));
}

export async function searchAddresses(text: string, signal: AbortSignal): Promise<PickedAddress[]> {
  const url = new URL('https://api.geoapify.com/v1/geocode/autocomplete');
  url.search = new URLSearchParams({ text, filter: 'countrycode:us', format: 'json', lang: 'en', limit: '10', apiKey: GEOAPIFY_KEY }).toString();
  const response = await fetch(url, { signal });
  if (!response.ok) throw new Error(`The address service answered ${response.status}`);
  return parseSuggestions(await response.json());
}
