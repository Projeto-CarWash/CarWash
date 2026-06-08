import { defineConfig, devices } from '@playwright/test';

/**
 * Config DEDICADO ao lote QA RF008 (agendamentos simultâneos) contra a stack HOM
 * já no ar (http://localhost). NÃO usa webServer — a stack está publicada. Um
 * único login de UI é feito no beforeAll do spec e o MESMO contexto é reusado
 * (serial), respeitando o rate-limit de 10/min do /auth/login e a rotação de
 * refresh token do backend.
 */
const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost';

export default defineConfig({
  testDir: '.',
  testMatch: /.*\.spec\.ts/,
  retries: 0,
  workers: 1,
  fullyParallel: false,
  timeout: 90_000,
  expect: { timeout: 15_000 },
  reporter: [['list']],
  outputDir: '../../../cards/rf008/evidencias/_artifacts',
  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    ignoreHTTPSErrors: true,
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
