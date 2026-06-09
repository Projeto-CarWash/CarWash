import '@testing-library/jest-dom/vitest';
import { afterAll, afterEach, beforeAll } from 'vitest';

import { server } from './mswServer';

/**
 * Setup global dos testes.
 *
 * <p>O servidor MSW intercepta as chamadas HTTP. `onUnhandledRequest: 'error'`
 * garante que nenhum teste vaze chamada de rede real sem handler explícito.</p>
 */
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

const originalConsoleError = console.error;
console.error = (...args: any[]) => {
  if (typeof args[0] === 'string' && args[0].includes('was not wrapped in act(...)')) {
    return;
  }
  originalConsoleError(...args);
};
