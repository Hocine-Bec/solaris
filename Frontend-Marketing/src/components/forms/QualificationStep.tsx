import RadioGroup from '../common/RadioGroup';
import Textarea from '../common/Textarea';
import type { QuoteFormStepData, FormErrors } from '../../types';

interface QualificationStepProps {
  data: QuoteFormStepData;
  errors: FormErrors;
  onChange: (field: keyof QuoteFormStepData, value: string) => void;
}

export default function QualificationStep({ data, errors, onChange }: QualificationStepProps) {
  const electricBillOptions = [
    { value: 'under100', label: 'Under $100' },
    { value: '100-200', label: '$100 - $200' },
    { value: '200-300', label: '$200 - $300' },
    { value: '300plus', label: '$300+' },
  ];

  const contactTimeOptions = [
    { value: 'Morning', label: 'Morning (8am-12pm)' },
    { value: 'Afternoon', label: 'Afternoon (12pm-5pm)' },
    { value: 'Evening', label: 'Evening (5pm-8pm)' },
  ];

  return (
    <div className="space-y-6">
      <div className="text-center mb-8">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
          Help us prepare your quote
        </h2>
        <p className="text-gray-600 dark:text-gray-400">
          Just a few more details to personalize your solar solution
        </p>
      </div>

      <RadioGroup
        label="Approximate Monthly Electric Bill"
        options={electricBillOptions}
        value={data.electricBill}
        onChange={(value) => onChange('electricBill', value)}
        error={errors.electricBill}
        required
      />

      <RadioGroup
        label="Best time to contact you"
        options={contactTimeOptions}
        value={data.contactTime}
        onChange={(value) => onChange('contactTime', value)}
        error={errors.contactTime}
        required
      />

      <Textarea
        label="Any questions or concerns?"
        value={data.additionalInfo}
        onChange={(value) => onChange('additionalInfo', value)}
        placeholder="Tell us anything that might help us prepare your quote..."
        rows={5}
      />
    </div>
  );
}