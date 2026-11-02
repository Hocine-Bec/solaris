import type { ReactNode } from 'react';

interface TimelineStepProps {
  icon: ReactNode;
  title: string;
  description: string;
  position: 'left' | 'right';
}

export default function TimelineStep({ icon, title, description, position }: TimelineStepProps) {
  return (
    <div className={`flex items-center gap-8 ${position === 'left' ? 'flex-row' : 'flex-row-reverse'}`}>
      {/* Content Side */}
      <div className={`flex-1 ${position === 'left' ? 'text-right' : 'text-left'}`}>
        <div className="inline-block">
          <div className="flex items-center gap-4">
            {position === 'left' && (
              <>
                <div>
                  <h3 className="text-xl font-bold text-gray-900 dark:text-white mb-1">{title}</h3>
                  <p className="text-sm text-gray-600 dark:text-gray-400">{description}</p>
                </div>
                <div className="w-14 h-14 rounded-full bg-primary border-2 border-primary/20 dark:border-transparent flex items-center justify-center flex-shrink-0 shadow-md dark:shadow-none">
                  <div className="text-black text-2xl">{icon}</div>
                </div>
              </>
            )}
            {position === 'right' && (
              <>
                <div className="w-14 h-14 rounded-full bg-primary border-2 border-primary/20 dark:border-transparent flex items-center justify-center flex-shrink-0 shadow-md dark:shadow-none">
                  <div className="text-black text-2xl">{icon}</div>
                </div>
                <div>
                  <h3 className="text-xl font-bold text-gray-900 dark:text-white mb-1">{title}</h3>
                  <p className="text-sm text-gray-600 dark:text-gray-400">{description}</p>
                </div>
              </>
            )}
          </div>
        </div>
      </div>

      {/* Timeline Dot */}
      <div className="w-4 h-4 rounded-full bg-primary border-4 border-white dark:border-primary flex-shrink-0 z-10 shadow-sm dark:shadow-none" />

      {/* Empty Space on Other Side */}
      <div className="flex-1" />
    </div>
  );
} 