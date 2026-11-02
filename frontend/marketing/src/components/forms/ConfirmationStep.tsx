import { FiCheckCircle, FiMail, FiPhone, FiCalendar, FiFileText } from 'react-icons/fi';
import { Link } from 'react-router-dom';
import Button from '../common/Button';

export default function ConfirmationStep() {
  return (
    <div className="text-center py-8">
      {/* Success Icon */}
      <div className="flex justify-center mb-6">
        <div className="w-20 h-20 rounded-full bg-green-500/20 flex items-center justify-center">
          <FiCheckCircle className="w-12 h-12 text-green-500" />
        </div>
      </div>

      {/* Thank You Message */}
      <h2 className="text-3xl font-bold text-gray-900 dark:text-white mb-4">
        Thank you! Your quote request has been received.
      </h2>
      <p className="text-lg text-gray-600 dark:text-gray-400 mb-12">
        We're excited to help you transition to solar energy!
      </p>

      {/* What Happens Next */}
      <div className="max-w-2xl mx-auto mb-12">
        <h3 className="text-xl font-bold text-gray-900 dark:text-white mb-6">
          What happens next?
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="bg-white dark:bg-white/5 p-6 rounded-xl border border-gray-200 dark:border-gray-700">
            <FiMail className="w-8 h-8 text-primary mb-3 mx-auto" />
            <h4 className="font-bold text-gray-900 dark:text-white mb-2">
              1. Review
            </h4>
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Our solar expert will review your information
            </p>
          </div>

          <div className="bg-white dark:bg-white/5 p-6 rounded-xl border border-gray-200 dark:border-gray-700">
            <FiPhone className="w-8 h-8 text-primary mb-3 mx-auto" />
            <h4 className="font-bold text-gray-900 dark:text-white mb-2">
              2. Contact
            </h4>
            <p className="text-sm text-gray-600 dark:text-gray-400">
              We'll contact you within 24 hours
            </p>
          </div>

          <div className="bg-white dark:bg-white/5 p-6 rounded-xl border border-gray-200 dark:border-gray-700">
            <FiCalendar className="w-8 h-8 text-primary mb-3 mx-auto" />
            <h4 className="font-bold text-gray-900 dark:text-white mb-2">
              3. Assessment
            </h4>
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Schedule a free site assessment
            </p>
          </div>

          <div className="bg-white dark:bg-white/5 p-6 rounded-xl border border-gray-200 dark:border-gray-700">
            <FiFileText className="w-8 h-8 text-primary mb-3 mx-auto" />
            <h4 className="font-bold text-gray-900 dark:text-white mb-2">
              4. Quote
            </h4>
            <p className="text-sm text-gray-600 dark:text-gray-400">
              Receive your personalized quote
            </p>
          </div>
        </div>
      </div>

      {/* Email Confirmation */}
      <div className="bg-primary/10 dark:bg-primary/20 rounded-lg p-6 mb-8 max-w-2xl mx-auto">
        <p className="text-gray-900 dark:text-white font-semibold">
          📧 Check your email for confirmation details!
        </p>
      </div>

      {/* Back to Home Button */}
      <Link to="/">
        <Button variant="primary" size="lg">
          Back to Home
        </Button>
      </Link>
    </div>
  );
}