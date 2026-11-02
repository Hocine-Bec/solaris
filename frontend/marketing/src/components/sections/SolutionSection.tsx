import ServiceCard from '../common/ServiceCard';
import { LuFactory } from 'react-icons/lu';
import { HiOutlineBriefcase } from 'react-icons/hi2';
import { BsCalendarCheck } from 'react-icons/bs';
import { TbChartAreaLine } from 'react-icons/tb';

export default function SolutionSection() {
  return (
    <section className="relative min-h-screen overflow-hidden bg-background-light dark:bg-background-dark py-20 px-6 sm:px-10 lg:px-16">
      {/* Subtle gradient background - bottom right */}
      <div 
        className="absolute inset-0 opacity-20 dark:opacity-80 pointer-events-none"
        style={{ background: 'radial-gradient(circle at 90% 50%, rgba(250, 198, 56, 0.15) 0%, rgba(250, 198, 56, 0) 40%)' }}
      />
      
      <div className="relative max-w-6xl mx-auto">
        {/* Section Header */}
        <div className="text-center mb-16">
          <h2 className="text-4xl lg:text-5xl font-extrabold tracking-tighter text-gray-900 dark:text-white mb-6">
            Our Solution
          </h2>
          <p className="text-lg text-gray-700 dark:text-gray-300 max-w-3xl mx-auto">
            A complete suite of tools to manage your solar business, from installation to long-term maintenance.
          </p>
        </div>

        {/* Services Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <ServiceCard
            icon={<LuFactory />}
            title="Solar Installation"
            description="Expert installation for homes and businesses."
          />
          <ServiceCard
            icon={<HiOutlineBriefcase />}
            title="Project Management CRM"
            description="Streamline your solar projects with our CRM."
          />
          <ServiceCard
            icon={<BsCalendarCheck />}
            title="Maintenance Scheduling"
            description="Manage maintenance for your solar systems."
          />
          <ServiceCard
            icon={<TbChartAreaLine />}
            title="Energy Monitoring"
            description="Track energy production and consumption"
          />
        </div>
      </div>
    </section>
  );
}