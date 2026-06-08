import type { APIRequestContext } from '@playwright/test';

/**
 * Helpers de API para os E2E. Todas as chamadas passam pelo PROXY (baseURL do
 * Playwright) em `/api/v1/...` — o mesmo caminho que o SPA usa em runtime. Assim
 * o E2E valida o pipeline real: nginx -> backend -> PostgreSQL semeado.
 *
 * IDs fixos do seed técnico (migration InitialSchema / DB001 §05):
 *  - Filial "Matriz": 00000000-0000-0000-0000-000000000010
 *  - Serviços: ...100 (Lavagem Simples), ...101 (Lavagem Completa), ...102 (Enceramento)
 *
 * Nota: não há endpoint para criar filiais no MVP — só a Matriz semeada existe
 * em runtime. O cenário cross-filial do CA006 é coberto nos IntegrationTests
 * (acesso direto ao banco); aqui o E2E cobre o conflito na MESMA filial.
 */

export const FILIAL_MATRIZ_ID = '00000000-0000-0000-0000-000000000010';
export const SERVICO_SIMPLES_ID = '00000000-0000-0000-0000-000000000100';

export const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL ?? 'admin@carwash.local';
export const ADMIN_PASSWORD =
  process.env.CARWASH_SEED_ADMIN_PASSWORD ?? 'TrocarEmCadaAmbiente!2026';

export interface SessaoApi {
  accessToken: string;
}

/** Faz login via API e devolve o accessToken do admin semeado (RF001). */
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

function authHeaders(sessao: SessaoApi): Record<string, string> {
  return { Authorization: `Bearer ${sessao.accessToken}` };
}

/** Gera uma placa Mercosul aleatória válida (LLLNLNN) para evitar colisão entre runs. */
export function placaAleatoria(): string {
  const L = () => 'ABCDEFGHIJKLMNOPQRSTUVWXYZ'[Math.floor(Math.random() * 26)];
  const N = () => Math.floor(Math.random() * 10);
  return `${L()}${L()}${L()}${N()}${L()}${N()}${N()}`;
}

/** Gera um CPF válido (com dígitos verificadores) aleatório. */
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

export interface DependenciasAgendamento {
  filialId: string;
  clienteId: string;
  veiculoId: string;
  servicoId: string;
}

/**
 * Semeia o mínimo para um agendamento: um cliente novo com um veículo novo,
 * reaproveitando a filial Matriz e o serviço "Lavagem Simples" do seed.
 * Dados sempre aleatórios (placa/CPF) para isolamento entre execuções.
 */
export async function semearDependencias(
  request: APIRequestContext,
  sessao: SessaoApi,
): Promise<DependenciasAgendamento> {
  const headers = authHeaders(sessao);

  const clienteResp = await request.post('/api/v1/clientes', {
    headers,
    data: {
      nome: 'Cliente E2E',
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
  const clienteId = await idDe(clienteResp, 'cliente');

  // Veículo é criado sob o cliente: /api/v1/clientes/{clienteId}/veiculos.
  const veiculoResp = await request.post(`/api/v1/clientes/${clienteId}/veiculos`, {
    headers,
    data: {
      placa: placaAleatoria(),
      modelo: 'Civic',
      fabricante: 'Honda',
      cor: 'Preto',
    },
  });
  const veiculoId = await idDe(veiculoResp, 'veiculo');

  return {
    filialId: FILIAL_MATRIZ_ID,
    clienteId,
    veiculoId,
    servicoId: SERVICO_SIMPLES_ID,
  };
}

async function idDe(
  resp: Awaited<ReturnType<APIRequestContext['post']>>,
  recurso: string,
): Promise<string> {
  if (!resp.ok()) {
    throw new Error(`Falha ao criar ${recurso} (${resp.status()}): ${await resp.text()}`);
  }
  const body = (await resp.json()) as { id?: string };
  if (!body.id) {
    throw new Error(`Resposta de ${recurso} sem id: ${JSON.stringify(body)}`);
  }
  return body.id;
}

export interface CriarAgendamentoInput {
  filialId: string;
  clienteId: string;
  veiculoId: string;
  servicoId: string;
  inicioIso: string;
}

export async function criarAgendamento(
  request: APIRequestContext,
  sessao: SessaoApi,
  input: CriarAgendamentoInput,
) {
  return request.post('/api/v1/agendamentos', {
    headers: authHeaders(sessao),
    data: {
      filialId: input.filialId,
      clienteId: input.clienteId,
      veiculoId: input.veiculoId,
      inicio: input.inicioIso,
      servicoIds: [input.servicoId],
    },
  });
}
