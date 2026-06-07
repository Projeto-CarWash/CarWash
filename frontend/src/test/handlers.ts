import { HttpResponse, http } from 'msw';

import type { AgendaItemDetalhado, AgendaItemSimples } from '@/types/agenda';
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

const agendaItemSimples: AgendaItemSimples = {
  agendamentoId: 'aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa',
  inicio: '2099-01-01T14:00:00.000Z',
  fim: '2099-01-01T15:30:00.000Z',
  titulo: 'Lavagem Completa',
  status: 'AGENDADO',
  clienteNome: 'Maria Souza',
  veiculoPlaca: 'ABC1D23',
  servicosResumo: 'Lavagem Completa + 1',
};

const agendaItemDetalhado: AgendaItemDetalhado = {
  agendamentoId: agendaItemSimples.agendamentoId,
  status: 'AGENDADO',
  filialId: IDS.filial,
  inicio: agendaItemSimples.inicio,
  fim: agendaItemSimples.fim,
  duracaoTotalMin: 90,
  valorTotal: 150,
  cliente: {
    id: IDS.cliente,
    nome: 'Maria Souza',
    cpfCnpj: '12345678901',
    telefone: null,
    celular: '11999990000',
  },
  veiculo: {
    id: IDS.veiculo,
    placa: 'ABC1D23',
    modelo: 'Civic',
    fabricante: 'Honda',
    cor: 'Prata',
  },
  servicos: [
    { id: IDS.servicoA, nome: 'Lavagem Completa', duracaoMin: 30, preco: 50 },
    { id: IDS.servicoB, nome: 'Enceramento', duracaoMin: 60, preco: 100 },
  ],
  observacoes: 'Atenção ao porta-malas',
  criadoEm: '2026-05-20T10:00:00.000Z',
  atualizadoEm: '2026-05-20T10:05:00.000Z',
};

