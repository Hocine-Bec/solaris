import { useRef } from 'react';
import { ChevronsUpDown, Monitor, Moon, Sun } from 'lucide-react';
import { Icon } from '../components/Icon';
import type { ThemeChoice } from './theme';

const CHOICES = [
  { value: 'limestone', label: 'Light', icon: Sun },
  { value: 'night', label: 'Dark', icon: Moon },
  { value: 'system', label: 'System', icon: Monitor },
] as const;

type Props = {
  /** Unique on the page: the side bar and the phone's top bar each have one */
  id: string;
  choice: ThemeChoice;
  onChoose: (choice: ThemeChoice) => void;
  /** An icon button, for the phone's top bar */
  compact?: boolean;
};

/**
 * The theme choice: Light, Dark or System, in a native popover under (or above) its button. With signing in, this
 * becomes the account menu, with the theme inside it.
 */
export function ThemeMenu({ id, choice, onChoose, compact = false }: Props) {
  const button = useRef<HTMLButtonElement>(null);
  const menu = useRef<HTMLDivElement>(null);
  const current = CHOICES.find(c => c.value === choice)!;

  // Placed by its button: above it when there is no room below (the side bar's button is at the bottom of the screen).
  const onToggle = (e: React.ToggleEvent<HTMLDivElement>) => {
    const m = menu.current, b = button.current?.getBoundingClientRect();
    if (e.newState !== 'open' || !m || !b) return;
    const above = b.bottom + m.offsetHeight + 8 > innerHeight;
    m.style.top = `${above ? b.top - m.offsetHeight - 6 : b.bottom + 6}px`;
    m.style.left = `${Math.min(innerWidth - m.offsetWidth - 8, Math.max(8, compact ? b.right - m.offsetWidth : b.left))}px`;
    m.querySelector<HTMLInputElement>('input:checked')?.focus({ preventScroll: true });
  };

  return (
    <>
      <button ref={button} className="theme-btn" type="button" popoverTarget={id} aria-label={compact ? `Theme: ${current.label}` : undefined}>
        <span className="badge"><Icon icon={current.icon} /></span>
        <span><b>Theme</b><small>{current.label}</small></span>
        <Icon icon={ChevronsUpDown} className="chev" />
      </button>
      <div ref={menu} className="menu" id={id} popover="auto" onToggle={onToggle}>
        <div className="menu-section">
          <p className="a-label" aria-hidden="true">Theme</p>
          <fieldset className="seg">
            <legend className="sr">Theme</legend>
            {CHOICES.map(c => (
              <label key={c.value}>
                <input type="radio" name={`${id}-choice`} value={c.value} checked={choice === c.value} onChange={() => onChoose(c.value)} />
                <Icon icon={c.icon} />{c.label}
              </label>
            ))}
          </fieldset>
        </div>
      </div>
    </>
  );
}
