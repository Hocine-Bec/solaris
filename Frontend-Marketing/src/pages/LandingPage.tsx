import Navbar from '../components/layout/Navbar'
import HeroSection from '../components/sections/HeroSection'
import AboutSection from '../components/sections/AboutSection'
import ServicesSection from '../components/sections/SolutionSection'
import HowItWorksSection from '../components/sections/HowItWorksSection'
import TestimonialsSection from '../components/sections/TestimonialsSection'
import WhyChooseSolarisSection from '../components/sections/WhyChooseSolarisSection'
import BlogSection from '../components/sections/BlogSection'
import CTASection from '../components/sections/CTASection'
import Footer from '../components/sections/FooterSection'

function App() {
  return (
    <div className="min-h-screen bg-background-dark">
      <div className="relative">
        <div className="absolute top-0 left-0 right-0 z-30">
          <Navbar />
        </div>
        <HeroSection />
      </div>
      <div className='space-y-[32px]bg-background-light dark:bg-background-dark'>
        <AboutSection />
        <ServicesSection />
        <HowItWorksSection />
        <TestimonialsSection />
        <WhyChooseSolarisSection />
        <BlogSection />
        <CTASection />
        <Footer />
      </div>
    </div>
  )
}

export default App