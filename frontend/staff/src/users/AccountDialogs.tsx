import { useEffect, useRef, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { Check, Copy, UserX } from 'lucide-react';
import { changePassword } from '../api/auth';
import { createTechnician, deactivateUser, setPassword, updateUser, USERS_KEY, type User } from '../api/users';
import { Dialog } from '../components/Dialog';
import { Icon } from '../components/Icon';
import { NewPasswordField } from '../components/NewPasswordField';
import { FormNotice, SubmitButton } from '../components/SubmitButton';
import { TextField } from '../components/TextField';
import { changePasswordSchema, editSchema, newPasswordSchema, RULE_HELP, technicianSchema } from './forms';
import { useDialogForm } from '../forms/useDialogForm';

/*
 * The account dialogs, from the approved Users design: New technician (then its password, once), Edit, Set a new
 * password, Deactivate, and Change your password from the account menu. Each closes itself on Cancel, Escape or its
 * close button; after a save, the lists and pages showing accounts load again.
 */

const TITLE = 'dlg-title';

function Cancel({ onClose }: { onClose: () => void }) {
  return <button className="btn btn-quiet" type="button" onClick={onClose}>Cancel</button>;
}

/** Accounts shown anywhere load again after a change */
function useRefreshUsers() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: USERS_KEY });
}

/** A result that replaces a dialog's form: its heading takes focus, so a screen reader reads what happened. */
function useFocusOnShow(shown: boolean) {
  const heading = useRef<HTMLParagraphElement>(null);
  useEffect(() => { if (shown) heading.current?.focus(); }, [shown]);
  return heading;
}

/** New technician, from the list; once created, their email and password, shown this once to give them in person. */
export function NewTechnicianDialog({ onClose }: { onClose: () => void }) {
  const refresh = useRefreshUsers();
  const navigate = useNavigate();
  const [created, setCreated] = useState<{ user: User; password: string } | null>(null);
  const [copied, setCopied] = useState(false);
  const heading = useFocusOnShow(created !== null);
  const mutation = useMutation({ mutationFn: createTechnician });
  const { form, notice, onSubmit } = useDialogForm({
    schema: technicianSchema,
    defaultValues: { name: '', email: '', password: '' },
    emailTaken: 'An account already uses this email.',
    send: async values => {
      const user = await mutation.mutateAsync(values);
      void refresh();
      setCreated({ user, password: values.password });
    },
  });
  const { errors, isSubmitting } = form.formState;

  if (created) {
    const copy = () => navigator.clipboard?.writeText(created.password).then(() => setCopied(true), () => {});
    return (
      <Dialog labelledBy={TITLE} onClose={onClose}>
        <div className="dialog-body">
          <p className="a-heading" id={TITLE} ref={heading} tabIndex={-1}>{created.user.name} can sign in</p>
          <p>Give them their email and this password, in person. It isn't shown again; if it's lost, set a new one from their page.</p>
          <div className="once"><span className="a-label">Email</span><span>{created.user.email}</span></div>
          <div className="once"><span className="a-label">Password</span><code>{created.password}</code></div>
          <div className="actions">
            <button className="btn btn-secondary" type="button" onClick={() => void copy()}>
              <Icon icon={copied ? Check : Copy} />{copied ? 'Copied' : 'Copy the password'}
            </button>
            <button className="btn btn-primary" type="button" onClick={() => { onClose(); navigate(`/users/${created.user.id}`); }}>
              Open their page
            </button>
          </div>
        </div>
      </Dialog>
    );
  }

  return (
    <Dialog labelledBy={TITLE} onClose={onClose}>
      <form noValidate onSubmit={onSubmit}>
        <p className="a-heading" id={TITLE}>New technician</p>
        <p>They sign in with this email and password. You'll see the password once, to give it to them.</p>
        {notice && <FormNotice>{notice}</FormNotice>}
        <TextField id="nt-name" label="Name" registration={form.register('name')} error={errors.name?.message} />
        <TextField id="nt-email" label="Email" type="email" registration={form.register('email')} error={errors.email?.message} />
        <NewPasswordField
          id="nt-pass" label="Password" help={RULE_HELP} registration={form.register('password')} error={errors.password?.message}
          onGenerate={p => form.setValue('password', p, { shouldValidate: form.formState.isSubmitted })}
        />
        <div className="actions">
          <Cancel onClose={onClose} />
          <SubmitButton busy={isSubmitting}>Create account</SubmitButton>
        </div>
      </form>
    </Dialog>
  );
}

/** Edit, from their page: a typo, or a new email. */
export function EditDialog({ user, onClose, onSaved }: { user: User; onClose: () => void; onSaved: () => void }) {
  const refresh = useRefreshUsers();
  const mutation = useMutation({ mutationFn: (values: { name: string; email: string }) => updateUser(user.id, values) });
  const { form, notice, onSubmit } = useDialogForm({
    schema: editSchema,
    defaultValues: { name: user.name, email: user.email },
    emailTaken: 'Another account uses this email.',
    send: async values => {
      await mutation.mutateAsync(values);
      await refresh();
      onSaved();
    },
  });
  const { errors, isSubmitting } = form.formState;

  return (
    <Dialog labelledBy={TITLE} onClose={onClose}>
      <form noValidate onSubmit={onSubmit}>
        <p className="a-heading" id={TITLE}>Edit {user.name}</p>
        <p>For a typo, or a new email. Their password and sessions don't change.</p>
        {notice && <FormNotice>{notice}</FormNotice>}
        <TextField id="ed-name" label="Name" registration={form.register('name')} error={errors.name?.message} />
        <TextField id="ed-email" label="Email" type="email" registration={form.register('email')} error={errors.email?.message} />
        <div className="actions">
          <Cancel onClose={onClose} />
          <SubmitButton busy={isSubmitting}>Save</SubmitButton>
        </div>
      </form>
    </Dialog>
  );
}

