import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { describe, expect, it } from 'vitest';

import { NovoAgendamentoPage } from '@/components/agendamentos/NovoAgendamentoPage';
import { IDS, respostaCriacao } from '@/test/handlers';
import { server } from '@/test/mswServer';
import { renderComProviders } from '@/test/renderComProviders';

/**
 * Testes de integração da tela de criação de agendamento com confirmação em
 * 2 etapas (RF007 + RF015 / card 133).
 *
 * <p>Cobertura: validação Zod, transição edição→revisão via pré-confirmação,
 * confirmação bem-sucedida, e os erros de negócio em cada etapa (409 conflito
 * RN011, 409 divergência de resumo, 410 sessão expirada, 400 por campo).</p>
 */

/** Início no formato datetime-local, sempre no futuro. */
function inicioFuturo(): string {
  const d = new Date(Date.now() + 48 * 60 * 60 * 1000);
  return d.toISOString().slice(0, 16);
}

/** Aguarda as listas de apoio carregarem (selects deixam de estar disabled). */
async function aguardarListas() {
  await waitFor(() => {
    expect(screen.getByLabelText('Cliente')).not.toBeDisabled();
    expect(screen.getByLabelText('Filial')).not.toBeDisabled();
  });
}

/**
 * Aguarda e retorna o banner de erro global (distinto dos erros de campo
 * pelo atributo `aria-live="assertive"`).
 */
async function acharErroGlobal() {
  return waitFor(() => {
    const alertas = screen.getAllByRole('alert');
    const global = alertas.find((el) => el.getAttribute('aria-live') === 'assertive');
    expect(global).toBeDefined();
    return global!;
  });
}

/** Preenche o formulário com dados válidos (etapa de edição). */
async function preencherFormularioValido(user: ReturnType<typeof userEvent.setup>) {
  await aguardarListas();

  await user.selectOptions(screen.getByLabelText('Cliente'), IDS.cliente);
  // Após escolher cliente, o select de veículo deixa de estar disabled.
  await waitFor(() => expect(screen.getByLabelText('Veículo')).not.toBeDisabled());
  await user.selectOptions(screen.getByLabelText('Veículo'), IDS.veiculo);
  await user.selectOptions(screen.getByLabelText('Filial'), IDS.filial);

  const inicio = screen.getByLabelText('Data e hora de início');
  await user.clear(inicio);
  await user.type(inicio, inicioFuturo());

  // Serviços — botões alternáveis com aria-pressed.
  await user.click(screen.getByRole('button', { name: /lavagem simples/i }));
}

/** Preenche o formulário e avança para a etapa de revisão. */
async function irParaRevisao(user: ReturnType<typeof userEvent.setup>) {
  await preencherFormularioValido(user);
  await user.click(screen.getByRole('button', { name: /revisar agendamento/i }));
  await screen.findByRole('button', { name: /confirmar agendamento/i });
}

describe('NovoAgendamentoPage — etapa de edição', () => {
  it('exibe erros de validação Zod ao submeter o formulário vazio', async () => {
    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);
    await aguardarListas();

    await user.click(screen.getByRole('button', { name: /revisar agendamento/i }));

    expect(await screen.findByText(/selecione a filial/i)).toBeInTheDocument();
    expect(screen.getByText(/selecione o cliente/i)).toBeInTheDocument();
    expect(screen.getByText(/selecione ao menos um serviço/i)).toBeInTheDocument();
  });

  it('atualiza o resumo inline com a duração e o valor totais dos serviços', async () => {
    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);
    await aguardarListas();

    await user.click(await screen.findByRole('button', { name: /lavagem simples/i }));
    await user.click(screen.getByRole('button', { name: /enceramento/i }));

    // Lavagem 50 + Enceramento 100 = 150; 30min + 60min = 90min.
    await waitFor(() => {
      expect(screen.getByTestId('resumo-valor')).toHaveTextContent('R$ 150,00');
      expect(screen.getByTestId('resumo-duracao')).toHaveTextContent('1 h 30 min');
    });
  });

  it('avança para a revisão após pré-confirmação bem-sucedida', async () => {
    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /revisar agendamento/i }));

    // A etapa de revisão exibe o resumo retornado pelo backend.
    expect(
      await screen.findByRole('button', { name: /confirmar agendamento/i }),
    ).toBeInTheDocument();
    expect(screen.getByText(/revise antes de confirmar/i)).toBeInTheDocument();
    expect(screen.getByText('Filial Centro')).toBeInTheDocument();
  });

  it('envia inicio em ISO-8601 UTC e sem campos derivados na pré-confirmação', async () => {
    let corpoEnviado: Record<string, unknown> | null = null;
    server.use(
      http.post('/api/v1/agendamentos/pre-confirmacao', async ({ request }) => {
        corpoEnviado = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            tokenConfirmacao: 't',
            expiraEm: '2099-01-01T13:45:00.000Z',
            resumo: {
              filial: { id: IDS.filial, nome: 'Filial Centro' },
              cliente: { id: IDS.cliente, nome: 'Cliente Teste', documento: '000' },
              veiculo: { id: IDS.veiculo, placa: 'ABC1D23', modelo: 'Uno', cor: 'Prata' },
              servicos: [{ id: IDS.servicoA, nome: 'Lavagem simples', duracaoMin: 30, preco: 50 }],
              inicio: '2099-01-01T14:00:00.000Z',
              fim: '2099-01-01T14:30:00.000Z',
              duracaoTotalMin: 30,
              valorTotal: 50,
              observacoes: null,
              hashResumo: 'h',
            },
            traceId: 'tr',
          },
          { status: 200 },
        );
      }),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /revisar agendamento/i }));
    await screen.findByRole('button', { name: /confirmar agendamento/i });

    expect(corpoEnviado).not.toBeNull();
    const corpo = corpoEnviado!;
    expect(corpo).not.toHaveProperty('fim');
    expect(corpo).not.toHaveProperty('valorTotal');
    expect(corpo.inicio).toMatch(/Z$/);
  });

  it('mostra conflito de agenda do veículo já na pré-confirmação (409 / RN011)', async () => {
    server.use(
      http.post('/api/v1/agendamentos/pre-confirmacao', () =>
        HttpResponse.json(
          { title: 'O veículo já possui um agendamento neste horário (RN011).' },
          { status: 409 },
        ),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /revisar agendamento/i }));

    const alerta = await acharErroGlobal();
    expect(within(alerta).getByText(/RN011/)).toBeInTheDocument();
    // Permanece na etapa de edição.
    expect(screen.getByRole('button', { name: /revisar agendamento/i })).toBeInTheDocument();
    expect(await screen.findByText(/conflito de agenda neste horário/i)).toBeInTheDocument();
  });

  it('destaca campos retornados pelo backend em validação 400 na pré-confirmação', async () => {
    server.use(
      http.post('/api/v1/agendamentos/pre-confirmacao', () =>
        HttpResponse.json(
          {
            title: 'Dados inválidos.',
            errors: { inicio: ['O horário está fora do funcionamento da filial.'] },
          },
          { status: 400 },
        ),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /revisar agendamento/i }));

    expect(await screen.findByText(/fora do funcionamento da filial/i)).toBeInTheDocument();
  });
});

