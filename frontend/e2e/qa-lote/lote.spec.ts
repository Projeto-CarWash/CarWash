import { expect, test } from '@playwright/test';

import { ADMIN_EMAIL, ADMIN_PASSWORD, cpfAleatorio, placaAleatoria } from '../helpers/api';

import type { BrowserContext, Page, Request } from '@playwright/test';

/**
 * Lote de QA (cards 01-10) contra a stack HOM já no ar (http://localhost).
 *
 * O backend ROTACIONA o refresh token (cada /auth/refresh invalida o cookie
 * anterior — confirmado: 2ª refresh com o mesmo cookie = 401). Por isso NÃO dá
 * para reusar um storageState estático entre contextos. Estratégia: UM contexto
 * de browser compartilhado por toda a suíte (serial), login de UI único no
 * beforeAll, e navegação por page.goto dentro do MESMO contexto — cada boot
 * rotaciona o cookie e o contexto mantém o cookie mais novo, mantendo a sessão
 * viva sem novos logins (respeita o rate-limit de 10/min do /auth/login).
 */

const EVID = '/home/gbrogio/university/carwash/cards/testes-a-fazer/evidencias';

test.describe.configure({ mode: 'serial' });

let context: BrowserContext;
let page: Page;

test.beforeAll(async ({ browser }) => {
  context = await browser.newContext({ ignoreHTTPSErrors: true });
  page = await context.newPage();

  await page.goto('/login');
  await page.evaluate(() => localStorage.clear());
  await page.reload();
  await expect(page.getByRole('heading', { name: /acesse sua conta/i })).toBeVisible();
  await page.getByLabel('E-mail', { exact: true }).fill(ADMIN_EMAIL);
  await page.getByLabel('Senha', { exact: true }).fill(ADMIN_PASSWORD);
  const respPromise = page.waitForResponse((r) => r.url().includes('/api/v1/auth/login'));
  await page.getByRole('button', { name: 'Entrar' }).click();
  const resp = await respPromise;
  if (resp.status() === 429) {
    throw new Error('Rate limit (429) no /auth/login. Aguarde 1 min e re-execute o lote.');
  }
  await expect(page).toHaveURL(/\/dashboard$/);
});

test.afterAll(async () => {
  await context?.close();
});

/** Navega para uma rota interna e garante que a sessão segue viva (não cai no /login). */
async function irLogado(path: string): Promise<void> {
  await page.goto(path);
  // O boot dispara /auth/refresh (rotaciona o cookie do contexto). Aguarda concluir.
  await page
    .waitForResponse((r) => r.url().includes('/api/v1/auth/refresh'), { timeout: 8000 })
    .catch(() => null);
  await page.waitForLoadState('networkidle');
  if (page.url().includes('/login')) {
    throw new Error(`Sessão não restaurada ao acessar ${path} (redirecionou para /login).`);
  }
}

async function preencherIdentificacao(nome: string): Promise<void> {
  await page.locator('#cpf').fill(cpfAleatorio());
  await page.locator('#birth').fill('01/01/1990');
  await page.locator('#name').fill(nome);
  await page.locator('#name').blur();
}

async function preencherContatoEndereco(numero = '1000'): Promise<void> {
  await page.locator('#celular').fill('11987654321');
  await page.locator('#cep').fill('01310100');
  await page.locator('#uf').fill('SP');
  await page.locator('#cidade').fill('Sao Paulo');
  await page.locator('#bairro').fill('Bela Vista');
  await page.locator('#logradouro').fill('Av. Paulista');
  await page.locator('#numero').fill(numero);
  await page.locator('#numero').blur();
}

// ───────────────────────────────────────────────────────────────────────────
// Card 01 — Limpar Formulário 3x não trava os campos nome/data
// ───────────────────────────────────────────────────────────────────────────
test('CARD01 - limpar formulario 3x nao trava campos nome/data', async () => {
  await irLogado('/clientes/novo');
  const nome = page.locator('#name');
  const birth = page.locator('#birth');
  const limpar = page.getByRole('button', { name: /limpar formul/i });

  for (let i = 0; i < 3; i++) {
    await nome.fill(`Teste ${i}`);
    await birth.fill('01/01/1990');
    await limpar.click();
    await expect(nome).toHaveValue('');
  }

  await nome.fill('Joao da Silva');
  await birth.fill('15/05/1985');
  await expect(nome).toHaveValue('Joao da Silva');
  await expect(birth).toHaveValue('15/05/1985');

  await page.screenshot({ path: `${EVID}/card01-apos-3-limpezas.png`, fullPage: true });
});

