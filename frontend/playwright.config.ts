import { defineConfig, devices } from '@playwright/test';

/**
 * Configuração do Playwright para os E2E do CarWash.
 *
 * O baseURL aponta para o PROXY (nginx), não para o container do frontend:
 * só o proxy serve o SPA e roteia `/api/*` para o backend no mesmo origin —
 * exatamente como em homologação/produção. Apontar direto para o frontend
 * (:8080) quebraria as chamadas `/api/...` por falta do upstream.
 *
 * `E2E_BASE_URL` permite sobrescrever (ex.: rodar contra hml remoto). Default
 * é o proxy publicado em http://proxy (dentro da rede compose) ou localhost
 * (quando rodado fora do compose contra a stack já no ar).
 */
const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost';

export default defineConfig({
  testDir: './e2e',
  // Suítes de QA manual (lote de bugs front e RF008) rodam contra uma stack já no ar
  // via seus próprios configs (e2e/qa-lote/qa-lote.config.ts, e2e/qa-rf008/qa-rf008.config.ts).
  // Não fazem parte do E2E padrão do CI (make test-e2e) — ignoradas aqui para não
  // serem coletadas pelo testDir.
  testIgnore: ['**/qa-lote/**', '**/qa-rf008/**'],
  // Sem retries locais para não mascarar flakiness; 1 retry no CI cobre flutuação
  // de rede do ambiente efêmero — o trace do retry expõe a causa real.
  retries: process.env.CI ? 1 : 0,
  // E2E compartilham a mesma stack/banco semeado; workers=1 evita corrida entre
  // specs que criam dados conflitantes (ex.: conflito global de veículo).
  workers: 1,
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['junit', { outputFile: 'playwright-report/results.xml' }],
  ],
  use: {
    baseURL,
    // Trace/vídeo só em falha (e no 1º retry) — barato em verde, diagnóstico em vermelho.
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
    // O proxy de hml usa cert auto-assinado quando em HTTPS; ignoramos para o E2E.
    ignoreHTTPSErrors: true,
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
  ],
});
