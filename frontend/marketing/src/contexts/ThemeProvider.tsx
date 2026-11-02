import { useState, useEffect, type ReactNode } from 'react';
import { ThemeContext, type Theme } from './theme';

export function ThemeProvider({ children }: { children: ReactNode }) {
  // Initialize theme from localStorage or default to 'dark'
  const [theme, setTheme] = useState<Theme>(() => {
    const savedTheme = localStorage.getItem('theme') as Theme;
    if (savedTheme) return savedTheme;

    // If no saved theme, detect system preference
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    return prefersDark ? 'dark' : 'light';
  });


  // Update localStorage and HTML class whenever theme changes
  useEffect(() => {
    localStorage.setItem('theme', theme);

    const root = document.documentElement;
    if (theme === 'dark') {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
  }, [theme]);

  const toggleTheme = () => {
    setTheme(prev => prev === 'dark' ? 'light' : 'dark');
  };

  return (
    <ThemeContext.Provider value={{ theme, toggleTheme }}>
      {children}
    </ThemeContext.Provider>
  );
}