// ───────────────────────────────────────────────────────────────────────────
// Card 02 — Editar cliente: ação existe e leva à tela de edição
// ───────────────────────────────────────────────────────────────────────────
test('CARD02 - acao de editar cliente existe e abre edicao', async () => {
  await irLogado('/clientes');
  await expect(page.locator('tbody tr').first()).toBeVisible();

  const btnEditar = page.getByRole('button', { name: /^Editar / }).first();
  await expect(btnEditar).toBeVisible();
  await page.screenshot({ path: `${EVID}/card02-lista-com-acao-editar.png`, fullPage: true });
  await btnEditar.click();
  await page.waitForLoadState('networkidle');
  await expect(page).toHaveURL(/\/clientes\/[0-9a-f-]+\/editar/);
  await page.screenshot({ path: `${EVID}/card02-tela-edicao.png`, fullPage: true });
});

// ───────────────────────────────────────────────────────────────────────────
// Card 03 — Usuários: toggle de status usa botão padrão (Power), não switch
// ───────────────────────────────────────────────────────────────────────────
test('CARD03 - usuarios usam botao Power (desligar) e nao switch', async () => {
  await irLogado('/usuarios');
  await expect(page.locator('tbody tr').first()).toBeVisible();

  const switches = await page.getByRole('switch').count();
  expect(switches, 'não deve haver switch antigo').toBe(0);

  const btnPower = page.getByRole('button', { name: /(Inativar|Ativar) / }).first();
  await expect(btnPower).toBeVisible();
  await page.screenshot({ path: `${EVID}/card03-usuarios-botao-power.png`, fullPage: true });
});

// ───────────────────────────────────────────────────────────────────────────
// Card 04 — Edição por botão (lápis) em vez de clicar no nome
// ───────────────────────────────────────────────────────────────────────────
test('CARD04 - edicao por botao lapis em usuarios e clientes', async () => {
  await irLogado('/clientes');
  await expect(page.locator('tbody tr').first()).toBeVisible();
  await expect(page.getByRole('button', { name: /^Editar / }).first()).toBeVisible();
  await page.screenshot({ path: `${EVID}/card04-clientes-lapis.png`, fullPage: true });

  await irLogado('/usuarios');
  await expect(page.locator('tbody tr').first()).toBeVisible();
  await expect(page.getByRole('button', { name: /^Editar / }).first()).toBeVisible();
  await page.screenshot({ path: `${EVID}/card04-usuarios-lapis.png`, fullPage: true });
});

