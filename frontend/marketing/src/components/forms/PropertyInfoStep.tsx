import RadioGroup from '../common/RadioGroup';
import type { QuoteFormStepData, FormErrors } from '../../types';
import AddressAutocomplete from '../common/AddressAutoComplete';

interface PropertyInfoStepProps {
  data: QuoteFormStepData;
  errors: FormErrors;
  onChange: (field: keyof QuoteFormStepData, value: string) => void;
}

export default function PropertyInfoStep({ data, errors, onChange }: PropertyInfoStepProps) {
  const propertyTypeOptions = [
    { value: 'house', label: 'House' },
    { value: 'apartment', label: 'Apartment' },
    { value: 'commercial', label: 'Commercial' },
  ];

  const ownershipOptions = [
    { value: 'yes', label: 'Yes' },
    { value: 'no', label: 'No' },
  ];

  return (
    <div className="space-y-6">
      <div className="text-center mb-8">
        <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-2">
          Tell us about your property
        </h2>
        <p className="text-gray-600 dark:text-gray-400">
          This helps us understand your solar potential
        </p>
      </div>

      <AddressAutocomplete
        label="Property Address"
        value={data.propertyAddress}
        onChange={(value: any) => onChange('propertyAddress', value)}
        onAddressSelect={(address: any) => {
          // Store individual address components in state
          onChange('propertyStreet', address.street || '');
          onChange('propertyCity', address.city || '');
          onChange('propertyState', address.state || '');
          onChange('propertyZipCode', address.zipCode || '');
          onChange('propertyCountry', address.country || '');
          onChange('propertyLatitude', address.latitude?.toString() || '');
          onChange('propertyLongitude', address.longitude?.toString() || '');
        }}
        error={errors.propertyAddress}
        placeholder="123 Main St, City, State, ZIP"
        required
      />


      <RadioGroup
        label="Property Type"
        options={propertyTypeOptions}
        value={data.propertyType}
        onChange={(value) => onChange('propertyType', value)}
        error={errors.propertyType}
        required
      />

      <RadioGroup
        label="Do you own this property?"
        options={ownershipOptions}
        value={data.ownsProperty}
        onChange={(value) => onChange('ownsProperty', value)}
        error={errors.ownsProperty}
        required
      />
    </div>
  );
}