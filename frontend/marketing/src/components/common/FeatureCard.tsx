import type { ReactNode } from 'react';

interface FeatureCardProps {
  icon: ReactNode;
  title: string;
  description: string;
}

export default function FeatureCard({ icon, title, description }: FeatureCardProps) {
  return (
    <div className="bg-white dark:bg-white/5 backdrop-blur-sm p-8 rounded-xl border border-gray-200 dark:border-primary/20 hover:border-primary/50 dark:hover:border-primary/30 transition-all duration-300 shadow-md hover:shadow-xl dark:shadow-none hover:shadow-primary/20 dark:hover:shadow-primary/10">
      {/* Icon */}
      <div className="w-14 h-14 rounded-full bg-primary/30 dark:bg-primary/20 flex items-center justify-center mb-6 shadow-sm dark:shadow-none">
        <div className="text-primary text-2xl">
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