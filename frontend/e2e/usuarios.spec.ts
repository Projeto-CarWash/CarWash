import { expect, test } from '@playwright/test';

import { ADMIN_EMAIL, ADMIN_PASSWORD } from './helpers/api';

import type { BrowserContext, Page } from '@playwright/test';

/**
 * E2E do módulo Usuários internos (RF014), contra a stack viva via PROXY
 * (baseURL do Playwright). Diferente de Serviços, o `userService` usa os paths
 * COM prefixo `/api/v1`, então o CRUD pela UI deve funcionar de ponta a ponta:
 * nginx -> backend -> PostgreSQL semeado.
 *
 * Sessão compartilhada (anti rate-limit): o /api/v1/auth/login tem FixedWindow de
 * 10 req/min por IP. Logamos UMA vez via UI no `beforeAll`, mantendo o MESMO
 * context/page vivo para todos os cenários autenticados — não reusamos
 * storageState entre contexts porque o backend ROTACIONA o refresh token a cada
 * /auth/refresh (one-time-use), e um context novo com cookie antigo cairia em
 * 401 -> /login (flakiness). Se o login do beforeAll cair em 429, a suíte
 * autenticada é PULADA honestamente (causa de ambiente, não regressão).
 *
 * Cuidado de dados: o toggle de status e a edição operam SEMPRE no usuário
 * recém-criado nesta run (e-mail único por timestamp). NUNCA tocar o admin
 * semeado — inativá-lo quebraria o login das demais specs.
 */

const RATE_LIMIT_SKIP =
  'Rate limit (429) do /auth/login atingido — janela de 10/min do ambiente, não regressão de produto.';

let loginFalhouPorRateLimit = false;
let contextoAutenticado: BrowserContext | undefined;
let paginaAutenticada: Page | undefined;

/** E-mail/nome únicos por execução para isolar dados entre runs e evitar 409. */
const STAMP = Date.now();
const NOVO_EMAIL = `e2e.usuario.${STAMP}@carwash.local`;
const NOVO_NOME = `Usuario E2E ${STAMP}`;
const NOVO_NOME_EDITADO = `${NOVO_NOME} Editado`;
const SENHA_VALIDA = 'senhaE2E123';

test.beforeAll(async ({ browser }) => {
  const context = await browser.newContext();
  const page = await context.newPage();
  await page.goto('/login');
  await expect(page.getByRole('heading', { name: /acesse sua conta/i })).toBeVisible();
  await page.getByLabel('E-mail', { exact: true }).fill(ADMIN_EMAIL);
  await page.getByLabel('Senha', { exact: true }).fill(ADMIN_PASSWORD);

  const respPromise = page
    .waitForResponse((r) => r.url().includes('/api/v1/auth/login'))
    .catch(() => null);
  await page.getByRole('button', { name: 'Entrar' }).click();
  const resp = await respPromise;

  if (resp?.status() === 429) {
    loginFalhouPorRateLimit = true;
    await context.close();
    return;
  }
  await expect(page).toHaveURL(/\/dashboard$/);
  contextoAutenticado = context;
  paginaAutenticada = page;
});

test.afterAll(async () => {
  await contextoAutenticado?.close();
});

