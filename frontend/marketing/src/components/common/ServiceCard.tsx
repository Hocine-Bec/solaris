import type { ReactNode } from 'react';

interface ServiceCardProps {
  icon: ReactNode;
  title: string;
  description: string;
}

export default function ServiceCard({ icon, title, description }: ServiceCardProps) {
  return (
    <div className="bg-white dark:bg-white/5 backdrop-blur-sm p-8 rounded-xl border border-gray-200 dark:border-white/10 hover:border-primary/50 dark:hover:border-primary/30 transition-all duration-300 shadow-md hover:shadow-xl dark:shadow-none hover:shadow-primary/20 dark:hover:shadow-primary/10">
      {/* Icon Container */}
      <div className="w-16 h-16 rounded-full bg-primary/30 dark:bg-primary/20 flex items-center justify-center mb-6">
        <div className="text-primary dark:text-primary text-3xl">
          {icon}
        </div>
      </div>
      
      {/* Content */}
      <h3 className="text-xl font-bold text-gray-900 dark:text-white mb-3">
        {title}
      </h3>
      <p className="text-gray-600 dark:text-gray-400 text-sm leading-relaxed">
        {description}
      </p>
    </div>
  );
}