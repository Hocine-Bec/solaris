import { useEffect, useRef, useState } from 'react';
import { CircleAlert, LoaderCircle } from 'lucide-react';
import { Icon } from '../components/Icon';
import { GEOAPIFY_KEY, narrowAnswer, normalizeQuery, searchAddresses, startsWithNumber } from './geoapify';
import type { PickedAddress } from './types';

type Props = {
  value: string;
  error?: string;
  onChange: (street: string) => void;
  onPick: (address: PickedAddress) => void;
};

/** What the list under the field says besides the suggestions */
type Note = 'none' | 'hint' | 'searching' | 'nothing';

const MIN_LENGTH = 3;
const DEBOUNCE_MS = 300;   // a pause between words, not between letters: each lookup counts against the daily limit
const EXAMPLE = '2104 Oak Hollow Dr';

/** The part of a suggestion that matches what was typed, in bold */
function Marked({ text, query }: { text: string; query: string }) {
  const i = text.toLowerCase().indexOf(query.toLowerCase());
  if (!query || i < 0) return <>{text}</>;
  return <>{text.slice(0, i)}<b>{text.slice(i, i + query.length)}</b>{text.slice(i + query.length)}</>;
}

let preconnected = false;
/** Opens the connection to the address service as soon as the visitor reaches the field. */
function preconnect() {
  if (preconnected) return;
  preconnected = true;
  const link = document.createElement('link');
  link.rel = 'preconnect';
  link.href = 'https://api.geoapify.com';
  link.crossOrigin = 'anonymous';
  document.head.append(link);
}

/**
 * The street field, with address suggestions as a helper: typing without picking works; picking fills the street,
 * city, state and ZIP; if the service has no key or fails, the field quietly becomes a plain one.
 *
 * The service answers in a second or more, so the field never waits on it: a lookup is never cancelled by the next
 * keystroke (each answer shows when it arrives, unless a later one already has), the list already shown narrows at
 * once as the visitor types on, and each answer is kept for the visit. Text that doesn't start with a house number
 * gets a hint instead of a lookup.
 */
