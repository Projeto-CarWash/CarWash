import { HttpResponse, http } from 'msw';

import type { AgendamentoResponse, PreConfirmacaoResponse } from '@/types/agendamento';

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

/** Resumo de pré-confirmação reaproveitável (RF015, card 133). */
const respostaPreConfirmacao: PreConfirmacaoResponse = {
  tokenConfirmacao: 'token-revisao-123',
  expiraEm: '2099-01-01T13:45:00.000Z',
  resumo: {
    filial: { id: IDS.filial, nome: 'Filial Centro' },
    cliente: {
      id: IDS.cliente,
      nome: 'Cliente Teste',
      documento: '123.456.789-00',
    },
    veiculo: {
      id: IDS.veiculo,
      placa: 'ABC1D23',
      modelo: 'Uno',
      cor: 'Prata',
    },
    servicos: [{ id: IDS.servicoA, nome: 'Lavagem simples', duracaoMin: 30, preco: 50 }],
    inicio: '2099-01-01T14:00:00.000Z',
    fim: '2099-01-01T14:30:00.000Z',
    duracaoTotalMin: 30,
    valorTotal: 50,
    observacoes: null,
    hashResumo: 'hash-abc',
  },
  traceId: 'trace-pre-abc',
};

/** Handlers do "caminho feliz" — listas de apoio e fluxo de confirmação. */
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

  http.post('/api/v1/agendamentos/pre-confirmacao', () =>
    HttpResponse.json(respostaPreConfirmacao, { status: 200 }),
  ),

  http.post('/api/v1/agendamentos/confirmar', () =>
    HttpResponse.json(respostaCriacao, { status: 201 }),
  ),

  http.get('/api/v1/agenda', ({ request }) => {
    const url = new URL(request.url);
    const formato = url.searchParams.get('formato');

    if (formato === 'detalhado') {
      return HttpResponse.json({
        message: 'Agenda consultada com sucesso.',
        data: [
          {
            agendamentoId: '99999999-9999-4999-8999-999999999999',
            status: 'AGENDADO',
            filialId: IDS.filial,
            inicio: '2099-01-01T14:00:00.000Z',
            fim: '2099-01-01T14:30:00.000Z',
            duracaoTotalMin: 30,
            valorTotal: 150.00,
            cliente: {
              id: IDS.cliente,
              nome: 'Maria Souza',
              cpfCnpj: '123.456.789-00',
              telefone: null,
              celular: '11999990000',
            },
            veiculo: {
              id: IDS.veiculo,
              placa: 'ABC1D23',
              modelo: 'Uno',
              fabricante: 'Honda',
              cor: 'Prata',
            },
            servicos: [
              {
                id: IDS.servicoA,
                nome: 'Lavagem Completa',
                duracaoMin: 30,
                preco: 50.00,
              },
            ],
            observacoes: 'atenção ao porta-malas',
            criadoEm: '2026-05-21T12:00:00.000Z',
            atualizadoEm: '2026-05-21T12:00:00.000Z',
          },
        ],
        traceId: 'trace-detalhado',
      });
    }

    return HttpResponse.json({
      message: 'Agenda consultada com sucesso.',
      data: [
        {
          agendamentoId: '99999999-9999-4999-8999-999999999999',
          inicio: '2099-01-01T14:00:00.000Z',
          fim: '2099-01-01T14:30:00.000Z',
          titulo: 'Lavagem Completa',
          status: 'AGENDADO',
          clienteNome: 'Maria Souza',
          veiculoPlaca: 'ABC1D23',
          servicosResumo: 'Lavagem Completa + 1',
        },
      ],
      traceId: 'trace-simples',
    });
  }),
];

/** Fixtures reaproveitáveis em asserções. */
export { respostaCriacao, respostaPreConfirmacao };
