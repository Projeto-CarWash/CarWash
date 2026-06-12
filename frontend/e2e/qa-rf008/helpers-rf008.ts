import type { APIRequestContext } from '@playwright/test';

/**
 * Helpers de seed/contrato para o lote QA RF008 (agendamentos simultâneos),
 * contra a stack HOM já no ar (http://localhost). Todas as chamadas passam pelo
 * proxy em /api/v1, o mesmo caminho do SPA.
 *
 * Particularidades CONFIRMADAS no ambiente de hom (2026-06-07):
 *  - O POST /api/v1/agendamentos EXIGE `responsavelId` (RF024 — validator
 *    `CriarAgendamentoCommandValidator`). Sem ele: 400 "Selecione um responsável
 *    para prosseguir.". O responsável é uma entidade nested em cliente:
 *    POST /api/v1/clientes/{clienteId}/responsaveis.
 *  - A filial Matriz (id ...010) tem `celulasAtivas = 4` ⇒ 4 agendamentos
 *    simultâneos no mesmo slot são aceitos; o 5º recebe 409 (capacidade).
 *  - 409 de veículo: mesmo veículo em janela sobreposta. title = "O veículo já
 *    possui um agendamento neste horário...". slug = agendamento-conflito-veiculo.
 *  - 409 de capacidade: title = "Capacidade da filial esgotada para o horário
 *    solicitado.". slug = capacidade-filial-esgotada.
 *  - O ProblemDetails NÃO traz campo `detail`; o título vai em `title`.
 */

export const FILIAL_MATRIZ_ID = '00000000-0000-0000-0000-000000000010';
export const SERVICO_SIMPLES_ID = '00000000-0000-0000-0000-000000000100';

export const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@carwash.local';
export const ADMIN_PASSWORD =
  process.env.CARWASH_SEED_ADMIN_PASSWORD ?? 'TrocarEmCadaAmbiente!2026';

export function placaAleatoria(): string {
  const L = () => 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'[Math.floor(Math.random() * 26)];
  const N = () => Math.floor(Math.random() * 10);
  return `${L()}${L()}${L()}${N()}${L()}${N()}${N()}`;
}

export function cpfAleatorio(): string {
  const n = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  const dv = (base: number[]): number => {
    const soma = base.reduce((acc, d, i) => acc + d * (base.length + 1 - i), 0);
    const resto = soma % 11;
    return resto < 2 ? 0 : 11 - resto;
  };
  const d1 = dv(n);
  const d2 = dv([...n, d1]);
  return [...n, d1, d2].join('');
}

export interface SessaoApi {
  accessToken: string;
}

export async function loginApi(request: APIRequestContext): Promise<SessaoApi> {
  const resp = await request.post('/api/v1/auth/login', {
    data: { email: ADMIN_EMAIL, senha: ADMIN_PASSWORD },
  });
  if (!resp.ok()) {
    throw new Error(`Login API falhou (${resp.status()}): ${await resp.text()}`);
  }
  const body = (await resp.json()) as { accessToken?: string; token?: string };
  const accessToken = body.accessToken ?? body.token;
  if (!accessToken) {
    throw new Error(`Resposta de login sem accessToken: ${JSON.stringify(body)}`);
  }
  return { accessToken };
}

function headers(s: SessaoApi): Record<string, string> {
  return { Authorization: `Bearer ${s.accessToken}` };
}

async function jsonId(
  resp: Awaited<ReturnType<APIRequestContext['post']>>,
  recurso: string,
): Promise<string> {
  if (!resp.ok()) {
    throw new Error(`Falha ao criar ${recurso} (${resp.status()}): ${await resp.text()}`);
  }
  const body = (await resp.json()) as { id?: string };
  if (!body.id) throw new Error(`Resposta de ${recurso} sem id: ${JSON.stringify(body)}`);
  return body.id;
}

/** Pega o 1º cliente já existente no seed/banco de hom. */
export async function primeiroClienteId(
  request: APIRequestContext,
  s: SessaoApi,
): Promise<string> {
  const resp = await request.get('/api/v1/clientes?tamanhoPagina=1', { headers: headers(s) });
  if (!resp.ok()) throw new Error(`GET clientes falhou (${resp.status()})`);
  const body = (await resp.json()) as { itens?: { id: string }[] };
  const id = body.itens?.[0]?.id;
  if (!id) throw new Error('Nenhum cliente disponível no banco de hom.');
  return id;
}

/** Cria um cliente novo (isolamento entre runs) e devolve seu id. */
export async function criarCliente(request: APIRequestContext, s: SessaoApi): Promise<string> {
  const resp = await request.post('/api/v1/clientes', {
    headers: headers(s),
    data: {
      nome: 'Cliente QA Simultaneo',
      cpf: cpfAleatorio(),
      dataNascimento: '1990-01-01',
      celular: '11987654321',
      endereco: {
        cep: '01310100',
        logradouro: 'Av. Paulista',
        numero: '1000',
        bairro: 'Bela Vista',
        cidade: 'Sao Paulo',
        uf: 'SP',
      },
    },
  });
  return jsonId(resp, 'cliente');
}

