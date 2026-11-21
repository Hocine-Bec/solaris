import { useState, type ReactNode } from 'react';
import { useMutation } from '@tanstack/react-query';
import { Ban, CircleCheck, CircleDashed, Globe, SolarPanel, UserRoundCheck } from 'lucide-react';
import { ApiError } from '../api/errors';
import { convertLead, moveLead, updateLead, type Lead } from '../api/leads';
import { Dialog } from '../components/Dialog';
import { Icon } from '../components/Icon';
import { FormNotice, SubmitButton } from '../components/SubmitButton';
import { TextField } from '../components/TextField';
import { useDialogForm } from '../forms/useDialogForm';
import { formatDay } from '../lib/dates';
import { editLeadSchema } from './forms';
import { cityLine, isMovedMeanwhile } from './view';

/*
 * The lead's dialogs, from the approved Leads design: the three that close it for good (Disqualify, Mark as lost,
 * Convert) say what happens and ask first; Edit corrects the contact details. Each closes itself on Cancel, Escape or
 * its close button.
 */

const TITLE = 'dlg-title';
const UNEXPECTED = 'Something went wrong in the app. Try again; if it happens again, report it.';

type ChangeProps = {
  lead: Lead;
  onClose: () => void;
  /** The lead as the API answered it */
  onDone: (lead: Lead) => void;
  /** Refused because the lead moved meanwhile: the page shows why and loads it again */
  onMovedMeanwhile: (error: ApiError) => void;
};

function Cancel({ onClose }: { onClose: () => void }) {
  return <button className="btn btn-quiet" type="button" onClick={onClose}>Cancel</button>;
}

/** A dialog whose one button sends one request; a refusal stays in the dialog, unless the lead moved meanwhile. */
function ConfirmDialog({ title, children, action, danger, send, onClose, onDone, onMovedMeanwhile }: {
  title: string;
  children: ReactNode;
  action: ReactNode;
  danger?: boolean;
  send: () => Promise<Lead>;
} & Omit<ChangeProps, 'lead'>) {
  const [notice, setNotice] = useState<string | null>(null);
  const mutation = useMutation({ mutationFn: send });

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (mutation.isPending) return;
    setNotice(null);
    try {
      onDone(await mutation.mutateAsync());
    } catch (error) {
      if (isMovedMeanwhile(error)) onMovedMeanwhile(error);
      else setNotice(error instanceof ApiError ? error.message : UNEXPECTED);
    }
  };

  return (
    <Dialog labelledBy={TITLE} onClose={onClose}>
      <form noValidate onSubmit={e => void submit(e)}>
        <p className="a-heading" id={TITLE}>{title}</p>
        {children}
        {notice && <FormNotice>{notice}</FormNotice>}
        <div className="actions">
          <Cancel onClose={onClose} />
          <SubmitButton busy={mutation.isPending} danger={danger}>{action}</SubmitButton>
        </div>
      </form>
    </Dialog>
  );
}

export function DisqualifyDialog({ lead, ...rest }: ChangeProps) {
  return (
    <ConfirmDialog title={`Disqualify ${lead.name}?`} action={<><Icon icon={Ban} />Disqualify</>} danger send={() => moveLead(lead.id, 'Disqualified')} {...rest}>
      <p>The lead is closed as Disqualified: not a fit for solar, such as a roof that can't take panels. This can't be undone. If they send a new request later, it becomes a new lead.</p>
    </ConfirmDialog>
  );
}

export function LostDialog({ lead, ...rest }: ChangeProps) {
  return (
    <ConfirmDialog title={`Mark ${lead.name} as lost?`} action={<><Icon icon={CircleDashed} />Mark as lost</>} danger send={() => moveLead(lead.id, 'Lost')} {...rest}>
      <p>The lead is closed as Lost: they chose someone else, or stopped answering. This can't be undone. If they send a new request later, it becomes a new lead.</p>
    </ConfirmDialog>
  );
}

/** What converting makes, from what the page already knows: the customer it joins or creates, and the proposed zone. */
export function ConvertDialog({ lead, ...rest }: ChangeProps) {
  const zone = lead.proposedTimeZone;
  return (
    <ConfirmDialog title={`Convert ${lead.name}?`} action={<><Icon icon={CircleCheck} />Convert</>} send={() => convertLead(lead.id)} {...rest}>
      <ul className="made">
        <li>
          <Icon icon={UserRoundCheck} />
          {lead.customer
            ? <span><b>{lead.customer.name} is already a customer</b>, since {formatDay(lead.customer.since)}: the installation is added to their account, and their details stay as they are.</span>
            : <span>A customer, <b>{lead.name}</b>, with this lead's contact details. Prospect until an installation is switched on.</span>}
        </li>
        <li><Icon icon={SolarPanel} /><span>An installation at <b>{lead.address.street}, {cityLine(lead.address)}</b>, at Survey.</span></li>
        <li><Icon icon={CircleCheck} /><span>The lead is closed as Converted. This can't be undone.</span></li>
      </ul>
      {zone && (
        <div className="tz">
          <Icon icon={Globe} />
          <span>Time zone <b>{zone.name}</b> (<code>{zone.id}</code>), from {lead.address.state}. You confirm it on the installation before it's switched on.</span>
        </div>
      )}
    </ConfirmDialog>
  );
}

/** Edit, for a typo: the name, the email or the phone. The property and the visitor's note stay as they were sent. */
export function EditLeadDialog({ lead, onClose, onDone }: Omit<ChangeProps, 'onMovedMeanwhile'>) {
  const mutation = useMutation({ mutationFn: (values: Parameters<typeof updateLead>[1]) => updateLead(lead.id, values) });
  const { form, notice, onSubmit } = useDialogForm({
    schema: editLeadSchema,
    defaultValues: { firstName: lead.firstName, lastName: lead.lastName, email: lead.email, phone: lead.phone },
    emailTaken: 'Another open lead has this email.',
    send: async values => onDone(await mutation.mutateAsync(values)),
  });
  const { errors, isSubmitting } = form.formState;

  return (
    <Dialog labelledBy={TITLE} onClose={onClose}>
      <form noValidate onSubmit={onSubmit}>
        <p className="a-heading" id={TITLE}>Edit {lead.name}</p>
        <p>For a typo: their name, email or phone. The property and their note stay as they sent them.</p>
        {notice && <FormNotice>{notice}</FormNotice>}
        <TextField id="ld-first" label="First name" registration={form.register('firstName')} error={errors.firstName?.message} />
        <TextField id="ld-last" label="Last name" registration={form.register('lastName')} error={errors.lastName?.message} />
        <TextField id="ld-email" label="Email" type="email" registration={form.register('email')} error={errors.email?.message} />
        <TextField id="ld-phone" label="Phone" registration={form.register('phone')} error={errors.phone?.message} />
        <div className="actions">
          <Cancel onClose={onClose} />
          <SubmitButton busy={isSubmitting}>Save</SubmitButton>
        </div>
      </form>
    </Dialog>
  );
}