describe('NovoAgendamentoPage — etapa de revisão (RF015)', () => {
  it('confirma o agendamento e exibe a mensagem de sucesso do backend', async () => {
    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await irParaRevisao(user);
    await user.click(screen.getByRole('button', { name: /confirmar agendamento/i }));

    const status = await screen.findByRole('status');
    expect(within(status).getByText(/agendamento criado com sucesso/i)).toBeInTheDocument();
  });

  it('envia tokenConfirmacao, confirmar e idempotencyKey no payload de confirmação', async () => {
    let corpoEnviado: Record<string, unknown> | null = null;
    server.use(
      http.post('/api/v1/agendamentos/confirmar', async ({ request }) => {
        corpoEnviado = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ ...respostaCriacao }, { status: 201 });
      }),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await irParaRevisao(user);
    await user.click(screen.getByRole('button', { name: /confirmar agendamento/i }));
    await screen.findByRole('status');

    expect(corpoEnviado).not.toBeNull();
    const corpo = corpoEnviado!;
    expect(corpo.confirmar).toBe(true);
    expect(corpo.tokenConfirmacao).toBe('token-revisao-123');
    expect(typeof corpo.idempotencyKey).toBe('string');
    expect((corpo.idempotencyKey as string).length).toBeGreaterThan(0);
  });

  it('o botão "Editar" volta para o formulário preservando os dados', async () => {
    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await irParaRevisao(user);
    await user.click(screen.getByRole('button', { name: /editar/i }));

    // Voltou para a edição com o cliente ainda selecionado.
    const cliente = await screen.findByLabelText('Cliente');
    expect(cliente).toHaveValue(IDS.cliente);
  });

  it('exibe conflito de horário (409) sem sair da revisão', async () => {
    server.use(
      http.post('/api/v1/agendamentos/confirmar', () =>
        HttpResponse.json(
          { title: 'O horário não está mais disponível para este veículo (RN011).' },
          { status: 409 },
        ),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await irParaRevisao(user);
    await user.click(screen.getByRole('button', { name: /confirmar agendamento/i }));

    expect(await screen.findByText(/não está mais disponível/i)).toBeInTheDocument();
    // Continua na revisão.
    expect(screen.getByRole('button', { name: /confirmar agendamento/i })).toBeInTheDocument();
  });

  it('volta para a edição em divergência de resumo (409)', async () => {
    server.use(
      http.post('/api/v1/agendamentos/confirmar', () =>
        HttpResponse.json(
          {
            title: 'Os dados do agendamento foram alterados. Revise antes de confirmar.',
          },
          { status: 409 },
        ),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await irParaRevisao(user);
    await user.click(screen.getByRole('button', { name: /confirmar agendamento/i }));

    // Volta para a edição com aviso de divergência.
    const alerta = await acharErroGlobal();
    expect(within(alerta).getByText(/foram alterados/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /revisar agendamento/i })).toBeInTheDocument();
  });

  it('volta para a edição quando a sessão de confirmação expira (410)', async () => {
    server.use(
      http.post('/api/v1/agendamentos/confirmar', () =>
        HttpResponse.json({ title: 'Token expirado.' }, { status: 410 }),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await irParaRevisao(user);
    await user.click(screen.getByRole('button', { name: /confirmar agendamento/i }));

    const alerta = await acharErroGlobal();
    expect(within(alerta).getByText(/sessão de confirmação expirada/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /revisar agendamento/i })).toBeInTheDocument();
  });

  it('mostra mensagem genérica em erro interno do servidor (500) na confirmação', async () => {
    server.use(
      http.post('/api/v1/agendamentos/confirmar', () => HttpResponse.json({}, { status: 500 })),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await irParaRevisao(user);
    await user.click(screen.getByRole('button', { name: /confirmar agendamento/i }));

    expect(await screen.findByText(/não foi possível concluir o agendamento/i)).toBeInTheDocument();
  });
});
