import api from './api';

import type {
  ClienteResumido,
  CriarAgendamentoPayload,
  CriarAgendamentoResponse,
  ServicoAtivo,
  VeiculoResumido,
  EstatisticasMes,
  AgendamentoSemana,
} from '@/types/agendamento';

// ---------------------------------------------------------------------------
// Mock data — será removido quando os endpoints estiverem disponíveis
// ---------------------------------------------------------------------------

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
    descricao: 'Lavagem externa com água e shampoo automotivo.',
  },
  {
    id: 's2',
    nome: 'Lavagem Completa',
    preco: 89.9,
    duracao: 60,
    descricao: 'Lavagem externa + aspiração interna + painel.',
  },
  {
    id: 's3',
    nome: 'Polimento',
    preco: 180.0,
    duracao: 120,
    descricao: 'Polimento com massa de corte e finalização.',
  },
  {
    id: 's4',
    nome: 'Cristalização',
    preco: 250.0,
    duracao: 90,
    descricao: 'Cristalização de pintura com proteção UV.',
  },
  {
    id: 's5',
    nome: 'Higienização Interna',
    preco: 120.0,
    duracao: 45,
    descricao: 'Limpeza profunda de estofados e carpetes.',
  },
  {
    id: 's6',
    nome: 'Enceramento',
    preco: 70.0,
    duracao: 40,
    descricao: 'Aplicação de cera protetora com brilho intenso.',
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
    nome: 'Vitrificação',
    preco: 350.0,
    duracao: 180,
    descricao: 'Proteção cerâmica de longa duração na pintura.',
  },
];

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

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
    // Quando a API estiver disponível, descomentar:
    // const { data } = await api.post<CriarAgendamentoResponse>('/api/v1/agendamentos', payload);
    // return data;

    // Mock: simula delay + sucesso
    await delay(800);
    void api; // referência para manter o import (usado na versão real)
    void payload;
    return { id: crypto.randomUUID() };
  },

  async obterEstatisticasAno(_ano: number): Promise<EstatisticasMes[]> {
    await delay(500);

    const nomesMeses = [
      'JANEIRO',
      'FEVEREIRO',
      'MARÇO',
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

    // Mock gerando dados aleatórios, mas fixos para abril (4) para bater com o layout
    return nomesMeses.map((nome, index) => {
      const mes = index + 1;
      return {
        mes,
        nome,
        confirmados: 0,
        pendentes: 0,
        cancelados: 0,
        total: 0,
      };
    });
  },

  async listarAgendamentosSemana(_dataInicio: Date, _dataFim: Date): Promise<AgendamentoSemana[]> {
    await delay(600);

    return [];
  },
};
