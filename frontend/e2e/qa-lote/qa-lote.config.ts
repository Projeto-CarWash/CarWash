import { defineConfig, devices } from '@playwright/test';

/**
 * Config DEDICADO ao lote de QA manual-automatizado contra a stack HOM já no ar.
 * NÃO usa webServer (a stack está publicada em http://localhost). Um único login
 * de UI é feito no global-setup e o storageState (cookie httpOnly de refresh) é
 * reusado por todos os testes — respeitando o rate-limit de 10/min do /auth/login.
 */
const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost';

export default defineConfig({
  testDir: '.',
  testMatch: /.*\.spec\.ts/,
  retries: 0,
  workers: 1,
  fullyParallel: false,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: [['list']],
  outputDir: '../../../cards/testes-a-fazer/evidencias/_artifacts',
  use: {
    baseURL,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    ignoreHTTPSErrors: true,
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
