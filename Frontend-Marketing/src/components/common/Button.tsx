import { Link } from "react-router-dom";

interface ButtonProps {
  children: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'outlined';
  size?: 'sm' | 'md' | 'lg';
  href?: string;
  to?: string;
  onClick?: () => void;
  className?: string;
}

export default function Button({
  children,
  variant = 'primary',
  size = 'md',
  href,
  to,
  onClick,
  className = ''
}: ButtonProps) {

  const baseStyles = 'inline-flex items-center justify-center font-bold rounded-lg transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary';

  const variantStyles = {
    primary: 'bg-primary text-black hover:bg-primary/90 hover:shadow-lg hover:shadow-primary/20 cursor-pointer',
    secondary: 'bg-gray-200 dark:bg-white/10 text-gray-900 dark:text-white hover:bg-gray-300 dark:hover:bg-white/20 hover:shadow-lg cursor-pointer',
    outlined: 'border-2 border-primary text-primary bg-transparent hover:shadow-lg hover:shadow-primary/20 hover:bg-primary/10 dark:hover:bg-primary/30 cursor-pointer'
  };

  const sizeStyles = {
    sm: 'px-5 py-2 text-sm',
    md: 'px-6 py-3 text-base',
    lg: 'px-8 py-4 text-base'
  };

  const classes = `${baseStyles} ${variantStyles[variant]} ${sizeStyles[size]} ${className}`;

  // If 'to' prop is provided, use React Router Link
  if (to) {
    return (
      <Link to={to} className={classes}>
        {children}
      </Link>
    );
  }

  // If href is provided, render as external link
  if (href) {
    return (
      <a href={href} className={classes}>
        {children}
      </a>
    );
  }

  // Otherwise render as button
  return (
    <button onClick={onClick} className={classes}>
      {children}
    </button>
  );
}