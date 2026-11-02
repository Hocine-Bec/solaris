import TestimonialCard from '../common/TestimonialCard';

const testimonials = [
  {
    quote: "Solaris has completely transformed our project management. The platform is intuitive, powerful, and has significantly boosted our efficiency from day one. It's a game-changer.",
    name: "Sarah Chen",
    role: "CEO",
    company: "Bright Future Energy",
    rating: 5
  },
  {
    quote: "The monitoring tools provided by Solaris are second to none. We can track performance in real-time, anticipate maintenance needs, and provide unparalleled service to our clients.",
    name: "David Lee",
    role: "CTO",
    company: "Solar Solutions Inc.",
    rating: 5
  },
  {
    quote: "As a project manager, my workflow has been streamlined immensely. Solaris's software is a must-have for any serious solar installation company.",
    name: "Emily Rodriguez",
    role: "Project Manager",
    company: "GreenTech Innovations",
    rating: 5
  },
  {
    quote: "The customer support team at Solaris is exceptional. They're always ready to help and ensure our systems run smoothly without any downtime.",
    name: "Michael Thompson",
    role: "Operations Director",
    company: "SunPower Solutions",
    rating: 5
  },
  {
    quote: "Implementing Solaris reduced our installation time by 30%. The ROI was immediate and our clients are happier than ever.",
    name: "Jessica Martinez",
    role: "Installation Manager",
    company: "EcoEnergy Systems",
    rating: 5
  },
  {
    quote: "The analytics dashboard gives us insights we never had before. Data-driven decisions have become our competitive advantage.",
    name: "Robert Kim",
    role: "Data Analyst",
    company: "Solar Metrics Co.",
    rating: 5
  },
  {
    quote: "Training our team on Solaris was effortless. The intuitive interface means even non-technical staff can use it effectively.",
    name: "Amanda Foster",
    role: "Training Coordinator",
    company: "Renewable Solutions Ltd.",
    rating: 5
  },
  {
    quote: "Solaris has become the backbone of our operations. We can't imagine going back to our old systems.",
    name: "Daniel Park",
    role: "VP of Operations",
    company: "Clean Energy Partners",
    rating: 5
  },
  {
    quote: "The mobile app allows our field technicians to update project status in real-time. This has eliminated communication gaps entirely.",
    name: "Lisa Anderson",
    role: "Field Operations Manager",
    company: "SolarTech Pro",
    rating: 5
  },
  {
    quote: "Best investment we've made in years. Solaris pays for itself within months through improved efficiency and client satisfaction.",
    name: "James Wilson",
    role: "CFO",
    company: "Sustainable Power Group",
    rating: 5
  }
];

export default function TestimonialsSection() {
  return (
    <section className="relative overflow-hidden bg-background-light dark:bg-background-dark py-20">
      {/* Subtle gradient background */}
      <div 
        className="absolute inset-0 opacity-15 dark:opacity-80 pointer-events-none"
        style={{ background: 'radial-gradient(circle at 50% 60%, rgba(250, 198, 56, 0.15) 0%, rgba(250, 198, 56, 0) 40%)' }}
      />
      
      {/* Section Header */}
      <div className="relative text-center mb-16 px-6">
        <h2 className="text-4xl lg:text-5xl font-extrabold tracking-tighter text-gray-900 dark:text-white mb-4">
          Trusted by Installers and Innovators Worldwide
        </h2>
        <p className="text-lg text-gray-700 dark:text-gray-300 max-w-2xl mx-auto">
          Hear what our partners have to say about their experience with Solaris.
        </p>
      </div>

      {/* Scrolling Container */}
      <div className="relative">
        <div className="flex gap-6 animate-scroll">
          {/* First set of testimonials */}
          {testimonials.map((testimonial, index) => (
            <TestimonialCard
              key={`first-${index}`}
              {...testimonial}
              imageIndex={index % 2 === 0 ? 1 : 2}
            />
          ))}
          {/* Duplicate set for seamless loop */}
          {testimonials.map((testimonial, index) => (
            <TestimonialCard
              key={`second-${index}`}
              {...testimonial}
              imageIndex={index % 2 === 0 ? 1 : 2}
            />
          ))}
        </div>
      </div>

      {/* Disclaimer */}
      <p className="relative text-center text-gray-500 dark:text-gray-500 text-sm mt-12 px-6">
        These testimonials reflect individual experiences and do not guarantee similar outcomes for all users.
      </p>
    </section>
  );
}