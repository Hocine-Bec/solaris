import { useTheme } from '../../contexts/ThemeContext';
import { HiOutlineSun, HiOutlineMoon } from 'react-icons/hi2';

export default function ThemeToggle() {
  const { theme, toggleTheme } = useTheme();
  
  return (
    <button
      onClick={toggleTheme}
      className="p-2 rounded-lg border-2 border-primary text-primary hover:shadow-lg hover:shadow-primary/10 hover:bg-primary/30 cursor-pointer transition-all"
      aria-label="Toggle theme"
    >
      {theme === 'dark' ? (
        <HiOutlineSun className="w-5 h-5" />
      ) : (
        <HiOutlineMoon className="w-5 h-5" />
      )}
    </button>
  );
}