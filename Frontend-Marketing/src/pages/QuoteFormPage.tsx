import { useState } from 'react';
import { Link } from 'react-router-dom';
import Navbar from '../components/layout/Navbar';
import StepIndicator from '../components/forms/StepIndicator';
import ContactInfoStep from '../components/forms/ContactInfoStep';
import PropertyInfoStep from '../components/forms/PropertyInfoStep';
import QualificationStep from '../components/forms/QualificationStep';
import ConfirmationStep from '../components/forms/ConfirmationStep';
import Button from '../components/common/Button';
import type { QuoteFormStepData, FormErrors } from '../types';

export default function QuoteFormPage() {
  const [currentStep, setCurrentStep] = useState(1);
  const [isSubmitted, setIsSubmitted] = useState(false);
  
  // Form data state
  const [formData, setFormData] = useState<QuoteFormStepData>({
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    propertyAddress: '',
    propertyType: '',
    ownsProperty: '',
    electricBill: '',
    contactTime: '',
    additionalInfo: '',
  });

  // Errors state
  const [errors, setErrors] = useState<FormErrors>({});

  // Update form field
  const handleFieldChange = (field: keyof QuoteFormStepData, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear error when user starts typing
    if (errors[field]) {
      setErrors(prev => {
        const newErrors = { ...prev };
        delete newErrors[field];
        return newErrors;
      });
    }
  };

  // Validate Step 1
  const validateStep1 = (): boolean => {
    const newErrors: FormErrors = {};
    
    if (!formData.firstName.trim()) {
      newErrors.firstName = 'First name is required';
    }
    if (!formData.lastName.trim()) {
      newErrors.lastName = 'Last name is required';
    }
    if (!formData.email.trim()) {
      newErrors.email = 'Email is required';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address';
    }
    if (!formData.phone.trim()) {
      newErrors.phone = 'Phone number is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Validate Step 2
  const validateStep2 = (): boolean => {
    const newErrors: FormErrors = {};
    
    if (!formData.propertyAddress.trim()) {
      newErrors.propertyAddress = 'Property address is required';
    }
    if (!formData.propertyType) {
      newErrors.propertyType = 'Please select a property type';
    }
    if (!formData.ownsProperty) {
      newErrors.ownsProperty = 'Please indicate if you own the property';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Validate Step 3
  const validateStep3 = (): boolean => {
    const newErrors: FormErrors = {};
    
    if (!formData.electricBill) {
      newErrors.electricBill = 'Please select your electric bill range';
    }
    if (!formData.contactTime) {
      newErrors.contactTime = 'Please select your preferred contact time';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  // Handle Next button
  const handleNext = () => {
    let isValid = false;

    if (currentStep === 1) {
      isValid = validateStep1();
    } else if (currentStep === 2) {
      isValid = validateStep2();
    }

    if (isValid) {
      setCurrentStep(prev => prev + 1);
      window.scrollTo(0, 0); // Scroll to top on step change
    }
  };

  // Handle Back button
  const handleBack = () => {
    setCurrentStep(prev => prev - 1);
    setErrors({}); // Clear errors when going back
    window.scrollTo(0, 0);
  };

  // Handle form submission
  const handleSubmit = () => {
    if (validateStep3()) {
      // Log form data to console
      console.log('=== QUOTE FORM SUBMISSION ===');
      console.log('Contact Info:', {
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        phone: formData.phone,
      });
      console.log('Property Info:', {
        address: formData.propertyAddress,
        type: formData.propertyType,
        owns: formData.ownsProperty,
      });
      console.log('Qualification:', {
        electricBill: formData.electricBill,
        contactTime: formData.contactTime,
        additionalInfo: formData.additionalInfo,
      });
      console.log('============================');

      // Show confirmation
      setIsSubmitted(true);
      window.scrollTo(0, 0);
    }
  };

  // Render current step
  const renderStep = () => {
    if (isSubmitted) {
      return <ConfirmationStep />;
    }

    switch (currentStep) {
      case 1:
        return (
          <ContactInfoStep
            data={formData}
            errors={errors}
            onChange={handleFieldChange}
          />
        );
      case 2:
        return (
          <PropertyInfoStep
            data={formData}
            errors={errors}
            onChange={handleFieldChange}
          />
        );
      case 3:
        return (
          <QualificationStep
            data={formData}
            errors={errors}
            onChange={handleFieldChange}
          />
        );
      default:
        return null;
    }
  };

  return (
    <div className="relative min-h-screen bg-background-light dark:bg-background-dark overflow-hidden">
      {/* Subtle Radial Gradient Background */}
      <div 
        className="absolute inset-0 opacity-20 dark:opacity-30 pointer-events-none" 
        style={{ background: 'radial-gradient(circle at 50% 20%, rgba(242, 185, 13, 0.2) 0%, rgba(242, 185, 13, 0) 60%)' }} 
      />

      {/* Navbar */}
      <div className="relative z-30">
        <Navbar />
      </div>

      {/* Form Container */}
      <div className="relative max-w-3xl mx-auto px-6 py-20">
        {/* Back to Home Link */}
        {!isSubmitted && (
          <Link 
            to="/" 
            className="inline-flex items-center gap-2 text-gray-600 dark:text-gray-400 hover:text-primary transition-colors mb-8"
          >
            <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
            </svg>
            Back to Home
          </Link>
        )}

        {/* Header */}
        {!isSubmitted && (
          <div className="text-center mb-12">
            <h1 className="text-4xl font-black text-gray-900 dark:text-white mb-4">
              Get Your Free Solar Quote
            </h1>
            <p className="text-lg text-gray-600 dark:text-gray-400">
              Take the first step towards clean, affordable energy
            </p>
          </div>
        )}

        {/* Step Indicator */}
        {!isSubmitted && <StepIndicator currentStep={currentStep} totalSteps={3} />}

        {/* Form Card with Glow Effect */}
        <div className="bg-white dark:bg-white/5 backdrop-blur-sm rounded-2xl border border-gray-200 dark:border-primary/30 p-8 md:p-12 shadow-lg ">
          {renderStep()}

          {/* Navigation Buttons */}
          {!isSubmitted && (
            <div className="flex items-center justify-between gap-4 mt-10">
              {currentStep > 1 ? (
                <Button
                  variant="secondary"
                  size="lg"
                  onClick={handleBack}
                >
                  ← Back
                </Button>
              ) : (
                <div /> // Empty div for spacing
              )}

              {currentStep < 3 ? (
                <Button
                  variant="primary"
                  size="lg"
                  onClick={handleNext}
                >
                  Next →
                </Button>
              ) : (
                <Button
                  variant="primary"
                  size="lg"
                  onClick={handleSubmit}
                >
                  Get My Free Quote!
                </Button>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
}