export function AddressField({ value, error, onChange, onPick }: Props) {
  const [serviceOn, setServiceOn] = useState(!!GEOAPIFY_KEY);
  const [items, setItems] = useState<PickedAddress[]>([]);
  const [note, setNote] = useState<Note>('none');
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(-1);
  const [pending, setPending] = useState(0);
  const [live, setLive] = useState('');
  const timer = useRef(0);
  const cache = useRef(new Map<string, PickedAddress[]>());
  const latest = useRef('');        // the text in the field now
  const sent = useRef(0);           // lookups sent
  const shown = useRef(0);          // the lookup whose answer is shown
  const answered = useRef<{ text: string; list: PickedAddress[] }>({ text: '', list: [] });   // that answer, whole
  const listRef = useRef<HTMLUListElement>(null);
  const controller = useRef<AbortController | null>(null);

  // One controller per mount: leaving the page cancels the lookups still on their way. (Made here, not once for the
  // component's life: in development React mounts twice, and the first unmount would cancel every later lookup.)
  useEffect(() => {
    const lookups = new AbortController();
    controller.current = lookups;
    return () => { clearTimeout(timer.current); lookups.abort(); };
  }, []);

  const close = () => { setOpen(false); setActive(-1); };

  /** Shows a list of addresses for the text now in the field; `final` when it is the answer to that very text. */
  const show = (list: PickedAddress[], final: boolean) => {
    setItems(list);
    setActive(-1);
    setNote(list.length ? 'none' : final ? 'nothing' : 'searching');
    setOpen(true);
    if (list.length) setLive(`${list.length} address suggestion${list.length > 1 ? 's' : ''}. Use the arrow keys to choose.`);
    else if (final) setLive('No address found. You can type the full address.');
  };

  const lookUp = async (text: string, key: string) => {
    const signal = controller.current?.signal;
    if (!signal || signal.aborted) return;
    const number = ++sent.current;
    setPending(n => n + 1);
    try {
      const found = await searchAddresses(text, signal);
      cache.current.set(key, found);
      const now = latest.current;
      // An answer to an earlier text, after a later one is already shown, or once the field no longer needs one: dropped.
      if (number < shown.current || !startsWithNumber(now) || now.trim().length < MIN_LENGTH) return;
      shown.current = number;
      answered.current = { text, list: found };
      // The answer to the text in the field is shown as it is; an answer to an earlier text, narrowed to what has been
      // typed since.
      const final = normalizeQuery(now) === key;
      show(final ? found : narrowAnswer(found, text, now), final);
    } catch {
      if (signal.aborted) return;
      setServiceOn(false);   // a plain field from now on
      close();
    } finally {
      setPending(n => n - 1);
    }
  };

  const onType = (text: string) => {
    onChange(text);
    latest.current = text;
    clearTimeout(timer.current);
    const q = text.trim();
    if (!serviceOn || q.length < 2) { close(); setNote('none'); return; }
    if (!startsWithNumber(q)) {
      setItems([]); setNote('hint'); setOpen(true); setActive(-1);
      setLive(`Start with the house number, like ${EXAMPLE}.`);
      return;
    }
    if (q.length < MIN_LENGTH) { close(); return; }
    const key = normalizeQuery(q);
    const known = cache.current.get(key);
    if (known) { shown.current = sent.current; answered.current = { text: q, list: known }; show(known, true); return; }
    // At once: the last answer, narrowed to what has been typed since; the lookup follows a short pause.
    show(narrowAnswer(answered.current.list, answered.current.text, q), false);
    timer.current = window.setTimeout(() => void lookUp(q, key), DEBOUNCE_MS);
  };

  const pick = (i: number) => {
    const address = items[i];
    onPick(address);
    close();
    setLive(`${address.street}, ${address.city}, ${address.state} ${address.zip} selected.`);
  };

  const highlight = (i: number) => {
    setActive(i);
    listRef.current?.querySelector(`#q-street-opt-${i}`)?.scrollIntoView({ block: 'nearest' });
  };

  const onKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (!open) return;
    if (e.key === 'Escape') { e.preventDefault(); close(); return; }
    if (e.key === 'Tab') { close(); return; }
    if (!items.length) return;
    if (e.key === 'ArrowDown') { e.preventDefault(); highlight((active + 1) % items.length); }
    else if (e.key === 'ArrowUp') { e.preventDefault(); highlight((active - 1 + items.length) % items.length); }
    else if (e.key === 'Enter' && active >= 0) { e.preventDefault(); pick(active); }
  };

  const describedBy = ['q-street-help', error && 'q-street-err'].filter(Boolean).join(' ');
  const query = value.trim().split(/\s+/)[0] ?? '';
  const listed = open && items.length > 0;

  return (
    <div className={`field${error ? ' has-error' : ''}`}>
      <label className="field-label" htmlFor="q-street">Street address</label>
      <div className="combo">
        <div className="control control-lg">
          <input
            id="q-street" name="street" autoComplete="address-line1" value={value}
            role={serviceOn ? 'combobox' : undefined}
            aria-autocomplete={serviceOn ? 'list' : undefined}
            aria-expanded={serviceOn ? listed : undefined}
            aria-controls={serviceOn ? 'q-street-list' : undefined}
            aria-activedescendant={listed && active >= 0 ? `q-street-opt-${active}` : undefined}
            aria-describedby={describedBy} aria-invalid={error ? true : undefined}
            onChange={e => onType(e.target.value)}
            onKeyDown={onKeyDown}
            onFocus={() => { if (serviceOn) preconnect(); }}
            onBlur={() => window.setTimeout(close, 120)}
          />
          {serviceOn && pending > 0 && <Icon icon={LoaderCircle} className="spin" />}
        </div>
        {serviceOn && (
          // Pressing in the list keeps the focus in the field, so a click picks (or follows a credit link) before it closes.
          <div className="suggest" hidden={!open} onMouseDown={e => e.preventDefault()}>
            <ul id="q-street-list" role="listbox" aria-label="Address suggestions" ref={listRef}>
              {items.map((item, i) => (
                // The same street and ZIP code can be in two cities: the key is the whole address, as parseSuggestions dedupes it.
                <li key={`${item.street}|${item.city}|${item.state}|${item.zip}`} role="option" id={`q-street-opt-${i}`} aria-selected={i === active} onClick={() => pick(i)}>
                  <span className="s1"><Marked text={item.street} query={query} /></span>
                  <span className="s2">{item.city}, {item.state} {item.zip}</span>
                </li>
              ))}
            </ul>
            {note === 'hint' && <p className="note" aria-hidden="true">Start with the house number, like {EXAMPLE}.</p>}
            {note === 'searching' && <p className="note" aria-hidden="true">Looking for addresses…</p>}
            {note === 'nothing' && <p className="note" aria-hidden="true">No address found. Keep typing, or type the address in full.</p>}
            {listed && <p className="foot" aria-hidden="true">Not listed? Keep typing your full address</p>}
            {note !== 'hint' && (
              <p className="credit">
                Powered by <a href="https://www.geoapify.com/" target="_blank" rel="noopener">Geoapify</a>
                {' · '}© <a href="https://www.openstreetmap.org/copyright" target="_blank" rel="noopener">OpenStreetMap</a> contributors
              </p>
            )}
          </div>
        )}
      </div>
      <p className="field-help" id="q-street-help">{serviceOn ? 'Start with the house number and pick your address, or type it in full.' : 'Type your full street address.'}</p>
      {error && <p className="field-error" id="q-street-err"><Icon icon={CircleAlert} /><span>{error}</span></p>}
      <p className="sr" aria-live="polite">{live}</p>
    </div>
  );
}