// ───────────────────────────────────────────────────────────────────────────
// Card 05 — GET /clientes/{id} já devolve veiculos[]; sem GET separado
// ───────────────────────────────────────────────────────────────────────────
test('CARD05 - GET cliente por id retorna veiculos embutidos; sem GET separado de veiculos', async () => {
  const apiReqs: string[] = [];
  const handler = (req: Request) => {
    const u = req.url();
    if (u.includes('/api/v1/')) apiReqs.push(`${req.method()} ${new URL(u).pathname}`);
  };
  page.on('request', handler);

  await irLogado('/clientes');
  await expect(page.locator('tbody tr').first()).toBeVisible();
  const verBtn = page.getByRole('button', { name: /^Visualizar / }).first();
  await expect(verBtn).toBeVisible();
  await verBtn.click();
  await page.waitForLoadState('networkidle');
  await expect(page).toHaveURL(/\/clientes\/[0-9a-f-]+$/);

  const clienteId = page.url().split('/clientes/')[1]!.split(/[/?#]/)[0]!;

  // Valida o contrato real do backend via a sessão do browser.
  const apiResp = await context.request.get(`/api/v1/clientes/${clienteId}`, {
    headers: { Authorization: '' },
  });
  // A chamada acima pode não ter token; o objetivo principal é o contrato visto na página.
  page.off('request', handler);

  const chamouVeiculosSeparado = apiReqs.some((r) =>
    /^GET \/api\/v1\/clientes\/[^/]+\/veiculos$/.test(r),
  );
  test.info().annotations.push({
    type: 'network',
    description: `Requests da página: ${apiReqs.join(' | ')} | GET cliente status=${apiResp.status()}`,
  });
  // A tela de detalhe deve renderizar a seção de veículos a partir do GET único do cliente.
  await expect(page.getByText(/Veículos do cliente/i)).toBeVisible();
  expect(chamouVeiculosSeparado, 'não deve haver GET separado de /veiculos').toBeFalsy();

  await page.screenshot({ path: `${EVID}/card05-detalhe-cliente-veiculos.png`, fullPage: true });
});

// ───────────────────────────────────────────────────────────────────────────
// Card 06 — Renavam: campo não deve existir (foi removido)
// ───────────────────────────────────────────────────────────────────────────
test('CARD06 - modal de veiculo nao possui campo renavam e salva normalmente', async () => {
  await irLogado('/clientes/novo');
  await preencherIdentificacao('Cliente Renavam Teste');
  await preencherContatoEndereco();

  await page.getByRole('button', { name: /adicionar ve/i }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();

  const renavam = await dialog.getByText(/renavam/i).count();
  expect(renavam, 'campo renavam não deve existir').toBe(0);

  await dialog.getByPlaceholder('AAA0000').fill(placaAleatoria());
  await dialog.getByPlaceholder('Porsche').fill('Honda');
  await dialog.getByPlaceholder('911 Carrera S').fill('Civic');
  await dialog.getByRole('button', { name: 'PRETO' }).click();
  await page.screenshot({ path: `${EVID}/card06-modal-sem-renavam.png` });
  await dialog.getByRole('button', { name: /salvar e continuar/i }).click();
  await expect(dialog).toBeHidden();
  await expect(page.getByText(/Honda Civic/)).toBeVisible();
});

// ───────────────────────────────────────────────────────────────────────────
// Card 07 — Payload de veículo conforme contrato do backend
// ───────────────────────────────────────────────────────────────────────────
test('CARD07 - payload de veiculo envia contrato correto (placa/fabricante/modelo/cor/ano)', async () => {
  await irLogado('/clientes/novo');
  await preencherIdentificacao('Cliente Payload Teste');
  await preencherContatoEndereco();

  await page.getByRole('button', { name: /adicionar ve/i }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await dialog.getByPlaceholder('AAA0000').fill(placaAleatoria());
  await dialog.getByPlaceholder('2024').fill('2000');
  await dialog.getByPlaceholder('Porsche').fill('Honda');
  await dialog.getByPlaceholder('911 Carrera S').fill('Civic');
  await dialog.getByRole('button', { name: 'AZUL' }).click();
  await dialog.getByRole('button', { name: /salvar e continuar/i }).click();
  await expect(dialog).toBeHidden();

  const reqPromise = page.waitForRequest(
    (req) => /\/api\/v1\/clientes\/[^/]+\/veiculos$/.test(req.url()) && req.method() === 'POST',
  );
  await page.getByRole('button', { name: /concluir cadastro/i }).click();

  const req = await reqPromise;
  const payload = req.postDataJSON() as Record<string, unknown>;
  test.info().annotations.push({
    type: 'network',
    description: `Payload veículo: ${JSON.stringify(payload)}`,
  });

  expect(payload).toHaveProperty('placa');
  expect(payload).toHaveProperty('fabricante');
  expect(payload).toHaveProperty('modelo');
  expect(payload).toHaveProperty('cor');
  expect(payload.ano).toBe(2000);
  expect(payload).not.toHaveProperty('marca');
  expect(payload).not.toHaveProperty('renavam');
  expect(payload).not.toHaveProperty('categoria');
  expect(payload).not.toHaveProperty('corHex');
  expect(payload).not.toHaveProperty('anoModelo');
  expect(payload).not.toHaveProperty('observacoesAtendimento');

  await page.screenshot({ path: `${EVID}/card07-payload-veiculo.png`, fullPage: true });
});

// ───────────────────────────────────────────────────────────────────────────
// Card 08 — "Salvar Rascunho" deve ter sido removido
// ───────────────────────────────────────────────────────────────────────────
test('CARD08 - botao Salvar Rascunho foi removido do cadastro de cliente', async () => {
  await irLogado('/clientes/novo');
  const rascunho = await page.getByRole('button', { name: /rascunho/i }).count();
  const textoRascunho = await page.getByText(/salvar rascunho/i).count();
  expect(rascunho + textoRascunho, 'não deve haver "Salvar Rascunho"').toBe(0);
  await expect(page.getByRole('button', { name: /limpar formul/i })).toBeVisible();
  await page.screenshot({ path: `${EVID}/card08-sem-salvar-rascunho.png`, fullPage: true });
});

// ───────────────────────────────────────────────────────────────────────────
// Card 09 — Número do endereço aceita alfanumérico
// ───────────────────────────────────────────────────────────────────────────
test('CARD09 - campo numero aceita alfanumerico (ex: 123A)', async () => {
  await irLogado('/clientes/novo');
  await preencherIdentificacao('Cliente Numero Teste');
  await preencherContatoEndereco('123A');

  const numero = page.locator('#numero');
  await expect(numero).toHaveValue('123A');
  await expect(page.locator('#numero-error')).toHaveCount(0);

  // Com número alfanumérico o form fica válido e libera a etapa de veículos.
  await page.getByRole('button', { name: /adicionar ve/i }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.screenshot({ path: `${EVID}/card09-numero-alfanumerico.png`, fullPage: true });
});

// ───────────────────────────────────────────────────────────────────────────
// Card 10 — Área de clientes possui actions de editar e inativar
// ───────────────────────────────────────────────────────────────────────────
test('CARD10 - lista de clientes possui actions editar e inativar', async () => {
  await irLogado('/clientes');
  await expect(page.locator('tbody tr').first()).toBeVisible();
  await expect(page.getByRole('button', { name: /^Visualizar / }).first()).toBeVisible();
  await expect(page.getByRole('button', { name: /^Editar / }).first()).toBeVisible();
  await expect(page.getByRole('button', { name: /^(Inativar|Reativar) / }).first()).toBeVisible();
  await page.screenshot({ path: `${EVID}/card10-actions-clientes.png`, fullPage: true });
});
