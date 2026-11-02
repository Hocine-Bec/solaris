import BlogCard from '../common/BlogCard';
import blog1 from '../../assets/blog-1.jpg';
import blog2 from '../../assets/blog-2.jpg';
import blog3 from '../../assets/blog-3.png';
import blog4 from '../../assets/blog-4.jpg';

const blogPosts = [
  {
    image: blog1,
    title: 'Solar Trends 2025',
    excerpt: 'Explore the latest advancements and predictions shaping the solar energy landscape in 2025.',
    slug: 'solar-trends-2025'
  },
  {
    image: blog2,
    title: 'How to Maximize Energy Efficiency',
    excerpt: 'Discover practical strategies to optimize your solar energy system\'s performance and reduce energy consumption.',
    slug: 'maximize-energy-efficiency'
  },
  {
    image: blog3,
    title: 'The Future of Smart Solar Monitoring',
    excerpt: 'Learn about the innovative technologies and trends driving the evolution of smart solar monitoring systems.',
    slug: 'future-smart-solar-monitoring'
  },
  {
    image: blog4,
    title: 'Solar Panel Maintenance Guide',
    excerpt: 'Keep your solar panels in top condition with our comprehensive maintenance tips and best practices.',
    slug: 'solar-panel-maintenance-guide'
  }
];

export default function BlogSection() {
  return (
    <section className="relative overflow-hidden bg-background-light dark:bg-background-dark py-20 px-6 sm:px-10 lg:px-16">
      {/* Subtle gradient */}
      <div 
        className="absolute inset-0 opacity-15 dark:opacity-80 pointer-events-none"
        style={{ background: 'radial-gradient(circle at 10% 50%, rgba(250, 198, 56, 0.15) 0%, rgba(250, 198, 56, 0) 40%)' }}
      />
      
      <div className="relative max-w-7xl mx-auto">
        {/* Section Header */}
        <div className="text-center mb-16">
          <h2 className="text-4xl lg:text-5xl font-extrabold tracking-tighter text-gray-900 dark:text-white mb-4">
            Insights from the Solar Frontier
          </h2>
          <p className="text-lg text-gray-700 dark:text-gray-300 max-w-2xl mx-auto">
            Your source for the latest news and analysis in solar technology.
          </p>
        </div>

        {/* Blog Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {blogPosts.map((post) => (
            <BlogCard key={post.slug} {...post} />
          ))}
        </div>
      </div>
    </section>
  );
}