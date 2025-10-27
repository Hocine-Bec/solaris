// Navigation link structure
export interface NavLink {
  label: string;
  href: string;
}

// Quote form data 
export interface QuoteFormData {
  fullName: string;
  email: string;
  phone: string;
  projectType: string;
  location: string;
  message: string;
}


// Navigation link structure 
export interface NavLink {
  label: string;
  href: string;
}

// Quote form data
export interface QuoteFormData {
  fullName: string;
  email: string;
  phone: string;
  projectType: string;
  location: string;
  message: string;
}

// Multi-step quote form data
export interface QuoteFormStepData {
  // Step 1: Contact info
  firstName: string;
  lastName: string;
  email: string;
  phone: string;

  // Step 2: Property Info
  propertyAddress: string;
  propertyType: 'House' | 'Apartment' | 'Commercial' | '';
  ownsProperty: 'yes' | 'no' | '';

  propertyStreet?: string;
  propertyCity?: string;
  propertyState?: string;
  propertyZipCode?: string;
  propertyCountry?: string;
  propertyLatitude?: number;
  propertyLongitude?: number;

  // Step 3: Qualification
  electricBill: 'under100' | '100-200' | '200-300' | '300plus' | '';
  contactTime: 'Morning' | 'Afternoon' | 'Evening' | '';
  additionalInfo: string;
}

export interface FormErrors {
  [key: string]: string;
}