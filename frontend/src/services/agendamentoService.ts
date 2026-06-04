import api from './api';

import type {
  AgendamentoResponse,
  AgendamentoSemana,
  ClienteResumido,
  ConfirmarAgendamentoRequest,
  CriarAgendamentoPayload,
  CriarAgendamentoRequest,
  CriarAgendamentoResponse,
  EstatisticasMes,
  PreConfirmacaoResponse,
  ServicoAtivo,
  VeiculoResumido,
  CancelarAgendamentoResponse,
} from '@/types/agendamento';

const MOCK_VEICULOS: Record<string, VeiculoResumido[]> = {
  c1: [
    { id: 'v1', placa: 'ABC-1D23', modelo: 'VW Golf GTI', cor: 'Preto', ano: 2023 },
    { id: 'v2', placa: 'XYZ-9H87', modelo: 'Hyundai HB20', cor: 'Prata', ano: 2021 },
  ],
  c2: [{ id: 'v3', placa: 'DEF-4E56', modelo: 'Honda Civic', cor: 'Branco', ano: 2024 }],
  c3: [
    { id: 'v4', placa: 'GHI-7F89', modelo: 'Toyota Hilux', cor: 'Prata', ano: 2022 },
    { id: 'v5', placa: 'JKL-2G34', modelo: 'Fiat Toro', cor: 'Vermelho', ano: 2023 },
    { id: 'v6', placa: 'MNO-5A67', modelo: 'Chevrolet S10', cor: 'Preto', ano: 2021 },
  ],
  c4: [],
  c5: [{ id: 'v7', placa: 'PQR-8B12', modelo: 'BMW 320i', cor: 'Azul', ano: 2024 }],
};

const MOCK_SERVICOS: ServicoAtivo[] = [
  {
    id: 's1',
    nome: 'Lavagem Simples',
    preco: 45.0,
    duracao: 30,
    descricao: 'Lavagem externa com agua e shampoo automotivo.',
  },
  {
    id: 's2',
    nome: 'Lavagem Completa',
    preco: 89.9,
    duracao: 60,
    descricao: 'Lavagem externa + aspiracao interna + painel.',
  },
  {
    id: 's3',
    nome: 'Polimento',
    preco: 180.0,
    duracao: 120,
    descricao: 'Polimento com massa de corte e finalizacao.',
  },
  {
    id: 's4',
    nome: 'Cristalizacao',
    preco: 250.0,
    duracao: 90,
    descricao: 'Cristalizacao de pintura com protecao UV.',
  },
  {
    id: 's5',
    nome: 'Higienizacao Interna',
    preco: 120.0,
    duracao: 45,
    descricao: 'Limpeza profunda de estofados e carpetes.',
  },
  {
    id: 's6',
    nome: 'Enceramento',
    preco: 70.0,
    duracao: 40,
    descricao: 'Aplicacao de cera protetora com brilho intenso.',
  },
  {
    id: 's7',
    nome: 'Lavagem de Motor',
    preco: 95.0,
    duracao: 35,
    descricao: 'Desengraxe e lavagem do compartimento do motor.',
  },
  {
    id: 's8',
    nome: 'Vitrificacao',
    preco: 350.0,
    duracao: 180,
    descricao: 'Protecao ceramica de longa duracao na pintura.',
  },
];

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

const MOCK_AGENDAMENTO_RESPONSE: AgendamentoResponse = {
  id: 'mock-agendamento-id',
  filialId: 'mock-filial-id',
  clienteId: 'mock-cliente-id',
  veiculoId: 'mock-veiculo-id',
  responsavelId: null,
  status: 'agendado',
  inicio: new Date().toISOString(),
  fim: new Date(Date.now() + 3600000).toISOString(),
  duracaoTotalMin: 60,
  valorTotal: 100,
  observacoes: null,
  versao: 1,
  itens: [],
  criadoEm: new Date().toISOString(),
  mensagem: 'Agendamento criado com sucesso.',
  traceId: 'mock-trace-id',
};

