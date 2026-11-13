import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import '../../brand/fonts.css';
import { App } from './App.tsx';
import './index.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
