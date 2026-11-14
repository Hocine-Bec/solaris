import { useRef, useState } from 'react';
import type { UseFormRegisterReturn } from 'react-hook-form';
import { CircleAlert, Dices, Eye, EyeOff } from 'lucide-react';
import { generatePassword } from '../users/forms';
import { Icon } from './Icon';

type Props = {
  id: string;
  label: string;
  help: string;
  registration: UseFormRegisterReturn;
  error?: string;
  /** Puts a generated password in the field (the form's setValue) */
  onGenerate: (password: string) => void;
};

/**
 * A password being set: Show while typing, and Generate, which fills in a strong one and shows it, so it can be read
 * out or copied. The browser is told it is a new password, so a password manager offers to save it.
 */
export function NewPasswordField({ id, label, help, registration, error, onGenerate }: Props) {
  const [shown, setShown] = useState(false);
  const input = useRef<HTMLInputElement | null>(null);
  const { ref, ...rest } = registration;

  return (
    <div className={`field${error ? ' has-error' : ''}`}>
      <label className="field-label" htmlFor={id}>{label}</label>
      <div className="pass-row">
        <div className="control control-pass">
          <input
            id={id} type={shown ? 'text' : 'password'} autoComplete="new-password" spellCheck={false} autoCapitalize="none"
            aria-invalid={error ? true : undefined} aria-describedby={`${id}-help${error ? ` ${id}-err` : ''}`}
            {...rest} ref={el => { ref(el); input.current = el; }}
          />
          <button
            className="btn btn-quiet btn-icon btn-sm reveal" type="button" aria-pressed={shown}
            aria-label={shown ? 'Hide the password' : 'Show the password'}
            onClick={() => { setShown(!shown); input.current?.focus(); }}
          >
            <Icon icon={shown ? EyeOff : Eye} />
          </button>
        </div>
        <button className="btn btn-secondary" type="button" onClick={() => { onGenerate(generatePassword()); setShown(true); }}>
          <Icon icon={Dices} />Generate
        </button>
      </div>
      <p className="field-help" id={`${id}-help`}>{help}</p>
      {error && <p className="field-error" id={`${id}-err`}><Icon icon={CircleAlert} /><span>{error}</span></p>}
    </div>
  );
}
