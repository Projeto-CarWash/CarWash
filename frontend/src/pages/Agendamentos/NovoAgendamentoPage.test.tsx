import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { describe, expect, it } from 'vitest';

import { IDS, respostaCriacao } from '@/test/handlers';
import { server } from '@/test/mswServer';
import { renderComProviders } from '@/test/renderComProviders';

import { NovoAgendamentoPage } from './NovoAgendamentoPage';

/**
 * Testes de integração da tela de criação de agendamento (RF007 / card 131).
 *
 * <p>Cobertura: submit válido (POST 201), erros de validação Zod e tratamento
 * dos erros de negócio do backend (409 conflito de agenda, 422 recurso
 * inativo).</p>
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

/** Preenche o formulário com dados válidos. */
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

describe('NovoAgendamentoPage', () => {
  it('exibe erros de validação Zod ao submeter o formulário vazio', async () => {
    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);
    await aguardarListas();

    await user.click(screen.getByRole('button', { name: /criar agendamento/i }));

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

  it('cria o agendamento com sucesso e exibe a mensagem do backend', async () => {
    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /criar agendamento/i }));

    const status = await screen.findByRole('status');
    expect(within(status).getByText(/agendamento criado com sucesso/i)).toBeInTheDocument();
  });

  it('mostra mensagem amigável de conflito de agenda do veículo (409 / RN011)', async () => {
    server.use(
      http.post('/api/v1/agendamentos', () =>
        HttpResponse.json(
          { title: 'O veículo já possui um agendamento neste horário (RN011).' },
          { status: 409 },
        ),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /criar agendamento/i }));

    const alerta = await acharErroGlobal();
    expect(within(alerta).getByText(/RN011/)).toBeInTheDocument();
    // O veículo e o início são destacados como campos em conflito.
    expect(await screen.findByText(/conflito de agenda neste horário/i)).toBeInTheDocument();
  });

  it('mostra mensagem amigável quando há recurso inativo (422)', async () => {
    server.use(
      http.post('/api/v1/agendamentos', () =>
        HttpResponse.json({ title: 'A filial selecionada está desativada.' }, { status: 422 }),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /criar agendamento/i }));

    const alerta = await acharErroGlobal();
    expect(within(alerta).getByText(/filial selecionada está desativada/i)).toBeInTheDocument();
  });

  it('mostra mensagem amigável quando um recurso não é encontrado (404)', async () => {
    server.use(
      http.post('/api/v1/agendamentos', () =>
        HttpResponse.json({ title: 'Veículo informado não foi encontrado.' }, { status: 404 }),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /criar agendamento/i }));

    const alerta = await acharErroGlobal();
    expect(within(alerta).getByText(/não foi encontrado/i)).toBeInTheDocument();
  });

  it('mostra mensagem de permissão negada (403) sem expor detalhes internos', async () => {
    server.use(
      http.post('/api/v1/agendamentos', () =>
        HttpResponse.json({ title: 'detalhe interno' }, { status: 403 }),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /criar agendamento/i }));

    const alerta = await acharErroGlobal();
    // Mensagem genérica de 403 — o title interno do backend é ignorado.
    expect(within(alerta).getByText(/não possui permissão/i)).toBeInTheDocument();
    expect(within(alerta).queryByText(/detalhe interno/i)).not.toBeInTheDocument();
  });

  it('mostra mensagem genérica em erro interno do servidor (500)', async () => {
    server.use(http.post('/api/v1/agendamentos', () => HttpResponse.json({}, { status: 500 })));

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /criar agendamento/i }));

    const alerta = await acharErroGlobal();
    expect(
      within(alerta).getByText(/não foi possível concluir o agendamento/i),
    ).toBeInTheDocument();
  });

  it('não envia fim, duracaoTotalMin nem valorTotal no payload do POST', async () => {
    let corpoEnviado: Record<string, unknown> | null = null;
    server.use(
      http.post('/api/v1/agendamentos', async ({ request }) => {
        corpoEnviado = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ ...respostaCriacao }, { status: 201 });
      }),
    );

    const user = userEvent.setup();
    renderComProviders(<NovoAgendamentoPage />);

    await preencherFormularioValido(user);
    await user.click(screen.getByRole('button', { name: /criar agendamento/i }));

    await screen.findByRole('status');
    expect(corpoEnviado).not.toBeNull();
    const corpo = corpoEnviado!;
    // O servidor deriva esses campos — o cliente não deve enviá-los.
    expect(corpo).not.toHaveProperty('fim');
    expect(corpo).not.toHaveProperty('duracaoTotalMin');
    expect(corpo).not.toHaveProperty('valorTotal');
    // inicio deve ir em ISO-8601 UTC com sufixo Z.
    expect(corpo.inicio).toMatch(/Z$/);
  });

  it('destaca campos retornados pelo backend em validação 400', async () => {
    server.use(
      http.post('/api/v1/agendamentos', () =>
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
    await user.click(screen.getByRole('button', { name: /criar agendamento/i }));

    expect(await screen.findByText(/fora do funcionamento da filial/i)).toBeInTheDocument();
  });
});
