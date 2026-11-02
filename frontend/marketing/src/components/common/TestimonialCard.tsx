import testiImage1 from '../../assets/testi-image-1.jpg';
import testiImage2 from '../../assets/testi-image-2.jpg';

interface TestimonialCardProps {
  quote: string;
  name: string;
  role: string;
  company: string;
  rating: number;
  imageIndex: 1 | 2;
}

export default function TestimonialCard({ quote, name, role, company, rating, imageIndex }: TestimonialCardProps) {
  const imageSrc = imageIndex === 1 ? testiImage1 : testiImage2;
  
  return (
    <div className="flex-shrink-0 w-[400px] bg-white dark:bg-white/5 backdrop-blur-sm p-8 rounded-xl border border-gray-200 dark:border-primary/20 hover:border-primary/50 dark:hover:border-primary/30 transition-all duration-300 shadow-lg hover:shadow-xl dark:shadow-none dark:hover:shadow-lg hover:shadow-primary/20 dark:hover:shadow-primary/10 flex flex-col">
      {/* Star Rating */}
      <div className="flex gap-1 mb-4">
        {[...Array(5)].map((_, i) => (
          <svg
            key={i}
            className={`w-5 h-5 ${i < rating ? 'text-primary' : 'text-gray-300 dark:text-gray-600'}`}
            fill="currentColor"
            viewBox="0 0 20 20"
          >
            <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
          </svg>
        ))}
      </div>

      {/* Quote - takes up remaining space */}
      <p className="text-gray-700 dark:text-gray-300 italic mb-6 leading-relaxed flex-grow">
        "{quote}"
      </p>

      {/* Author Info - always at bottom */}
      <div className="flex items-center gap-4 mt-auto">
        <img 
          src={imageSrc} 
          alt={name}
          className="w-12 h-12 rounded-full object-cover ring-2 ring-gray-200 dark:ring-transparent"
        />
        <div>
          <p className="text-gray-900 dark:text-white font-semibold">{name}</p>
          <p className="text-gray-600 dark:text-gray-400 text-sm">{role}, {company}</p>
        </div>
      </div>
    </div>
  );
}