export async function criarVeiculo(
  request: APIRequestContext,
  s: SessaoApi,
  clienteId: string,
): Promise<string> {
  const resp = await request.post(`/api/v1/clientes/${clienteId}/veiculos`, {
    headers: headers(s),
    data: { placa: placaAleatoria(), modelo: 'Civic', fabricante: 'Honda', cor: 'Preto' },
  });
  return jsonId(resp, 'veiculo');
}

/** Cria um responsável autorizado (RF024) sob o cliente e devolve seu id. */
export async function criarResponsavel(
  request: APIRequestContext,
  s: SessaoApi,
  clienteId: string,
): Promise<string> {
  const resp = await request.post(`/api/v1/clientes/${clienteId}/responsaveis`, {
    headers: headers(s),
    data: {
      nome: 'Responsavel QA Simultaneo',
      documento: cpfAleatorio(),
      grauVinculo: 'OUTRO',
    },
  });
  return jsonId(resp, 'responsavel');
}

export interface CriarAgInput {
  clienteId: string;
  veiculoId: string;
  responsavelId: string;
  inicioIso: string;
  filialId?: string;
  servicoId?: string;
}

/** Cria um agendamento via API e devolve a resposta crua (status + body). */
export async function criarAgendamento(
  request: APIRequestContext,
  s: SessaoApi,
  input: CriarAgInput,
): Promise<{ status: number; body: Record<string, unknown> }> {
  const resp = await request.post('/api/v1/agendamentos', {
    headers: headers(s),
    data: {
      filialId: input.filialId ?? FILIAL_MATRIZ_ID,
      clienteId: input.clienteId,
      veiculoId: input.veiculoId,
      responsavelId: input.responsavelId,
      inicio: input.inicioIso,
      servicoIds: [input.servicoId ?? SERVICO_SIMPLES_ID],
    },
  });
  let body: Record<string, unknown> = {};
  try {
    body = (await resp.json()) as Record<string, unknown>;
  } catch {
    body = {};
  }
  return { status: resp.status(), body };
}

/**
 * Semeia N agendamentos simultâneos no MESMO slot (mesma filial, mesmo início),
 * cada um com um veículo distinto (evita 409 de veículo). Respeita o teto de
 * células ativas (4) da Matriz. Devolve a janela ISO usada.
 */
export async function semearSimultaneos(
  request: APIRequestContext,
  s: SessaoApi,
  quantidade: number,
  inicioIso: string,
): Promise<{ inicioIso: string; placas: string[] }> {
  const clienteId = await criarCliente(request, s);
  const responsavelId = await criarResponsavel(request, s, clienteId);
  const placas: string[] = [];
  for (let i = 0; i < quantidade; i++) {
    const veiculoId = await criarVeiculo(request, s, clienteId);
    const r = await criarAgendamento(request, s, {
      clienteId,
      veiculoId,
      responsavelId,
      inicioIso,
    });
    if (r.status !== 201) {
      throw new Error(`Seed simultâneo #${i + 1} falhou (${r.status}): ${JSON.stringify(r.body)}`);
    }
    placas.push((r.body['veiculoPlaca'] as string) ?? '');
  }
  return { inicioIso, placas };
}

/**
 * Janela de início ISO (Z) a `dias` dias no futuro. Para evitar colisão de
 * capacidade entre RE-EXECUÇÕES (slots determinísticos acumulariam itens no
 * banco de hom até esgotar as 4 células), o dia é deslocado por um valor
 * aleatório e a hora é sorteada num intervalo amplo — cada run usa um slot
 * virgem. A hora cheia mantém a janela alinhada ao minuto.
 */
export function slotFuturoIso(dias: number): string {
  const d = new Date();
  // Deslocamento aleatório de dias (60..420) + o offset do chamador, garantindo
  // janelas distintas entre execuções e entre os diferentes testes do lote.
  const offsetDias = dias + 60 + Math.floor(Math.random() * 360);
  d.setUTCDate(d.getUTCDate() + offsetDias);
  const hora = 8 + Math.floor(Math.random() * 9); // 08..16h
  d.setUTCHours(hora, 0, 0, 0);
  return d.toISOString().replace(/\.\d{3}Z$/, '.000Z');
}

/** Formata um ISO Z em `dd/MM/yyyy` para preencher o filtro de período da agenda. */
export function diaDoIso(iso: string): { dataLocalInput: string } {
  // datetime-local usa horário LOCAL do browser; para o filtro basta cobrir o dia.
  const d = new Date(iso);
  const yyyy = d.getFullYear();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return { dataLocalInput: `${yyyy}-${mm}-${dd}` };
}
