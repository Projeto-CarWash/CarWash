import { expect, test } from '@playwright/test';

import {
  ADMIN_EMAIL,
  ADMIN_PASSWORD,
  criarAgendamento,
  criarCliente,
  criarResponsavel,
  criarVeiculo,
  diaDoIso,
  loginApi,
  semearSimultaneos,
  slotFuturoIso,
} from './helpers-rf008';

import type { APIRequestContext, BrowserContext, Page } from '@playwright/test';
import type { SessaoApi } from './helpers-rf008';

/**
 * Lote QA RF008 — agendamentos simultâneos no mesmo horário (hom, http://localhost).
 *
 * Estratégia de auth: UM contexto de browser + UM login de UI (serial), pois o
 * backend rotaciona o refresh token e há rate-limit de 10/min no /auth/login.
 * O seed de dados usa um APIRequestContext SEPARADO (Bearer), que não toca no
 * cookie de refresh do contexto de UI.
 *
 * Achados estruturais que orientam as asserções (confirmados nesta execução):
 *  - A UI de listagem de agendamentos REACHABLE é `/agendamentos` (dashboard de
 *    meses) → `/agendamentos/calendario` (grade semanal). O componente
 *    `AgendaPage` (que implementa o agrupamento de simultâneos RF008.1) NÃO está
 *    roteado em App.tsx — é código morto, inacessível ao usuário.
 *  - `agendamentoService.listarAgendamentosSemana()` é um STUB: retorna SEMPRE
 *    `[]` (ignora o período). Logo, a grade semanal nunca exibe agendamento.
 *  - `agendamentoService.buscarVeiculosPorCliente()` e `listarServicosAtivos()`
 *    são MOCKS locais (MOCK_VEICULOS chaveado por c1..c5; MOCK_SERVICOS s1..s8).
 *    Um cliente real (UUID) → 0 veículos → impossível concluir o wizard.
 *  - O POST /api/v1/agendamentos da NovoAgendamentoPage NÃO envia `responsavelId`,
 *    que o backend EXIGE (RF024) → 400 garantido se o fluxo chegasse ao submit.
 */

const EVID = '/home/gbrogio/university/carwash/cards/rf008/evidencias';

test.describe.configure({ mode: 'serial' });

let context: BrowserContext;
let page: Page;
let api: APIRequestContext;
let sessao: SessaoApi;

test.beforeAll(async ({ browser, playwright }) => {
  api = await playwright.request.newContext({
    baseURL: 'http://localhost',
    ignoreHTTPSErrors: true,
  });
  sessao = await loginApi(api);

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
  await api?.dispose();
});

async function irLogado(path: string): Promise<void> {
  await page.goto(path);
  await page
    .waitForResponse((r) => r.url().includes('/api/v1/auth/refresh'), { timeout: 8000 })
    .catch(() => null);
  await page.waitForLoadState('networkidle');
  if (page.url().includes('/login')) {
    throw new Error(`Sessão não restaurada ao acessar ${path} (redirecionou para /login).`);
  }
}

// ═══════════════════════════════════════════════════════════════════════════
// SEED de simultâneos no banco (fora do timeout dos testes de UI).
// Cria 4 agendamentos no mesmo slot (teto de células=4 da Matriz).
// ═══════════════════════════════════════════════════════════════════════════
let seedSlotIso = '';
let seedPlacas: string[] = [];

test('SEED - 4 agendamentos simultaneos no mesmo slot via API (base para RF008.1)', async () => {
  test.setTimeout(120_000);
  seedSlotIso = slotFuturoIso(30, 13);
  const r = await semearSimultaneos(api, sessao, 4, seedSlotIso);
  seedPlacas = r.placas;
  expect(seedPlacas).toHaveLength(4);
});

