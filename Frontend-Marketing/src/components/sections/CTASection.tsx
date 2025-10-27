import Button from '../common/Button';

export default function CTASection() {
  return (
    <section className="relative overflow-hidden bg-background-light dark:bg-background-dark py-32 px-6 sm:px-10 lg:px-16">
      {/* Gradient background */}
      <div 
        className="absolute inset-0 opacity-20 dark:opacity-30 pointer-events-none" 
        style={{ background: 'radial-gradient(circle at 50% 50%, rgba(242, 185, 13, 0.2) 0%, rgba(242, 185, 13, 0) 60%)' }} 
      />
      
      <div className="relative max-w-4xl mx-auto text-center">
        <h2 className="text-4xl sm:text-5xl lg:text-6xl font-black tracking-tighter text-gray-900 dark:text-white mb-6 leading-tight">
          Let's power your business with the sun.
        </h2>
        <p className="text-lg sm:text-xl text-gray-700 dark:text-gray-300 mb-10 max-w-2xl mx-auto">
          Talk to our team today and see how Solaris can simplify your solar journey.
        </p>
        <Button variant="primary" size="lg" to='/quote'>
          Book a Consultation
        </Button>
      </div>
    </section>
  );
}