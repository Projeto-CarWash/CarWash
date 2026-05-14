import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import './index.css';
import { ThemeProvider } from './providers/ThemeProvider.tsx';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider>{null}</ThemeProvider>
  </StrictMode>,
);
