import heroBg from '../../assets/hero-bg.png';
import Button from '../common/Button';

export default function HeroSection() {
  return (
    <div className="relative min-h-screen w-full overflow-hidden">
      {/* Background Image */}
      <div
        className="absolute inset-0 z-0"
        style={{ backgroundImage: `url(${heroBg})`, backgroundSize: 'cover', backgroundPosition: 'center' }}
      />

      {/* Adaptive Overlay with Blur */}
      <div className="absolute inset-0 z-10 bg-black/25 dark:bg-black/75 backdrop-blur-sm" />

      {/* Content */}
      <div className="relative z-20 flex flex-col min-h-screen">
        <main className="flex-grow flex items-center">
          <div className="w-full max-w-7xl mx-auto px-6 sm:px-10 lg:px-16 py-20 text-center">
            <div className="max-w-5xl mx-auto">
              <h1 className="text-4xl sm:text-5xl lg:text-7xl font-black tracking-tighter leading-tight text-white drop-shadow-lg">
                We bring light to the darkness with <span className="text-primary">Smart Solar</span> Solutions
              </h1>
              <p className="mt-6 max-w-2xl mx-auto text-lg text-gray-100 dark:text-gray-300 drop-shadow-md">
                From installation to real-time tracking, Solaris simplifies your journey to clean energy.
              </p>
              <div className="mt-10 flex flex-col sm:flex-row items-center justify-center gap-4">
                <Button variant="primary" size="lg" to='/quote' className="w-full sm:w-auto">
                  Get a Free Quote
                </Button>
                <Button variant="secondary" size="lg" href="#learn-more" className="w-full sm:w-auto">
                  Learn More
                </Button>
              </div>
            </div>
          </div>
        </main>
      </div>
    </div>
  );
}