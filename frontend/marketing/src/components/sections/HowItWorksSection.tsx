import TimelineStep from '../common/TimelineStep';
import { HiOutlineClipboardDocumentList } from 'react-icons/hi2';
import { LuPencilRuler } from "react-icons/lu";
import { PiSolarPanelBold } from "react-icons/pi";
import { TbChartAreaLine } from 'react-icons/tb';

export default function HowItWorksSection() {
  return (
    <section className="relative min-h-screen overflow-hidden bg-background-light dark:bg-background-dark py-20 px-6 sm:px-10 lg:px-16">
      {/* Subtle gradient background */}
      <div 
        className="absolute inset-0 opacity-20 dark:opacity-80 pointer-events-none" 
        style={{ background: 'radial-gradient(circle at 50% 50%, rgba(242, 185, 13, 0.15) 0%, rgba(242, 185, 13, 0) 60%)' }} 
      />
      
      <div className="relative max-w-4xl mx-auto">
        {/* Section Header */}
        <div className="text-center mb-20">
          <h2 className="text-4xl lg:text-5xl font-extrabold tracking-tighter text-gray-900 dark:text-white mb-4">
            How Solaris Works
          </h2>
          <p className="text-lg text-gray-700 dark:text-gray-300 max-w-2xl mx-auto">
            A streamlined process from consultation to live monitoring, designed for your convenience.
          </p>
        </div>

        {/* Timeline */}
        <div className="relative">
          {/* Vertical Line */}
          <div className="absolute left-1/2 transform -translate-x-1/2 w-0.5 h-full bg-primary/40 dark:bg-primary" />
          
          {/* Timeline Steps */}
          <div className="relative space-y-16">
            <TimelineStep
              position="left"
              icon={<HiOutlineClipboardDocumentList />}
              title="Request a Quote"
              description="Start with a free, no-obligation consultation."
            />
            <TimelineStep
              position="right"
              icon={<LuPencilRuler />}
              title="Design & Planning"
              description="Customized system design for your property."
            />
            <TimelineStep
              position="left"
              icon={<PiSolarPanelBold />}
              title="Installation"
              description="Professional, certified installation team."
            />
            <TimelineStep
              position="right"
              icon={<TbChartAreaLine />}
              title="Tracking"
              description="Monitor your energy production in real-time."
            />
          </div>
        </div>
      </div>
    </section>
  );
}