/** Set a new password, from their page: it signs them out everywhere. */
export function SetPasswordDialog({ user, onClose, onSaved }: { user: User; onClose: () => void; onSaved: () => void }) {
  const mutation = useMutation({ mutationFn: (password: string) => setPassword(user.id, password) });
  const { form, notice, onSubmit } = useDialogForm({
    schema: newPasswordSchema,
    defaultValues: { password: '' },
    send: async ({ password }) => {
      await mutation.mutateAsync(password);
      onSaved();
    },
  });
  const { errors, isSubmitting } = form.formState;

  return (
    <Dialog labelledBy={TITLE} onClose={onClose}>
      <form noValidate onSubmit={onSubmit}>
        <p className="a-heading" id={TITLE}>A new password for {user.name}</p>
        <p>Their current password stops working, and they're signed out everywhere: give them the new one in person.</p>
        {notice && <FormNotice>{notice}</FormNotice>}
        <NewPasswordField
          id="np-pass" label="New password" help={RULE_HELP} registration={form.register('password')} error={errors.password?.message}
          onGenerate={p => form.setValue('password', p, { shouldValidate: form.formState.isSubmitted })}
        />
        <div className="actions">
          <Cancel onClose={onClose} />
          <SubmitButton busy={isSubmitting}>Set the password</SubmitButton>
        </div>
      </form>
    </Dialog>
  );
}

/** Deactivate, from their page: final enough to ask first. */
export function DeactivateDialog({ user, onClose, onDone }: { user: User; onClose: () => void; onDone: () => void }) {
  const refresh = useRefreshUsers();
  const [notice, setNotice] = useState<string | null>(null);
  const mutation = useMutation({ mutationFn: () => deactivateUser(user.id) });

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (mutation.isPending) return;
    setNotice(null);
    try {
      await mutation.mutateAsync();
      await refresh();
      onDone();
    } catch (error) {
      setNotice(error instanceof Error ? error.message : 'Something went wrong in the app. Try again; if it happens again, report it.');
    }
  };

  return (
    <Dialog labelledBy={TITLE} onClose={onClose}>
      <form noValidate onSubmit={e => void submit(e)}>
        <p className="a-heading" id={TITLE}>Deactivate {user.name}?</p>
        <p>They're signed out at their next action and can't sign in again. Their assignments and history stay; reassign the open ones. You can reactivate the account later.</p>
        {notice && <FormNotice>{notice}</FormNotice>}
        <div className="actions">
          <Cancel onClose={onClose} />
          <SubmitButton busy={mutation.isPending} danger><Icon icon={UserX} />Deactivate</SubmitButton>
        </div>
      </form>
    </Dialog>
  );
}

/** Change your password, from the account menu or your own page: you stay signed in here; elsewhere you're signed out. */
export function ChangePasswordDialog({ onClose }: { onClose: () => void }) {
  const [changed, setChanged] = useState(false);
  const heading = useFocusOnShow(changed);
  const mutation = useMutation({ mutationFn: (v: { currentPassword: string; newPassword: string }) => changePassword(v.currentPassword, v.newPassword) });
  const { form, notice, onSubmit } = useDialogForm({
    schema: changePasswordSchema,
    defaultValues: { currentPassword: '', newPassword: '' },
    send: async values => {
      await mutation.mutateAsync(values);
      setChanged(true);
    },
  });
  const { errors, isSubmitting } = form.formState;

  if (changed) {
    return (
      <Dialog labelledBy={TITLE} onClose={onClose}>
        <div className="dialog-body">
          <p className="a-heading" id={TITLE} ref={heading} tabIndex={-1}>Your password is changed</p>
          <p>You stay signed in here; your other sessions are signed out. Use the new password next time you sign in.</p>
          <div className="actions">
            <button className="btn btn-primary" type="button" onClick={onClose}>Done</button>
          </div>
        </div>
      </Dialog>
    );
  }

  return (
    <Dialog labelledBy={TITLE} onClose={onClose}>
      <form noValidate onSubmit={onSubmit}>
        <p className="a-heading" id={TITLE}>Change your password</p>
        <p>You stay signed in here; your other sessions are signed out.</p>
        {notice && <FormNotice>{notice}</FormNotice>}
        <TextField
          id="cp-current" label="Current password" type="password" autoComplete="current-password"
          registration={form.register('currentPassword')} error={errors.currentPassword?.message}
        />
        <NewPasswordField
          id="cp-new" label="New password" help={`The same rule: ${RULE_HELP.charAt(0).toLowerCase()}${RULE_HELP.slice(1)}`}
          registration={form.register('newPassword')} error={errors.newPassword?.message}
          onGenerate={p => form.setValue('newPassword', p, { shouldValidate: form.formState.isSubmitted })}
        />
        <div className="actions">
          <Cancel onClose={onClose} />
          <SubmitButton busy={isSubmitting}>Change password</SubmitButton>
        </div>
      </form>
    </Dialog>
  );
}
