import logo from '../../assets/logo.png';
import { FaLinkedin, FaTwitter, FaInstagram } from 'react-icons/fa';

export default function Footer() {
  return (
    <footer className="relative bg-gray-50 dark:bg-background-dark border-t border-gray-200 dark:border-white/10">
      {/* Gradient background */}
      <div 
        className="absolute inset-0 opacity-20 dark:opacity-30 pointer-events-none" 
        style={{ background: 'radial-gradient(circle at 50% 130%, rgba(242, 185, 13, 0.15) 0%, rgba(242, 185, 13, 0) 60%)' }} 
      />
      
      <div className="relative max-w-7xl mx-auto px-6 sm:px-10 lg:px-16 py-16">
        {/* Footer Grid */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-12 mb-12">
          
          {/* Brand Column */}
          <div>
            <div className="flex items-center gap-3 mb-4">
              <img src={logo} alt="Solaris Logo" className="h-8 w-8" />
              <h3 className="text-xl font-bold text-gray-900 dark:text-white">Solaris</h3>
            </div>
            <p className="text-gray-600 dark:text-gray-400 text-sm leading-relaxed">
              Empowering a cleaner future with intelligent solar solutions.
            </p>
          </div>

          {/* Navigation Column */}
          <div>
            <h4 className="text-gray-900 dark:text-white font-bold mb-4">Navigation</h4>
            <ul className="space-y-3">
              <li>
                <a href="#home" className="text-gray-600 dark:text-gray-400 hover:text-primary dark:hover:text-primary transition-colors text-sm">
                  Home
                </a>
              </li>
              <li>
                <a href="#about" className="text-gray-600 dark:text-gray-400 hover:text-primary dark:hover:text-primary transition-colors text-sm">
                  About
                </a>
              </li>
              <li>
                <a href="#services" className="text-gray-600 dark:text-gray-400 hover:text-primary dark:hover:text-primary transition-colors text-sm">
                  Services
                </a>
              </li>
              <li>
                <a href="#contact" className="text-gray-600 dark:text-gray-400 hover:text-primary dark:hover:text-primary transition-colors text-sm">
                  Contact
                </a>
              </li>
              <li>
                <a href="#careers" className="text-gray-600 dark:text-gray-400 hover:text-primary dark:hover:text-primary transition-colors text-sm">
                  Careers
                </a>
              </li>
            </ul>
          </div>

          {/* Legal Column */}
          <div>
            <h4 className="text-gray-900 dark:text-white font-bold mb-4">Legal</h4>
            <ul className="space-y-3">
              <li>
                <a href="#privacy" className="text-gray-600 dark:text-gray-400 hover:text-primary dark:hover:text-primary transition-colors text-sm">
                  Privacy Policy
                </a>
              </li>
              <li>
                <a href="#terms" className="text-gray-600 dark:text-gray-400 hover:text-primary dark:hover:text-primary transition-colors text-sm">
                  Terms of Service
                </a>
              </li>
            </ul>
          </div>

          {/* Social Column */}
          <div>
            <h4 className="text-gray-900 dark:text-white font-bold mb-4">Follow Us</h4>
            <div className="flex gap-4">
              <a 
                href="https://linkedin.com" 
                target="_blank" 
                rel="noopener noreferrer"
                className="w-10 h-10 rounded-lg border-2 border-primary flex items-center justify-center text-primary hover:shadow-lg hover:shadow-primary/20 dark:hover:shadow-primary/10 hover:bg-primary/20 dark:hover:bg-primary/30 cursor-pointer transition-all"
              >
                <FaLinkedin className="w-5 h-5" />
              </a>
              <a 
                href="https://twitter.com" 
                target="_blank" 
                rel="noopener noreferrer"
                className="w-10 h-10 rounded-lg border-2 border-primary flex items-center justify-center text-primary hover:shadow-lg hover:shadow-primary/20 dark:hover:shadow-primary/10 hover:bg-primary/20 dark:hover:bg-primary/30 cursor-pointer transition-all"
              >
                <FaTwitter className="w-5 h-5" />
              </a>
              <a 
                href="https://instagram.com" 
                target="_blank" 
                rel="noopener noreferrer"
                className="w-10 h-10 rounded-lg border-2 border-primary flex items-center justify-center text-primary hover:shadow-lg hover:shadow-primary/20 dark:hover:shadow-primary/10 hover:bg-primary/20 dark:hover:bg-primary/30 cursor-pointer transition-all"
              >
                <FaInstagram className="w-5 h-5" />
              </a>
            </div>
          </div>
        </div>

        {/* Bottom Bar */}
        <div className="pt-8 border-t border-gray-200 dark:border-white/10">
          <p className="text-center text-gray-500 dark:text-gray-500 text-sm">
            © 2025 <span className='text-primary font-bold'>Solaris</span>. All rights reserved.
          </p>
        </div>
      </div>
    </footer>
  );
}