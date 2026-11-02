import FeatureCard from '../common/FeatureCard';
import ComparisonTable from '../common/ComparisonTable';
import { TbClockBolt } from 'react-icons/tb';
import { HiOutlineChatBubbleLeftRight } from 'react-icons/hi2';
import { TbChartBar } from 'react-icons/tb';
import { IoLeafOutline } from 'react-icons/io5';

export default function WhyChooseSolarisSection() {
  return (
    <section className="relative overflow-hidden bg-background-light dark:bg-background-dark py-20 px-6 sm:px-10 lg:px-16">
      {/* Subtle gradient */}
      <div 
        className="absolute inset-0 opacity-15 dark:opacity-20 pointer-events-none" 
        style={{ background: 'radial-gradient(circle at 80% 20%, rgba(242, 185, 13, 0.15) 0%, rgba(242, 185, 13, 0) 50%)' }} 
      />
      
      <div className="relative max-w-6xl mx-auto">
        {/* Section Header */}
        <div className="text-center mb-16">
          <h2 className="text-4xl lg:text-5xl font-extrabold tracking-tighter text-gray-900 dark:text-white mb-4">
            Why Choose Solaris?
          </h2>
          <p className="text-lg text-gray-700 dark:text-gray-300 max-w-3xl mx-auto">
            Solaris is committed to providing top-tier solar solutions, from seamless installation to insightful energy management. Here's why we stand out.
          </p>
        </div>

        {/* Feature Cards Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-20">
          <FeatureCard
            icon={<TbClockBolt />}
            title="Fast Installation Tracking"
            description="Real-time updates on your project's progression, ensuring timely completion and minimal disruption."
          />
          <FeatureCard
            icon={<HiOutlineChatBubbleLeftRight />}
            title="Transparent Communication"
            description="Clear and consistent communication throughout the process, keeping you informed every step of the way."
          />
          <FeatureCard
            icon={<TbChartBar />}
            title="Data-Driven Energy Insights"
            description="Actionable insights into your energy production and consumption, optimizing performance."
          />
          <FeatureCard
            icon={<IoLeafOutline />}
            title="Sustainable Impact"
            description="Contribute to a greener future with our eco-friendly solutions, reducing your carbon footprint."
          />
        </div>

        {/* Comparison Table */}
        <ComparisonTable />
      </div>
    </section>
  );
}