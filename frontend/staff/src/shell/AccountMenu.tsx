import { useRef } from 'react';
import { ChevronsUpDown, LogOut, Monitor, Moon, Sun } from 'lucide-react';
import type { CurrentUser } from '../api/auth';
import { Icon } from '../components/Icon';
import { initials } from '../session/session';
import type { ThemeChoice } from './theme';

const CHOICES = [
  { value: 'limestone', label: 'Light', icon: Sun },
  { value: 'night', label: 'Dark', icon: Moon },
  { value: 'system', label: 'System', icon: Monitor },
] as const;

type Props = {
  /** Unique on the page: the side bar and the phone's top bar each have one */
  id: string;
  user: CurrentUser;
  theme: ThemeChoice;
  onTheme: (choice: ThemeChoice) => void;
  onSignOut: () => void;
  /** Initials only, for the phone's top bar */
  compact?: boolean;
};

/**
 * The account button of the approved shell (initials, name, role) and its menu, a native popover placed by the
 * button: the email, so a shared computer shows whose session it is, the theme, and Sign out.
 */
export function AccountMenu({ id, user, theme, onTheme, onSignOut, compact = false }: Props) {
  const button = useRef<HTMLButtonElement>(null);
  const menu = useRef<HTMLDivElement>(null);
  const short = initials(user.name);

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
      <button ref={button} className="acct" type="button" popoverTarget={id} aria-label={compact ? `Account: ${user.name}, ${user.role}` : undefined}>
        <span className="avatar" aria-hidden="true">{short}</span>
        <span><b>{user.name}</b><small>{user.role}</small></span>
        <Icon icon={ChevronsUpDown} className="chev" />
      </button>
      <div ref={menu} className="menu acct-menu" id={id} popover="auto" onToggle={onToggle} aria-label="Account">
        <div className="who">
          <span className="avatar" aria-hidden="true">{short}</span>
          <span><b>{user.name}</b><small>{user.email}</small></span>
        </div>
        <hr />
        <div className="menu-section">
          <p className="a-label" aria-hidden="true">Theme</p>
          <fieldset className="seg">
            <legend className="sr">Theme</legend>
            {CHOICES.map(c => (
              <label key={c.value}>
                <input type="radio" name={`${id}-theme`} value={c.value} checked={theme === c.value} onChange={() => onTheme(c.value)} />
                <Icon icon={c.icon} />{c.label}
              </label>
            ))}
          </fieldset>
        </div>
        <hr />
        <button className="menu-item" type="button" onClick={() => { menu.current?.hidePopover(); onSignOut(); }}>
          <Icon icon={LogOut} />Sign out
        </button>
      </div>
    </>
  );
}
