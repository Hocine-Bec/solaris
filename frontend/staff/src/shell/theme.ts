import { useEffect, useState } from 'react';

/** Light is the Limestone theme and Dark the Night theme; System follows the device. */
export type ThemeChoice = 'limestone' | 'night' | 'system';
export type Theme = 'limestone' | 'night';

/** The key index.html's pre-paint script reads too */
const KEY = 'solaris-staff-theme';

export const parseChoice = (value: string | null): ThemeChoice =>
  value === 'limestone' || value === 'night' ? value : 'system';

export const themeFor = (choice: ThemeChoice, systemDark: boolean): Theme =>
  choice === 'system' ? (systemDark ? 'night' : 'limestone') : choice;

function readChoice(): ThemeChoice {
  try {
    return parseChoice(localStorage.getItem(KEY));
  } catch {
    return 'system';   // storage blocked: follow the device
  }
}

function saveChoice(choice: ThemeChoice) {
  try {
    if (choice === 'system') localStorage.removeItem(KEY);
    else localStorage.setItem(KEY, choice);
  } catch {
    // storage blocked: the choice lasts until the page closes
  }
}

/** The theme choice, applied to the whole page and kept for the next visit; System follows the device as it changes. */
export function useTheme() {
  const [choice, setChoice] = useState(readChoice);

  useEffect(() => {
    const dark = matchMedia('(prefers-color-scheme: dark)');
    const apply = () => { document.documentElement.dataset.theme = themeFor(choice, dark.matches); };
    apply();
    if (choice !== 'system') return;
    dark.addEventListener('change', apply);
    return () => dark.removeEventListener('change', apply);
  }, [choice]);

  const choose = (next: ThemeChoice) => { saveChoice(next); setChoice(next); };
  return [choice, choose] as const;
}