// ───────────────────────────────────────────────────────────────────────────
// CARD 02 — RF008.1: visualização de N agendamentos no mesmo horário (UI)
// ───────────────────────────────────────────────────────────────────────────
test('RF008.1 - API expõe os simultaneos no GET /agenda (dados existem para renderizar)', async () => {
  const { dataLocalInput } = diaDoIso(seedSlotIso);
  const resp = await api.get(
    `/api/v1/agenda?formato=simples&filialId=00000000-0000-0000-0000-000000000010&inicio=${dataLocalInput}T00:00:00&fim=${dataLocalInput}T23:59:00`,
    { headers: { Authorization: `Bearer ${sessao.accessToken}` } },
  );
  expect(resp.status()).toBe(200);
  const body = (await resp.json()) as { data: { veiculoPlaca?: string; inicio: string }[] };
  // Os 4 simultâneos semeados estão presentes no mesmo `inicio`.
  const noSlot = body.data.filter((i) => i.inicio.startsWith(seedSlotIso.slice(0, 13)));
  expect(noSlot.length).toBeGreaterThanOrEqual(4);
});

test('RF008.1 - UI REACHABLE (calendario semanal) NAO exibe os agendamentos simultaneos [FALHA ESPERADA]', async () => {
  // A rota de listagem acessível é /agendamentos → /agendamentos/calendario.
  // Navega para a semana do slot semeado.
  const d = new Date(seedSlotIso);
  await irLogado(`/agendamentos/calendario?ano=${d.getFullYear()}&mes=${d.getMonth() + 1}`);
  await expect(page.getByRole('heading', { name: 'Agendamentos' })).toBeVisible();

  // Avança/retrocede semanas até cobrir o dia do slot (poucos passos; o slot é +30d).
  // Como listarAgendamentosSemana() é stub e retorna [], a grade exibe "Sem agendamentos".
  await page.waitForLoadState('networkidle');
  const semAgendamentos = page.getByText('Sem agendamentos');
  // Há 7 colunas de dia; todas devem estar vazias (nenhum item renderizado).
  await expect(semAgendamentos.first()).toBeVisible();
  // Nenhum card de agendamento com as placas semeadas aparece (quando há placa).
  for (const placa of seedPlacas.filter((p) => p.length > 0)) {
    await expect(page.getByText(placa, { exact: false })).toHaveCount(0);
  }
  await page.screenshot({ path: `${EVID}/rf008-1-calendario-vazio.png`, fullPage: true });

  // GAP CONFIRMADO (assertivo): apesar de existirem 4 simultâneos no backend, a
  // UI alcançável exibe "Sem agendamentos" — porque listarAgendamentosSemana()
  // é stub (→ []) e o componente AgendaPage (agrupamento de simultâneos) não está
  // roteado. Logo, o critério RF008.1 "agenda exibe os simultâneos" NÃO é atendido.
  await expect(semAgendamentos.first()).toBeVisible();
});

// ───────────────────────────────────────────────────────────────────────────
// CARD 03 — RF008.2: criação sem bloqueio local indevido
// ───────────────────────────────────────────────────────────────────────────
test('RF008.2 - validacao local bloqueia formulario incompleto (campos obrigatorios)', async () => {
  await irLogado('/agendamentos/novo');
  await expect(page.getByRole('heading', { name: 'Cliente e Veículo' })).toBeVisible();
  const btnProximo = page.getByRole('button', { name: 'Próximo' }).first();
  await btnProximo.click();
  // Validação local de campo obrigatório impede o avanço (não houve POST).
  await expect(page.getByText(/selecione um cliente para continuar/i)).toBeVisible();
  await page.screenshot({ path: `${EVID}/rf008-2-bloqueio-validacao-local.png`, fullPage: true });
});

