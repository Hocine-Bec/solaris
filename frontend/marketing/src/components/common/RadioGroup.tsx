interface RadioOption {
  value: string;
  label: string;
}

interface RadioGroupProps {
  label: string;
  options: RadioOption[];
  value: string;
  onChange: (value: string) => void;
  error?: string;
  required?: boolean;
}

export default function RadioGroup({
  label,
  options,
  value,
  onChange,
  error,
  required = false
}: RadioGroupProps) {
  return (
    <div className="w-full">
      <label className="block text-sm font-semibold text-gray-900 dark:text-white mb-3">
        {label} {required && <span className="text-red-500">*</span>}
      </label>

      <div className="space-y-3">
        {options.map((option) => (
          <label
            key={option.value}
            className={`
              flex items-center p-4 rounded-lg border-2 cursor-pointer transition-all duration-300 backdrop-blur-sm
              ${value === option.value
                ? 'border-primary bg-primary/10 dark:bg-primary/20 shadow-lg shadow-primary/20 dark:shadow-primary/10'
                : 'border-gray-300 dark:border-white/10 hover:border-primary/50 dark:hover:border-primary/30 bg-white dark:bg-gray-800/50 hover:shadow-md hover:shadow-primary/10'
              }
            `}
          >
            <input
              type="radio"
              value={option.value}
              checked={value === option.value}
              onChange={(e) => onChange(e.target.value)}
              className="w-5 h-5 text-primary focus:ring-2 focus:ring-primary cursor-pointer"
            />
            <span className="ml-3 text-gray-900 dark:text-white font-medium">
              {option.label}
            </span>
          </label>
        ))}
      </div>

      {error && (
        <p className="mt-2 text-sm text-red-500 dark:text-red-400 flex items-center gap-1">
          <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
            <path fillRule="evenodd" d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z" clipRule="evenodd" />
          </svg>
          {error}
        </p>
      )}
    </div>
  );
}