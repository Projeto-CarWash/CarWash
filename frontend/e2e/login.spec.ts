import { expect, test } from '@playwright/test';

import { ADMIN_EMAIL, ADMIN_PASSWORD } from './helpers/api';

import type { Page, Response } from '@playwright/test';

/**
 * E2E do fluxo de Login (RF001), executado contra a stack viva via PROXY
 * (baseURL do Playwright). Cobre caminho feliz, persistência de sessão por
 * cookie httpOnly, "lembrar e-mail", erros do backend, validação client-side e
 * proteção de rotas (PrivateRoute).
 *
 * Isolamento: o login da UI seta um cookie httpOnly real (carwash_refresh_token)
 * que o AuthProvider usa no boot para restaurar sessão. Como workers=1 e a stack
 * é compartilhada, cada teste limpa cookies + storage no início para começar
 * deslogado e num estado conhecido — evitando vazamento de sessão entre specs.
 *
 * Rate limit (segurança real, NÃO bug): o endpoint /api/v1/auth/login tem
 * FixedWindow de 10 req/min por IP (Program.cs, policy "auth-login"). A suíte
 * cabe nessa janela numa única execução, mas re-rodar a spec várias vezes em
 * menos de 1 min estoura o limite e o backend devolve 429 ("Muitas
 * tentativas..."). Para não gerar falso-negativo de flakiness, os cenários que
 * dependem de uma resposta de login específica são PULADOS (test.skip) quando o
 * ambiente devolve 429 — isso expõe a causa de ambiente em vez de mascará-la
 * com retry cego.
 */

const RATE_LIMIT_SKIP =
  'Rate limit (429) do /auth/login atingido — janela de 10/min do ambiente, não regressão de produto.';

/** Garante estado deslogado e UI de login estável antes de interagir. */
async function irParaLoginLimpo(page: Page): Promise<void> {
  // Limpa cookies (refresh token httpOnly) e storage da origin do proxy.
  await page.context().clearCookies();
  await page.goto('/login');
  // localStorage só existe após carregar a origin; limpa depois do goto.
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  // Espera o AuthProvider terminar o refresh do boot e a tela renderizar.
  await esperarTelaLogin(page);
}

/** Espera a tela de login estabilizar (após spinner de restauração de sessão). */
async function esperarTelaLogin(page: Page): Promise<void> {
  await expect(page.getByRole('heading', { name: /acesse sua conta/i })).toBeVisible();
}

/** Preenche e-mail e senha pelos labels acessíveis. */
async function preencherCredenciais(page: Page, email: string, senha: string): Promise<void> {
  // `exact: true` evita casar o checkbox "Lembrar meu e-mail neste dispositivo"
  // (e-mail) e o botão "Mostrar senha" (senha), que compartilham o termo no nome.
  await page.getByLabel('E-mail', { exact: true }).fill(email);
  await page.getByLabel('Senha', { exact: true }).fill(senha);
}

/**
 * Clica em "Entrar" e devolve a resposta HTTP do POST /auth/login. Captura a
 * resposta de forma síncrona com o clique (sem timeout fixo) para podermos
 * inspecionar o status real e tratar o 429 de rate limit.
 */
async function submeterLogin(page: Page): Promise<Response | null> {
  const respPromise = page
    .waitForResponse((r) => r.url().includes('/api/v1/auth/login'))
    .catch(() => null);
  await page.getByRole('button', { name: 'Entrar' }).click();
  return respPromise;
}

