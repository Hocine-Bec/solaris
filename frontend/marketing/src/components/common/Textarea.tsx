interface TextareaProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  placeholder?: string;
  rows?: number;
  required?: boolean;
}

export default function Textarea({ 
  label, 
  value, 
  onChange, 
  error, 
  placeholder,
  rows = 4,
  required = false 
}: TextareaProps) {
  return (
    <div className="w-full">
      <label className="block text-sm font-semibold text-gray-900 dark:text-white mb-2">
        {label} {required && <span className="text-red-500">*</span>}
      </label>
      <textarea
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        rows={rows}
        className={`
          w-full px-4 py-3 rounded-lg border-2 transition-all duration-300 resize-none
          bg-white dark:bg-gray-800/50 backdrop-blur-sm
          text-gray-900 dark:text-white
          placeholder:text-gray-400 dark:placeholder:text-gray-500
          focus:outline-none
          ${error 
            ? 'border-red-500 dark:border-red-500 shadow-lg shadow-red-500/20' 
            : 'border-gray-300 dark:border-white/10 hover:border-gray-400 dark:hover:border-primary/30 focus:border-primary focus:shadow-lg focus:shadow-primary/20 dark:focus:shadow-primary/10'
          }
        `}
      />
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