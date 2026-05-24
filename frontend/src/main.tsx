import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';

import './index.css';
import App from './App';
import { ThemeProvider } from './providers/ThemeProvider';

async function enableMocking() {
  if (!import.meta.env.DEV) {
    return;
  }

  try {
    // eslint-disable-next-line no-console
    console.log('[MSW] Tentando iniciar Service Worker...');
    const { worker } = await import('./mocks/browser');
    await worker.start({
      onUnhandledRequest: 'bypass',
    });
    // eslint-disable-next-line no-console
    console.log('[MSW] Service Worker iniciado com sucesso!');
  } catch (err) {
    console.error('[MSW] Erro ao iniciar:', err);
  }
}

void enableMocking().finally(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <ThemeProvider>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </ThemeProvider>
    </StrictMode>,
  );
});