test.describe('Login (RF001)', () => {
  test.beforeEach(async ({ page }) => {
    await irParaLoginLimpo(page);
  });

  test('1. credenciais válidas redirecionam para /dashboard', async ({ page }) => {
    await preencherCredenciais(page, ADMIN_EMAIL, ADMIN_PASSWORD);
    const resp = await submeterLogin(page);
    test.skip(resp?.status() === 429, RATE_LIMIT_SKIP);

    await expect(page).toHaveURL(/\/dashboard$/);
    // Âncora estável do Dashboard renderizado.
    await expect(page.getByText(/login realizado com sucesso/i)).toBeVisible();
  });

  test('2. sessão persiste após reload (cookie httpOnly restaura via refresh)', async ({
    page,
  }) => {
    await preencherCredenciais(page, ADMIN_EMAIL, ADMIN_PASSWORD);
    const resp = await submeterLogin(page);
    test.skip(resp?.status() === 429, RATE_LIMIT_SKIP);
    await expect(page).toHaveURL(/\/dashboard$/);

    // Reload: o access token está em memória e se perde; o AuthProvider deve
    // restaurar a sessão pelo cookie httpOnly (POST /auth/refresh) e manter logado.
    await page.reload();

    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByText(/login realizado com sucesso/i)).toBeVisible();
  });

  test('3. "lembrar meu e-mail" pré-preenche o campo no próximo acesso a /login', async ({
    page,
  }) => {
    await preencherCredenciais(page, ADMIN_EMAIL, ADMIN_PASSWORD);
    await page.getByLabel(/lembrar meu e-mail/i).check();
    const resp = await submeterLogin(page);
    test.skip(resp?.status() === 429, RATE_LIMIT_SKIP);
    await expect(page).toHaveURL(/\/dashboard$/);

    // Logout limpa a sessão mas o e-mail lembrado fica em localStorage.
    await page.getByRole('button', { name: /sair/i }).click();
    await expect(page).toHaveURL(/\/login$/);
    await esperarTelaLogin(page);

    // O campo deve vir pré-preenchido com o e-mail normalizado (lower/trim).
    await expect(page.getByLabel('E-mail', { exact: true })).toHaveValue(ADMIN_EMAIL.toLowerCase());
  });

  test('4. toggle mostrar/ocultar senha alterna o type do input', async ({ page }) => {
    // Sem login real: não consome rate limit.
    const senha = page.getByLabel('Senha', { exact: true });
    await senha.fill('algumaSenha');
    await expect(senha).toHaveAttribute('type', 'password');

    const toggle = page.getByRole('button', { name: 'Mostrar senha' });
    await toggle.click();
    await expect(senha).toHaveAttribute('type', 'text');
    await expect(page.getByRole('button', { name: 'Ocultar senha' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );

    await page.getByRole('button', { name: 'Ocultar senha' }).click();
    await expect(senha).toHaveAttribute('type', 'password');
  });

  test('5. credenciais inválidas exibem alerta 401 e permanecem em /login', async ({ page }) => {
    await preencherCredenciais(page, ADMIN_EMAIL, 'senha-totalmente-errada');
    const resp = await submeterLogin(page);
    // 429 também consome esta tentativa; se o ambiente limitou, pula (não é o 401 sob teste).
    test.skip(resp?.status() === 429, RATE_LIMIT_SKIP);

    const alerta = page.getByRole('alert');
    await expect(alerta).toBeVisible();
    await expect(alerta).toContainText('Usuário ou senha inválidos.');

    // Sem redirect: continua na tela de login.
    await expect(page).toHaveURL(/\/login$/);
    // O campo senha é limpo após a falha (onSubmit faz setValue('senha', '')).
    await expect(page.getByLabel('Senha', { exact: true })).toHaveValue('');
  });

  test('6. validação client-side bloqueia submit vazio e e-mail inválido (sem navegar)', async ({
    page,
  }) => {
    // Submit vazio: zod onBlur + handleSubmit barram antes de qualquer rede.
    await page.getByRole('button', { name: 'Entrar' }).click();
    await expect(page.getByText('E-mail é obrigatório.')).toBeVisible();
    await expect(page.getByText('Senha é obrigatória.')).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);

    // E-mail mal formado: mensagem específica de formato.
    await page.getByLabel('E-mail', { exact: true }).fill('nao-eh-email');
    await page.getByLabel('Senha', { exact: true }).fill('algumaSenha');
    await page.getByRole('button', { name: 'Entrar' }).click();
    await expect(page.getByText('Informe um e-mail válido.')).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
    // Sem alerta global (nenhuma chamada de login chegou ao backend).
    await expect(page.getByRole('alert')).toHaveCount(0);
  });

  test('7. acessar /dashboard sem sessão redireciona para /login', async ({ page }) => {
    // beforeEach já garantiu cookies/storage limpos. Navega direto à rota protegida.
    await page.goto('/dashboard');
    // PrivateRoute mostra spinner durante isLoading; espera a URL estabilizar.
    await expect(page).toHaveURL(/\/login$/);
    await esperarTelaLogin(page);
  });

  test('8. rota desconhecida sem sessão cai em /login', async ({ page }) => {
    await page.goto('/qualquer-coisa-inexistente');
    await expect(page).toHaveURL(/\/login$/);
    await esperarTelaLogin(page);
  });

  test('9. já autenticado: visitar /login redireciona de volta para /dashboard', async ({
    page,
  }) => {
    await preencherCredenciais(page, ADMIN_EMAIL, ADMIN_PASSWORD);
    const resp = await submeterLogin(page);
    test.skip(resp?.status() === 429, RATE_LIMIT_SKIP);
    await expect(page).toHaveURL(/\/dashboard$/);

    // Com sessão ativa (cookie + memória), o useEffect do Login salta pro dashboard.
    await page.goto('/login');
    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.getByText(/login realizado com sucesso/i)).toBeVisible();
  });
});