const MOCK_PRE_CONFIRMACAO: PreConfirmacaoResponse = {
  tokenConfirmacao: 'mock-token',
  expiraEm: new Date(Date.now() + 600000).toISOString(),
  resumo: {
    filial: { id: 'mock-filial-id', nome: 'Filial Centro' },
    cliente: { id: 'mock-cliente-id', nome: 'Cliente Mock', documento: '000.000.000-00' },
    veiculo: { id: 'mock-veiculo-id', placa: 'ABC1D23', modelo: 'Fiat Uno', cor: 'Prata' },
    servicos: [{ id: 's1', nome: 'Lavagem Simples', duracaoMin: 30, preco: 45 }],
    inicio: new Date().toISOString(),
    fim: new Date(Date.now() + 1800000).toISOString(),
    duracaoTotalMin: 30,
    valorTotal: 45,
    observacoes: null,
    hashResumo: 'mock-hash',
  },
  traceId: 'mock-trace-pre',
};

export const agendamentoService = {
  async buscarClientes(busca: string): Promise<ClienteResumido[]> {
    const { data } = await api.get<{ itens: ClienteResumido[] }>('/api/v1/clientes', {
      params: {
        ...(busca.trim() ? { busca: busca.trim() } : {}),
        pagina: 1,
        tamanhoPagina: 50,
      },
    });

    return data.itens;
  },

  async buscarVeiculosPorCliente(clienteId: string): Promise<VeiculoResumido[]> {
    await delay(400);
    return MOCK_VEICULOS[clienteId] ?? [];
  },

  async listarServicosAtivos(): Promise<ServicoAtivo[]> {
    await delay(350);
    return [...MOCK_SERVICOS];
  },

  async criarAgendamento(payload: CriarAgendamentoPayload): Promise<CriarAgendamentoResponse> {
    await delay(800);
    void api;
    void payload;
    return { id: crypto.randomUUID() };
  },

  async criar(_payload: CriarAgendamentoRequest): Promise<AgendamentoResponse> {
    await delay(800);
    return { ...MOCK_AGENDAMENTO_RESPONSE, id: crypto.randomUUID() };
  },

  async preConfirmar(_payload: CriarAgendamentoRequest): Promise<PreConfirmacaoResponse> {
    await delay(600);
    return { ...MOCK_PRE_CONFIRMACAO, tokenConfirmacao: crypto.randomUUID() };
  },

  async confirmar(_payload: ConfirmarAgendamentoRequest): Promise<AgendamentoResponse> {
    await delay(800);
    return { ...MOCK_AGENDAMENTO_RESPONSE, id: crypto.randomUUID() };
  },

  async obterEstatisticasAno(_ano: number): Promise<EstatisticasMes[]> {
    await delay(500);

    const nomesMeses = [
      'JANEIRO',
      'FEVEREIRO',
      'MARCO',
      'ABRIL',
      'MAIO',
      'JUNHO',
      'JULHO',
      'AGOSTO',
      'SETEMBRO',
      'OUTUBRO',
      'NOVEMBRO',
      'DEZEMBRO',
    ];

    return nomesMeses.map((nome, index) => ({
      mes: index + 1,
      nome,
      confirmados: 0,
      pendentes: 0,
      cancelados: 0,
      total: 0,
    }));
  },

  async listarAgendamentosSemana(_dataInicio: Date, _dataFim: Date): Promise<AgendamentoSemana[]> {
    await delay(600);
    return [];
  },

  async cancelar(id: string, motivoCancelamento: string): Promise<CancelarAgendamentoResponse> {
    const { data } = await api.patch<CancelarAgendamentoResponse>(
      `/api/v1/agendamentos/${id}/cancelar`,
      {
        motivoCancelamento,
        origem: 'CLIENTE',
      },
    );
    return data;
  },

  async atualizar(
    id: string,
    payload: { observacoes: string | null },
  ): Promise<AgendamentoResponse> {
    const { data } = await api.put<AgendamentoResponse>(`/api/v1/agendamentos/${id}`, payload);
    return data;
  },
};
