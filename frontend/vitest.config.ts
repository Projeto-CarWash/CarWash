import path from 'path';

import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

/**
 * Configuração do Vitest. Roda em ambiente jsdom (componentes React) e
 * carrega o setup global (jest-dom + MSW). Usa o plugin de React apenas
 * para o transform de JSX — sem o preset de React Compiler do
 * `vite.config.ts`, desnecessário (e custoso) nos testes.
 */
export default defineConfig({
  plugins: [react()],
  esbuild: {
    jsx: 'automatic',
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    css: false,
    include: ['src/**/*.{test,spec}.{ts,tsx}'],
  },
});