test.describe('Usuários internos (RF014) — autenticado', () => {
  test.beforeEach(() => {
    test.skip(loginFalhouPorRateLimit, RATE_LIMIT_SKIP);
  });

  test('a. /usuarios renderiza o título e carrega a lista do backend (vê o admin semeado)', async () => {
    const page = paginaAutenticada!;
    await page.goto('/usuarios');
    await expect(page).toHaveURL(/\/usuarios$/);
    await expect(page.getByRole('heading', { name: 'Usuários internos' })).toBeVisible();

    // Busca pelo e-mail do admin semeado para confirmar que a lista vem da API.
    await page.getByPlaceholder(/buscar por nome/i).fill(ADMIN_EMAIL);
    await expect(page.getByRole('link', { name: /.+/ }).first()).toBeVisible();
    await expect(page.getByText(ADMIN_EMAIL)).toBeVisible();
  });

  test('c. criar usuário com e-mail único redireciona ao dashboard e aparece na lista', async () => {
    const page = paginaAutenticada!;
    await page.goto('/usuarios/novo');
    await expect(page.getByRole('heading', { name: 'Novo usuário interno' })).toBeVisible();

    await page.getByLabel('Nome completo').fill(NOVO_NOME);
    await page.getByLabel('E-mail').fill(NOVO_EMAIL);
    await page.getByLabel('Senha inicial').fill(SENHA_VALIDA);
    await page.getByLabel('Confirmar senha').fill(SENHA_VALIDA);
    // Perfil já vem "Funcionário" por default.

    await page.getByRole('button', { name: /salvar usuário/i }).click();

    // Sucesso e redirect (setTimeout ~2s na página; toHaveURL aguarda).
    await expect(page.getByText('Usuário cadastrado com sucesso! Redirecionando...')).toBeVisible();
    await expect(page).toHaveURL(/\/dashboard$/);

    // Navega para a lista e busca pelo e-mail criado: deve aparecer.
    await page.goto('/usuarios');
    await page.getByPlaceholder(/buscar por nome/i).fill(NOVO_EMAIL);
    await expect(page.getByRole('link', { name: NOVO_NOME })).toBeVisible();
    await expect(page.getByText(NOVO_EMAIL)).toBeVisible();
  });

  test('d. abrir detalhe do usuário criado, editar o nome e salvar mostra sucesso', async () => {
    const page = paginaAutenticada!;
    await page.goto('/usuarios');
    await page.getByPlaceholder(/buscar por nome/i).fill(NOVO_EMAIL);
    await page.getByRole('link', { name: NOVO_NOME }).click();

    await expect(page).toHaveURL(/\/usuarios\/[0-9a-f-]+$/);
    const nome = page.getByLabel('NOME');
    await expect(nome).toHaveValue(NOVO_NOME);

    await nome.fill(NOVO_NOME_EDITADO);
    await page.getByRole('button', { name: /salvar alterações/i }).click();

    await expect(page.getByText('Dados atualizados com sucesso.')).toBeVisible();
  });

  test('e. toggle de status no usuário criado (NUNCA no admin) mostra inativado/ativado', async () => {
    const page = paginaAutenticada!;
    await page.goto('/usuarios');
    await page.getByPlaceholder(/buscar por nome/i).fill(NOVO_EMAIL);
    // O nome pode ter sido editado pelo cenário anterior; casa pelo prefixo.
    await page
      .getByRole('link', { name: new RegExp(`^${NOVO_NOME}`) })
      .first()
      .click();

    await expect(page).toHaveURL(/\/usuarios\/[0-9a-f-]+$/);

    // Estado inicial: usuário criado vem ATIVO. Inativa.
    const sw = page.getByRole('switch', { name: 'Inativar usuário' });
    await expect(sw).toBeVisible();
    await sw.click();
    await expect(
      page.getByText('Usuário inativado com sucesso. O acesso ao sistema foi bloqueado.'),
    ).toBeVisible();

    // Reativa para deixar o dado num estado conhecido.
    const swAtivar = page.getByRole('switch', { name: 'Ativar usuário' });
    await expect(swAtivar).toBeVisible();
    await swAtivar.click();
    await expect(
      page.getByText('Usuário ativado com sucesso. O acesso ao sistema foi restaurado.'),
    ).toBeVisible();
  });

  test('f. filtro de status "Inativos" recarrega a lista (param ativo=false)', async () => {
    const page = paginaAutenticada!;
    await page.goto('/usuarios');

    const respPromise = page.waitForResponse(
      (r) =>
        new URL(r.url()).pathname === '/api/v1/usuarios' &&
        new URL(r.url()).searchParams.get('ativo') === 'false',
    );
    await page.getByRole('button', { name: 'Inativos' }).click();
    const resp = await respPromise;
    expect(resp.ok()).toBe(true);
  });
});

test.describe('Usuários internos (RF014) — proteção de rota', () => {
  test('b. /usuarios e /usuarios/novo sem sessão redirecionam para /login', async ({ browser }) => {
    // Contexto NOVO e limpo (sem storageState) = deslogado.
    const context = await browser.newContext();
    const page = await context.newPage();
    try {
      await page.goto('/usuarios');
      await expect(page).toHaveURL(/\/login$/);
      await expect(page.getByRole('heading', { name: /acesse sua conta/i })).toBeVisible();

      await page.goto('/usuarios/novo');
      await expect(page).toHaveURL(/\/login$/);
    } finally {
      await context.close();
    }
  });
});
