interface StepIndicatorProps {
  currentStep: number;
  totalSteps: number;
}

export default function StepIndicator({ currentStep, totalSteps }: StepIndicatorProps) {
  return (
    <div className="flex items-center justify-center gap-3 mb-12">
      {[...Array(totalSteps)].map((_, index) => {
        const stepNumber = index + 1;
        const isActive = stepNumber === currentStep;
        const isCompleted = stepNumber < currentStep;
        
        return (
          <div key={stepNumber} className="flex items-center">
            {/* Step Circle */}
            <div
              className={`
                w-10 h-10 rounded-full flex items-center justify-center font-bold text-sm transition-all
                ${isActive ? 'bg-primary text-black scale-110' : ''}
                ${isCompleted ? 'bg-green-500 text-white' : ''}
                ${!isActive && !isCompleted ? 'bg-gray-300 dark:bg-gray-700 text-gray-600 dark:text-gray-400' : ''}
              `}
            >
              {isCompleted ? '✓' : stepNumber}
            </div>
            
            {/* Connector Line */}
            {stepNumber < totalSteps && (
              <div
                className={`
                  w-12 h-1 mx-2 transition-all
                  ${isCompleted ? 'bg-green-500' : 'bg-gray-300 dark:bg-gray-700'}
                `}
              />
            )}
          </div>
        );
      })}
    </div>
  );
}