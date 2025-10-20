import Input from '../common/Input';
import type { QuoteFormStepData, FormErrors } from '../../types';

interface ContactInfoStepProps {
  data: QuoteFormStepData;
  errors: FormErrors;
  onChange: (field: keyof QuoteFormStepData, value: string) => void;
}

export default function ContactInfoStep({ data, errors, onChange }: ContactInfoStepProps) {
  return (
    <div className="space-y-6">
      <div className="text-center mb-8">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
          Let's get to know you
        </h2>
        <p className="text-gray-600 dark:text-gray-400">
          We'll use this information to prepare your personalized quote
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Input
          label="First Name"
          value={data.firstName}
          onChange={(value) => onChange('firstName', value)}
          error={errors.firstName}
          placeholder="John"
          required
        />
        
        <Input
          label="Last Name"
          value={data.lastName}
          onChange={(value) => onChange('lastName', value)}
          error={errors.lastName}
          placeholder="Doe"
          required
        />
      </div>

      <Input
        label="Email Address"
        type="email"
        value={data.email}
        onChange={(value) => onChange('email', value)}
        error={errors.email}
        placeholder="john.doe@example.com"
        required
      />

      <Input
        label="Phone Number"
        type="tel"
        value={data.phone}
        onChange={(value) => onChange('phone', value)}
        error={errors.phone}
        placeholder="+1 (555) 123-4567"
        required
      />
    </div>
  );
}