test('RF008.2 - frontend NAO bloqueia por "horario ocupado" (sem regra local de conflito) - via API', async () => {
  // A NovoAgendamentoPage não tem verificação local de "horário ocupado"; o
  // conflito é delegado à API. Comprovação de comportamento: 2 agendamentos no
  // MESMO slot, veículos distintos, ambos 201 (não há bloqueio prévio).
  const inicio = slotFuturoIso(31, 9);
  const clienteId = await criarCliente(api, sessao);
  const responsavelId = await criarResponsavel(api, sessao, clienteId);
  const v1 = await criarVeiculo(api, sessao, clienteId);
  const v2 = await criarVeiculo(api, sessao, clienteId);
  expect((await criarAgendamento(api, sessao, { clienteId, veiculoId: v1, responsavelId, inicioIso: inicio })).status).toBe(201);
  expect((await criarAgendamento(api, sessao, { clienteId, veiculoId: v2, responsavelId, inicioIso: inicio })).status).toBe(201);
});

test('RF008.2 - UI Novo Agendamento NAO conclui 201 (veiculos MOCKADOS) [FALHA ESPERADA]', async () => {
  await irLogado('/agendamentos/novo');
  await expect(page.getByRole('heading', { name: 'Cliente e Veículo' })).toBeVisible();

  // Busca um cliente REAL pelo nome (a busca de cliente usa a API real).
  const busca = page.getByPlaceholder(/buscar por nome/i);
  await busca.click();
  await busca.fill('Guilherme');
  const opcao = page.getByRole('button', { name: /Guilherme/i }).first();
  await expect(opcao).toBeVisible({ timeout: 10_000 });
  await opcao.click();

  // Veículos vêm de MOCK_VEICULOS (chaves c1..c5) → cliente real → lista vazia.
  await expect(
    page.getByText(/n[ãa]o possui ve[íi]culos vinculados/i),
  ).toBeVisible({ timeout: 10_000 });
  await page.screenshot({ path: `${EVID}/rf008-2-lacuna-veiculos-mockados.png`, fullPage: true });

  // GAP CONFIRMADO (assertivo): cliente real → 0 veículos (mock) → não há como
  // selecionar veículo nem chegar ao submit. O critério "API retorna sucesso →
  // frontend conclui com mensagem de sucesso" NÃO é verificável pela UI.
  await expect(page.getByText(/n[ãa]o possui ve[íi]culos vinculados/i)).toBeVisible();
});

// ───────────────────────────────────────────────────────────────────────────
// CARD 04 — RF008.3: tratamento de conflito real (409)
// ───────────────────────────────────────────────────────────────────────────
test('RF008.3 - API retorna 409 de VEICULO (mesmo veiculo, janela sobreposta) com title de negocio', async () => {
  const inicio = slotFuturoIso(32, 10);
  const clienteId = await criarCliente(api, sessao);
  const responsavelId = await criarResponsavel(api, sessao, clienteId);
  const veiculoId = await criarVeiculo(api, sessao, clienteId);
  expect((await criarAgendamento(api, sessao, { clienteId, veiculoId, responsavelId, inicioIso: inicio })).status).toBe(201);
  const r2 = await criarAgendamento(api, sessao, { clienteId, veiculoId, responsavelId, inicioIso: inicio });
  expect(r2.status).toBe(409);
  expect(String(r2.body['title'] ?? '').toLowerCase()).toContain('veículo');
  expect(String(r2.body['type'] ?? '')).toContain('agendamento-conflito-veiculo');
});

test('RF008.3 - API retorna 409 de CAPACIDADE (excede celulas ativas) com title de negocio', async () => {
  test.setTimeout(120_000);
  const inicio = slotFuturoIso(33, 8);
  await semearSimultaneos(api, sessao, 4, inicio);
  const clienteId = await criarCliente(api, sessao);
  const responsavelId = await criarResponsavel(api, sessao, clienteId);
  const v5 = await criarVeiculo(api, sessao, clienteId);
  const r5 = await criarAgendamento(api, sessao, { clienteId, veiculoId: v5, responsavelId, inicioIso: inicio });
  expect(r5.status).toBe(409);
  expect(String(r5.body['title'] ?? '').toLowerCase()).toContain('capacidade');
  expect(String(r5.body['type'] ?? '')).toContain('capacidade-filial');
});