/** Handlers do "caminho feliz" — listas de apoio e fluxo de confirmação. */
export const handlersPadrao = [
  // Auth (RF001): o AuthProvider chama POST /auth/refresh no mount para tentar
  // restaurar a sessão a partir do cookie httpOnly. O default devolve 401
  // ("sem sessão") — assim o boot do provider nunca dispara request sem handler
  // sob `onUnhandledRequest: 'error'`. Testes de sessão restaurada sobrescrevem
  // este handler via `server.use(...)` para devolver 200 com LoginResponse.
  http.post('/api/v1/auth/refresh', () =>
    HttpResponse.json({ title: 'Não autenticado.', status: 401 }, { status: 401 }),
  ),

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
          fabricante: 'Fiat',
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
        {
          id: IDS.servicoA,
          nome: 'Lavagem simples',
          preco: 50,
          duracaoMin: 30,
          ativo: true,
          criadoEm: '2026-01-01T00:00:00.000Z',
          atualizadoEm: '2026-01-01T00:00:00.000Z',
        },
        {
          id: IDS.servicoB,
          nome: 'Enceramento',
          preco: 100,
          duracaoMin: 60,
          ativo: true,
          criadoEm: '2026-01-01T00:00:00.000Z',
          atualizadoEm: '2026-01-01T00:00:00.000Z',
        },
      ],
      total: 2,
    }),
  ),

  http.patch('/api/v1/servicos/:id/status', async ({ request, params }) => {
    const body = (await request.json()) as { ativo?: boolean };
    return HttpResponse.json(
      {
        id: params.id,
        nome: 'Serviço',
        preco: 50,
        duracaoMin: 30,
        ativo: Boolean(body.ativo),
        criadoEm: '2026-01-01T00:00:00.000Z',
        atualizadoEm: new Date().toISOString(),
      },
      { status: 200 },
    );
  }),

  http.patch('/api/v1/servicos/:id', async ({ request, params }) => {
    const body = (await request.json()) as { nome?: string; preco?: number; duracaoMin?: number };
    return HttpResponse.json(
      {
        id: params.id,
        nome: body.nome ?? 'Serviço',
        preco: body.preco ?? 50,
        duracaoMin: body.duracaoMin ?? 30,
        ativo: true,
        criadoEm: '2026-01-01T00:00:00.000Z',
        atualizadoEm: new Date().toISOString(),
      },
      { status: 200 },
    );
  }),

  http.post('/api/v1/servicos', async ({ request }) => {
    const body = (await request.json()) as { nome?: string; preco?: number; duracaoMin?: number };
    return HttpResponse.json(
      {
        id: crypto.randomUUID(),
        nome: body.nome ?? 'Novo Serviço',
        preco: body.preco ?? 50,
        duracaoMin: body.duracaoMin ?? 30,
        ativo: true,
        criadoEm: new Date().toISOString(),
        atualizadoEm: new Date().toISOString(),
      },
      { status: 201 },
    );
  }),

  http.get('/api/v1/agenda', ({ request }) => {
    const formato = new URL(request.url).searchParams.get('formato');

    if (formato === 'detalhado') {
      return HttpResponse.json({
        message: 'Agenda consultada com sucesso.',
        data: [agendaItemDetalhado],
        traceId: 'trace-agenda-detalhada',
      });
    }

    return HttpResponse.json({
      message: 'Agenda consultada com sucesso.',
      data: [agendaItemSimples],
      traceId: 'trace-agenda-simples',
    });
  }),

  http.post('/api/v1/agendamentos', () => HttpResponse.json(respostaCriacao, { status: 201 })),

  http.post('/api/v1/agendamentos/pre-confirmacao', () =>
    HttpResponse.json(respostaPreConfirmacao, { status: 200 }),
  ),

  http.post('/api/v1/agendamentos/confirmar', () =>
    HttpResponse.json(respostaCriacao, { status: 201 }),
  ),

  http.patch('/api/v1/agendamentos/:id/cancelar', async ({ params, request }) => {
    const body = (await request.json()) as { motivoCancelamento?: string };
    const id = params.id as string;
    const motivo = body.motivoCancelamento?.trim() ?? '';

    if (motivo === 'trigger-409') {
      return HttpResponse.json(
        {
          title: 'Conflito de estado inválido para operação.',
          detail: 'Não é possível cancelar o agendamento.',
        },
        { status: 409 },
      );
    }
    if (motivo === 'trigger-403') {
      return HttpResponse.json({ title: 'Sem permissão.' }, { status: 403 });
    }
    if (motivo === 'trigger-401') {
      return HttpResponse.json({ title: 'Não autenticado.' }, { status: 401 });
    }
    if (motivo === 'trigger-404') {
      return HttpResponse.json({ title: 'Não encontrado.' }, { status: 404 });
    }
    if (motivo === 'trigger-500') {
      return HttpResponse.json({ title: 'Erro interno.' }, { status: 500 });
    }

    if (motivo.length < 5) {
      return HttpResponse.json(
        {
          title: 'Dados inválidos.',
          errors: {
            motivoCancelamento: ['O motivo do cancelamento deve ter pelo menos 5 caracteres.'],
          },
        },
        { status: 400 },
      );
    }

    return HttpResponse.json({
      message: 'Agendamento cancelado com sucesso.',
      data: {
        id,
        status: 'CANCELADO',
        canceladoEm: new Date().toISOString(),
        canceladoPor: '00000000-0000-0000-0000-000000000001',
        motivoCancelamento: motivo,
      },
      traceId: 'trace-cancel',
    });
  }),

  http.put('/api/v1/agendamentos/:id', async ({ params, request }) => {
    const body = (await request.json()) as { observacoes?: string | null };
    const id = params.id as string;
    const obs = body.observacoes?.trim() ?? '';

    if (obs === 'trigger-409') {
      return HttpResponse.json(
        { title: 'O agendamento não permite edição neste status.', detail: 'Status inválido.' },
        { status: 409 },
      );
    }
    if (obs === 'trigger-403') {
      return HttpResponse.json({ title: 'Sem permissão.' }, { status: 403 });
    }
    if (obs === 'trigger-401') {
      return HttpResponse.json({ title: 'Não autenticado.' }, { status: 401 });
    }
    if (obs === 'trigger-404') {
      return HttpResponse.json({ title: 'Não encontrado.' }, { status: 404 });
    }
    if (obs === 'trigger-500') {
      return HttpResponse.json({ title: 'Erro interno.' }, { status: 500 });
    }
    if (obs === 'trigger-400') {
      return HttpResponse.json(
        { title: 'Dados inválidos.', errors: { observacoes: ['Observações inválidas.'] } },
        { status: 400 },
      );
    }

    return HttpResponse.json({
      id,
      filialId: IDS.filial,
      clienteId: IDS.cliente,
      veiculoId: IDS.veiculo,
      responsavelId: null,
      status: 'AGENDADO',
      inicio: '2099-01-01T14:00:00.000Z',
      fim: '2099-01-01T15:30:00.000Z',
      duracaoTotalMin: 90,
      valorTotal: 150,
      observacoes: body.observacoes ?? null,
      versao: 2,
      itens: [],
      criadoEm: '2026-05-21T12:00:00.000Z',
      mensagem: 'Agendamento atualizado com sucesso.',
      traceId: 'trace-edit-mock',
    });
  }),
];

/** Fixtures reaproveitáveis em asserções. */
export { respostaCriacao, respostaPreConfirmacao };
