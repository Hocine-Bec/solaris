import type { UseFormRegisterReturn } from 'react-hook-form';
import { CircleAlert } from 'lucide-react';
import { Icon } from './Icon';

type Props = {
  id: string;
  label: string;
  type?: 'text' | 'email' | 'password';
  autoComplete?: string;
  registration: UseFormRegisterReturn;
  error?: string;
};

/** A labelled one-line field, with its error under it; the error is tied to the input for screen readers. */
export function TextField({ id, label, type = 'text', autoComplete = 'off', registration, error }: Props) {
  return (
    <div className={`field${error ? ' has-error' : ''}`}>
      <label className="field-label" htmlFor={id}>{label}</label>
      <div className="control">
        <input
          id={id} type={type} autoComplete={autoComplete} spellCheck={false} autoCapitalize={type === 'email' ? 'none' : undefined}
          aria-invalid={error ? true : undefined} aria-describedby={error ? `${id}-err` : undefined} {...registration}
        />
      </div>
      {error && <p className="field-error" id={`${id}-err`}><Icon icon={CircleAlert} /><span>{error}</span></p>}
    </div>
  );
}
