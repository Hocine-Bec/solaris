import aboutImg from '../../assets/about.jpg';
import Button from '../common/Button';
import StatCard from '../common/StatCard';

export default function AboutSection() {
  return (
    <section className="relative min-h-screen overflow-hidden bg-background-light dark:bg-background-dark">
      {/* Subtle gradient background */}
      <div 
        className="absolute inset-0 opacity-30 dark:opacity-80"
        style={{ background: 'radial-gradient(circle at 10% 20%, rgba(250, 198, 56, 0.15) 0%, rgba(250, 198, 56, 0) 40%)' }}
      />

      <div className="relative flex flex-col justify-center min-h-screen px-6 py-16 sm:px-10 lg:px-16">
        <div className="max-w-6xl mx-auto w-full">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-12 lg:gap-16 items-center">

            {/* Left Column - Content */}
            <div className="flex flex-col gap-8">
              <div className="space-y-4">
                <h2 className="text-4xl lg:text-5xl font-extrabold tracking-tighter text-gray-900 dark:text-white">
                  Built for Efficiency, Designed for the Planet.
                </h2>
                <p className="text-lg text-gray-700 dark:text-gray-300">
                  Solaris is dedicated to making solar adoption effortless through intelligent software.
                  We streamline the process, from installation to monitoring, ensuring a seamless transition
                  to clean energy for a brighter, more sustainable future.
                </p>
              </div>

              {/* Stats Grid */}
              <div className="grid grid-cols-2 gap-6">
                <StatCard value="+500" label="homes powered" />
                <StatCard value="25%" label="faster installations" />
              </div>

              {/* CTA Button */}
              <div>
                <Button variant="primary" size="md">
                  Learn More
                </Button>
              </div>
            </div>

            {/* Right Column - Image */}
            <div className="w-full h-full">
              <div
                className="w-full aspect-square bg-center bg-no-repeat bg-cover rounded-xl shadow-2xl shadow-gray-300/50 dark:shadow-primary/10"
                style={{ backgroundImage: `url(${aboutImg})` }}
              />
            </div>

          </div>
        </div>
      </div>
    </section>
  );
}