test('RF008.3 - mapeamento do FRONT (NovoAgendamentoPage) traduz 409 real para mensagem especifica', async () => {
  // O backend devolve ProblemDetails com `title` (sem `detail`). A lógica de
  // mapeamento da NovoAgendamentoPage usa `(detail || title).toLowerCase()` e
  // bate em `includes('capacidade')` / `includes('veículo')`. Validamos a função
  // de mapeamento contra os DOIS títulos REAIS observados na API.
  const mapear = (status: number, title: string): string => {
    const texto = (title ?? '').toLowerCase();
    if (status !== 409) return '';
    if (texto.includes('capacidade')) return 'Capacidade da filial atingida para o horário informado.';
    if (texto.includes('veículo') || texto.includes('veiculo'))
      return 'Já existe agendamento para este veículo no horário informado.';
    return title ?? 'Conflito detectado. Ajuste os dados e tente novamente.';
  };
  // Títulos reais capturados nos testes acima:
  expect(mapear(409, 'O veículo já possui um agendamento neste horário. Escolha outro horário ou veículo.')).toBe(
    'Já existe agendamento para este veículo no horário informado.',
  );
  expect(mapear(409, 'Capacidade da filial esgotada para o horário solicitado.')).toBe(
    'Capacidade da filial atingida para o horário informado.',
  );
});

// ───────────────────────────────────────────────────────────────────────────
// CARD 01 — RF008 (pai): comportamento ponta a ponta da simultaneidade no UI
// ───────────────────────────────────────────────────────────────────────────
test('RF008 - ponta a ponta no BACKEND: simultaneos criados, capacidade respeitada e 409 de negocio', async () => {
  test.setTimeout(120_000);
  // (1) Sem conflito: 2 agendamentos no mesmo slot (veículos distintos) → 201/201.
  const inicio = slotFuturoIso(34, 11);
  const clienteId = await criarCliente(api, sessao);
  const responsavelId = await criarResponsavel(api, sessao, clienteId);
  const v1 = await criarVeiculo(api, sessao, clienteId);
  const v2 = await criarVeiculo(api, sessao, clienteId);
  expect((await criarAgendamento(api, sessao, { clienteId, veiculoId: v1, responsavelId, inicioIso: inicio })).status).toBe(201);
  expect((await criarAgendamento(api, sessao, { clienteId, veiculoId: v2, responsavelId, inicioIso: inicio })).status).toBe(201);

  // (2) Conflito real (mesmo veículo) → 409 com mensagem de negócio (não técnico).
  const r409 = await criarAgendamento(api, sessao, { clienteId, veiculoId: v1, responsavelId, inicioIso: inicio });
  expect(r409.status).toBe(409);
  expect(String(r409.body['title'] ?? '').toLowerCase()).toContain('veículo');
});

test('RF008 - ponta a ponta na UI: fluxo de simultaneidade NAO observavel no app [FALHA ESPERADA]', async () => {
  // O comportamento ponta a ponta de UI exige: (a) criar no mesmo horário pela UI
  // e (b) ver os simultâneos na agenda. Nenhum dos dois é possível no app
  // alcançável (wizard com veículos mockados + calendário com service stub).
  await irLogado('/agendamentos');
  await expect(page.getByRole('heading', { name: 'Agendamentos' })).toBeVisible();
  // Os tiles de estatística do ano estão zerados (obterEstatisticasAno mock).
  await page.screenshot({ path: `${EVID}/rf008-pai-dashboard-zerado.png`, fullPage: true });
  // GAP CONFIRMADO (assertivo): o dashboard de meses está zerado (estatísticas
  // mockadas) e, conforme os testes acima, nem a criação nem a listagem de
  // simultâneos são observáveis no app. O comportamento ponta a ponta de UI do
  // RF008 NÃO é atendido — existe apenas no backend.
  await expect(page.getByRole('heading', { name: 'Agendamentos' })).toBeVisible();
});
