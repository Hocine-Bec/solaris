import type { NavLink } from '../../types';
import logo from '../../assets/logo.png';
import Button from '../common/Button';
import ThemeToggle from '../common/ThemeToggle';


const navigationLinks: NavLink[] = [
  { label: 'Solutions', href: '#solutions' },
  { label: 'Products', href: '#products' },
  { label: 'Resources', href: '#resources' },
  { label: 'Pricing', href: '#pricing' },
];

export default function Navbar() {
  return (
    <header className="w-full px-6 sm:px-10 lg:px-16 py-4">
      <nav className="flex items-center justify-between mx-auto max-w-7xl">
        {/* Logo Section */}
        <div className="flex items-center gap-3">
          <img src={logo} alt="Solaris Logo" className="h-8 w-8" />
          <h2 className="text-xl font-bold tracking-tight text-white">Solaris</h2>
        </div>

        {/* Desktop Navigation */}
        <div className="hidden md:flex items-center gap-8">
          {navigationLinks.map((link) => (
            <a key={link.label} href={link.href} className="text-sm font-medium text-gray-300 hover:text-primary transition-colors">
              {link.label}
            </a>
          ))}
        </div>

        {/* CTA Buttons, Theme Toggle, and Mobile Menu */}
        <div className="flex items-center gap-3">
          <div className="hidden sm:flex items-center gap-3">
            <Button variant="primary" size="sm" href="#login">
              Login
            </Button>
            <Button variant="outlined" size="sm" href="#get-started">
              Get Started
            </Button>
          </div>

          <ThemeToggle />

          <button className="md:hidden p-2 rounded-lg text-white hover:bg-primary/20 transition-colors">
            <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16m-7 6h7" />
            </svg>
          </button>
        </div>
      </nav>
    </header>
  );
}