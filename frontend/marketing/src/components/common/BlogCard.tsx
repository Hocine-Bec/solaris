import { HiArrowRight } from 'react-icons/hi2';

interface BlogCardProps {
  image: string;
  title: string;
  excerpt: string;
  slug: string;
}

export default function BlogCard({ image, title, excerpt, slug }: BlogCardProps) {
  return (
    <div className="bg-white dark:bg-white/5 backdrop-blur-sm rounded-xl border border-gray-200 dark:border-white/10 overflow-hidden hover:border-primary/50 dark:hover:border-primary/30 transition-all shadow-md hover:shadow-xl dark:shadow-none hover:shadow-primary/20 dark:hover:shadow-primary/10 flex flex-col group">
      {/* Blog Image */}
      <div className="relative h-56 overflow-hidden bg-gray-100 dark:bg-gray-900">
        <img 
          src={image} 
          alt={title}
          className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
        />
      </div>
      
      {/* Card Content */}
      <div className="p-6 flex flex-col flex-grow">
        <h3 className="text-xl font-bold text-gray-900 dark:text-white mb-3 leading-tight">
          {title}
        </h3>
        <p className="text-gray-600 dark:text-gray-400 text-sm leading-relaxed mb-6 flex-grow">
          {excerpt}
        </p>
        
        {/* Read More Link */}
        <a 
          href={`/blog/${slug}`}
          className="inline-flex items-center gap-2 text-primary font-semibold hover:gap-3 transition-all"
        >
          Read More
          <HiArrowRight className="w-5 h-5 group-hover:translate-x-1 transition-transform" />
        </a>
      </div>
    </div>
  );
}