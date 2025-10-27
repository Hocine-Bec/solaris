import { useState, useEffect, useRef } from 'react';

interface AddressComponents {
  street: string;
  city: string;
  state: string;
  zipCode: string;
  country: string;
  latitude: number;
  longitude: number;
  formattedAddress: string;
}

interface AddressAutocompleteProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  onAddressSelect: (addressComponents: AddressComponents) => void;
  error?: string;
  placeholder?: string;
  required?: boolean;
}

interface GeoapifySuggestion {
  properties: {
    formatted: string;
    address_line1?: string;
    address_line2?: string;
    street?: string;
    housenumber?: string;
    city?: string;
    state?: string;
    postcode?: string;
    country?: string;
    lat: number;
    lon: number;
  };
}

export default function AddressAutocomplete({
  label,
  value,
  onChange,
  onAddressSelect,
  error,
  placeholder,
  required = false
}: AddressAutocompleteProps) {
  const [suggestions, setSuggestions] = useState<GeoapifySuggestion[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [showDropdown, setShowDropdown] = useState(false);
  const debounceTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const wrapperRef = useRef<HTMLDivElement>(null);

  const apiKey = import.meta.env.VITE_GEOAPIFY_API_KEY;

  // Close dropdown when clicking outside
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (wrapperRef.current && !wrapperRef.current.contains(event.target as Node)) {
        setShowDropdown(false);
      }
    }

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  // Fetch suggestions from Geoapify
  const fetchSuggestions = async (searchText: string) => {
    if (!searchText || searchText.length < 3) {
      setSuggestions([]);
      return;
    }

    setIsLoading(true);

    try {
      const response = await fetch(
        `https://api.geoapify.com/v1/geocode/autocomplete?text=${encodeURIComponent(searchText)}&limit=5&apiKey=${apiKey}`
      );

      const data = await response.json();

      if (data.features && Array.isArray(data.features)) {
        setSuggestions(data.features);
        setShowDropdown(true);
      } else {
        setSuggestions([]);
      }
    } catch (err) {
      console.error('Error fetching address suggestions:', err);
      setSuggestions([]);
    } finally {
      setIsLoading(false);
    }
  };

  // Handle input change with debounce
  const handleInputChange = (newValue: string) => {
    onChange(newValue);

    // Clear existing timer
    if (debounceTimer.current) {
      clearTimeout(debounceTimer.current);
    }

    // Set new timer
    debounceTimer.current = setTimeout(() => {
      fetchSuggestions(newValue);
    }, 200); // 300ms debounce
  };

  // Handle suggestion selection
  const handleSelectSuggestion = (suggestion: GeoapifySuggestion) => {
    const props = suggestion.properties;

    // Build street address
    let street = '';
    if (props.housenumber) {
      street = props.housenumber;
    }
    if (props.street) {
      street = street ? `${street} ${props.street}` : props.street;
    }
    // Fallback to address_line1 if street is empty
    if (!street && props.address_line1) {
      street = props.address_line1;
    }

    const addressComponents: AddressComponents = {
      street: street || '',
      city: props.city || '',
      state: props.state || '',
      zipCode: props.postcode || '',
      country: props.country || '',
      latitude: props.lat,
      longitude: props.lon,
      formattedAddress: props.formatted,
    };

    // Update parent
    onChange(props.formatted);
    onAddressSelect(addressComponents);

    // Close dropdown
    setShowDropdown(false);
    setSuggestions([]);
  };

  return (
    <div className="w-full" ref={wrapperRef}>
      <label className="block text-sm font-semibold text-gray-900 dark:text-white mb-2">
        {label} {required && <span className="text-red-500">*</span>}
      </label>

      <div className="relative">
        <input
          type="text"
          value={value}
          onChange={(e) => handleInputChange(e.target.value)}
          placeholder={placeholder || 'Start typing your address...'}
          className={`
            w-full px-4 py-3 rounded-lg border-2 transition-all
            bg-white dark:bg-gray-800
            text-gray-900 dark:text-white
            placeholder:text-gray-400 dark:placeholder:text-gray-500
            focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent
            ${error
              ? 'border-red-500 dark:border-red-500'
              : 'border-gray-300 dark:border-gray-600 hover:border-gray-400 dark:hover:border-gray-500'
            }
          `}
        />

        {/* Loading Spinner */}
        {isLoading && (
          <div className="absolute right-3 top-1/2 transform -translate-y-1/2">
            <div className="w-5 h-5 border-2 border-primary border-t-transparent rounded-full animate-spin" />
          </div>
        )}

        {/* Dropdown Suggestions */}
        {showDropdown && suggestions.length > 0 && (
          <div className="absolute z-50 w-full mt-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-xl max-h-60 overflow-y-auto">
            {suggestions.map((suggestion, index) => (
              <button
                key={index}
                type="button"
                onClick={() => handleSelectSuggestion(suggestion)}
                className="w-full text-left px-4 py-3 hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors border-b border-gray-100 dark:border-gray-700 last:border-b-0"
              >
                <div className="text-sm text-gray-900 dark:text-white font-medium">
                  {suggestion.properties.formatted}
                </div>
              </button>
            ))}
          </div>
        )}

        {/* No Results Message */}
        {showDropdown && !isLoading && suggestions.length === 0 && value.length >= 3 && (
          <div className="absolute z-50 w-full mt-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg shadow-xl p-4">
            <p className="text-sm text-gray-500 dark:text-gray-400">
              No addresses found. Try being more specific.
            </p>
          </div>
        )}
      </div>

      {error && (
        <p className="mt-2 text-sm text-red-500 dark:text-red-400">{error}</p>
      )}

      {!apiKey && (
        <p className="mt-2 text-sm text-red-500">
          Geoapify API key is missing. Please add VITE_GEOAPIFY_API_KEY to your .env file.
        </p>
      )}
    </div>
  );
}