import { useRef, useState } from 'react';
import type { UseFormRegisterReturn } from 'react-hook-form';
import { ArrowBigUp, CircleAlert, Eye, EyeOff } from 'lucide-react';
import { Icon } from '../components/Icon';

type Props = {
  id: string;
  registration: UseFormRegisterReturn;
  error?: string;
  /** Whether the password shows as text; the form hides it again after a refusal */
  shown: boolean;
  onShownChange: (shown: boolean) => void;
};

/**
 * The password: paste works, so password managers do; a button shows it while typing; a warning appears when Caps
 * Lock is on (NIST SP 800-63B asks verifiers to allow paste and to offer to show the password).
 */
export function PasswordField({ id, registration, error, shown, onShownChange }: Props) {
  const [capsLock, setCapsLock] = useState(false);
  const input = useRef<HTMLInputElement | null>(null);
  const { ref, onBlur, ...rest } = registration;
  const checkCapsLock = (e: React.KeyboardEvent<HTMLInputElement>) => setCapsLock(e.getModifierState('CapsLock'));

  return (
    <div className={`field${error ? ' has-error' : ''}`}>
      <label className="field-label" htmlFor={id}>Password</label>
      <div className="control control-pass">
        <input
          id={id} type={shown ? 'text' : 'password'} autoComplete="current-password"
          aria-invalid={error ? true : undefined} aria-describedby={`${id}-caps ${id}-err`}
          {...rest}
          ref={el => { ref(el); input.current = el; }}
          onKeyDown={checkCapsLock} onKeyUp={checkCapsLock}
          onBlur={e => { setCapsLock(false); void onBlur(e); }}
        />
        <button
          className="btn btn-quiet btn-icon btn-sm reveal" type="button" aria-pressed={shown}
          aria-label={shown ? 'Hide the password' : 'Show the password'}
          onClick={() => { onShownChange(!shown); input.current?.focus(); }}
        >
          <Icon icon={shown ? EyeOff : Eye} />
        </button>
      </div>
      <p className="field-help caps" id={`${id}-caps`} hidden={!capsLock}><Icon icon={ArrowBigUp} /><span>Caps Lock is on.</span></p>
      {error && <p className="field-error" id={`${id}-err`}><Icon icon={CircleAlert} /><span>{error}</span></p>}
    </div>
  );
}
