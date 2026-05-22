import { HttpResponse, http } from 'msw';

import type {
  AgendaItemDetalhado,
  AgendaItemSimples,
  ConsultarAgendaResponse,
} from '@/types/agenda';
import type { AgendamentoResponse } from '@/types/agendamento';

/**
 * Fixtures e handlers MSW para os testes da feature de agendamento (RF007).
 *
 * <p>UUIDs fixos para que os testes possam selecionar opções de forma
 * determinística.</p>
 */
export const IDS = {
  cliente: '11111111-1111-4111-8111-111111111111',
  veiculo: '22222222-2222-4222-8222-222222222222',
  filial: '33333333-3333-4333-8333-333333333333',
  servicoA: '44444444-4444-4444-8444-444444444444',
  servicoB: '55555555-5555-4555-8555-555555555555',
} as const;

const respostaCriacao: AgendamentoResponse = {
  id: '99999999-9999-4999-8999-999999999999',
  filialId: IDS.filial,
  clienteId: IDS.cliente,
  veiculoId: IDS.veiculo,
  responsavelId: null,
  status: 'agendado',
  inicio: '2099-01-01T14:00:00.000Z',
  fim: '2099-01-01T15:30:00.000Z',
  duracaoTotalMin: 90,
  valorTotal: 150,
  observacoes: null,
  versao: 1,
  itens: [
    {
      id: 'a1',
      servicoId: IDS.servicoA,
      nomeServico: 'Lavagem simples',
      precoAplicado: 50,
      duracaoAplicada: 30,
    },
  ],
  criadoEm: '2026-05-21T12:00:00.000Z',
  mensagem: 'Agendamento criado com sucesso.',
  traceId: 'trace-abc',
};

/** Fixture de item da agenda no formato `simples`. */
const agendaItemSimples: AgendaItemSimples = {
  agendamentoId: '99999999-9999-4999-8999-999999999999',
  inicio: '2099-01-01T13:00:00.000Z',
  fim: '2099-01-01T14:30:00.000Z',
  titulo: 'Lavagem Completa',
  status: 'AGENDADO',
  clienteNome: 'Maria Souza',
  veiculoPlaca: 'ABC1D23',
  servicosResumo: 'Lavagem Completa + 1',
};

/** Fixture de item da agenda no formato `detalhado`. */
const agendaItemDetalhado: AgendaItemDetalhado = {
  agendamentoId: '99999999-9999-4999-8999-999999999999',
  status: 'AGENDADO',
  filialId: IDS.filial,
  inicio: '2099-01-01T13:00:00.000Z',
  fim: '2099-01-01T14:30:00.000Z',
  duracaoTotalMin: 90,
  valorTotal: 150,
  cliente: {
    id: IDS.cliente,
    nome: 'Maria Souza',
    cpfCnpj: '12345678901',
    telefone: '1133334444',
    celular: '11999998888',
  },
  veiculo: {
    id: IDS.veiculo,
    placa: 'ABC1D23',
    modelo: 'Civic',
    fabricante: 'Honda',
    cor: 'Preto',
  },
  servicos: [{ id: IDS.servicoA, nome: 'Lavagem Completa', duracaoMin: 60, preco: 100 }],
  observacoes: 'Cliente pediu atenção ao porta-malas.',
  criadoEm: '2026-04-20T12:00:00.000Z',
  atualizadoEm: '2026-04-20T12:00:00.000Z',
};

/** Resposta `200` de `GET /api/v1/agenda` no formato `simples`. */
const agendaRespostaSimples: ConsultarAgendaResponse<AgendaItemSimples> = {
  message: 'Agenda consultada com sucesso.',
  data: [agendaItemSimples],
  traceId: 'trace-agenda-simples',
};

/** Resposta `200` de `GET /api/v1/agenda` no formato `detalhado`. */
const agendaRespostaDetalhada: ConsultarAgendaResponse<AgendaItemDetalhado> = {
  message: 'Agenda consultada com sucesso.',
  data: [agendaItemDetalhado],
  traceId: 'trace-agenda-detalhada',
};

/** Handlers do "caminho feliz" — listas de apoio e criação bem-sucedida. */
export const handlersPadrao = [
  http.get('/api/v1/clientes', () =>
    HttpResponse.json({
      itens: [
        {
          id: IDS.cliente,
          nome: 'Cliente Teste',
          celular: '11999990000',
          cidade: 'São Paulo',
          uf: 'SP',
          ativo: true,
          criadoEm: '2026-01-01T00:00:00.000Z',
        },
      ],
      total: 1,
      pagina: 1,
      tamanhoPagina: 50,
    }),
  ),

  http.get('/api/v1/veiculos', () =>
    HttpResponse.json({
      itens: [
        {
          id: IDS.veiculo,
          clienteId: IDS.cliente,
          placa: 'ABC1D23',
          marca: 'Fiat',
          modelo: 'Uno',
          ativo: true,
        },
      ],
      total: 1,
      pagina: 1,
      tamanhoPagina: 100,
    }),
  ),

  http.get('/api/v1/filiais', () =>
    HttpResponse.json({
      itens: [
        { id: IDS.filial, nome: 'Filial Centro', cidade: 'São Paulo', uf: 'SP', ativo: true },
      ],
      total: 1,
    }),
  ),

  http.get('/api/v1/servicos', () =>
    HttpResponse.json({
      itens: [
        { id: IDS.servicoA, nome: 'Lavagem simples', precoBase: 50, duracaoMin: 30, ativo: true },
        { id: IDS.servicoB, nome: 'Enceramento', precoBase: 100, duracaoMin: 60, ativo: true },
      ],
      total: 2,
    }),
  ),

  http.post('/api/v1/agendamentos', () => HttpResponse.json(respostaCriacao, { status: 201 })),

  // Visualização de agenda (RF009 / card 132): responde conforme o `formato`.
  http.get('/api/v1/agenda', ({ request }) => {
    const formato = new URL(request.url).searchParams.get('formato');
    return HttpResponse.json(
      formato === 'detalhado' ? agendaRespostaDetalhada : agendaRespostaSimples,
    );
  }),
];

/** Resposta de criação reaproveitável em asserções. */
export { respostaCriacao, agendaRespostaSimples, agendaRespostaDetalhada };
