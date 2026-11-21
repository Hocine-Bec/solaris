import { useState } from 'react';
import { useForm, type DefaultValues, type FieldValues, type Path } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import type { z } from 'zod';
import { formErrors } from './formErrors';

type Options<T extends FieldValues> = {
  schema: z.ZodType<T, T>;
  defaultValues: DefaultValues<T>;
  /** Sends the checked values; a failure is shown on the form */
  send: (values: T) => Promise<void>;
  /** Where a taken email is shown, in this form's words */
  emailTaken?: string;
};

/**
 * A form in a dialog (an account's, a lead's): its own checks first, then the request, and the API's refusal shown
 * where it belongs: under the fields it names, focus on the first, or as one notice. One press sends one request.
 */
export function useDialogForm<T extends FieldValues>({ schema, defaultValues, send, emailTaken }: Options<T>) {
  const [notice, setNotice] = useState<string | null>(null);
  const form = useForm<T>({ resolver: zodResolver(schema), defaultValues });
  const fields = Object.keys(defaultValues ?? {});

  const onSubmit = form.handleSubmit(async values => {
    setNotice(null);
    try {
      await send(values);
    } catch (error) {
      const shown = formErrors(error, { fields, emailTaken });
      const named = Object.entries(shown.fields);
      for (const [field, message] of named) form.setError(field as Path<T>, { message });
      if (named.length > 0) form.setFocus(named[0][0] as Path<T>);
      setNotice(shown.notice);
    }
  });

  return { form, notice, onSubmit: (e: React.FormEvent) => void onSubmit(e) };
}
