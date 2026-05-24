import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { describe, expect, it } from 'vitest';

import { IDS } from '@/test/handlers';
import { server } from '@/test/mswServer';
import { renderComProviders } from '@/test/renderComProviders';

import { AgendaPage } from './AgendaPage';

/**
 * Testes de integração da tela de visualização de agenda (RF009 / card 132).
 *
 * <p>Cobertura: render dos formatos simples e detalhado, estado vazio, estado
 * de erro, alternância de formato e aplicação de filtro. As chamadas HTTP são
 * interceptadas pelo MSW (`handlersPadrao` + sobrescritas pontuais).</p>
 */

/** Valor `datetime-local` deslocado de `dias` a partir de agora. */
function emDias(dias: number): string {
  const d = new Date(Date.now() + dias * 24 * 60 * 60 * 1000);
  return d.toISOString().slice(0, 16);
}

/** Aguarda o select de filial carregar (deixa de estar disabled). */
async function aguardarFiliais() {
  await waitFor(() => expect(screen.getByLabelText('Filial')).not.toBeDisabled());
}

/**
 * Preenche os filtros obrigatórios com um período válido e seleciona a filial.
 */
async function preencherFiltros(user: ReturnType<typeof userEvent.setup>) {
  await aguardarFiliais();
  await user.selectOptions(screen.getByLabelText('Filial'), IDS.filial);

  const inicio = screen.getByLabelText('Início do período');
  await user.clear(inicio);
  await user.type(inicio, emDias(0));

  const fim = screen.getByLabelText('Fim do período');
  await user.clear(fim);
  await user.type(fim, emDias(5));
}

describe('AgendaPage', () => {
  it('mostra estado inicial pedindo para definir filtros e buscar', () => {
    renderComProviders(<AgendaPage />);
    expect(screen.getByText(/defina os filtros e busque a agenda/i)).toBeInTheDocument();
  });

  it('renderiza a agenda no formato simples após buscar', async () => {
    const user = userEvent.setup();
    renderComProviders(<AgendaPage />);

    await preencherFiltros(user);
    await user.click(screen.getByRole('button', { name: /buscar agenda/i }));

    expect(await screen.findByText('Lavagem Completa')).toBeInTheDocument();
    expect(screen.getByText('Maria Souza')).toBeInTheDocument();
    expect(screen.getByText('ABC1D23')).toBeInTheDocument();
    expect(screen.getByText('Lavagem Completa + 1')).toBeInTheDocument();
  });

  it('renderiza a agenda no formato detalhado com dados completos', async () => {
    const user = userEvent.setup();
    renderComProviders(<AgendaPage />);

    await user.click(screen.getByRole('button', { name: /detalhado/i }));
    await preencherFiltros(user);
    await user.click(screen.getByRole('button', { name: /buscar agenda/i }));

    // Cartão detalhado expõe blocos de cliente e veículo.
    expect(await screen.findByLabelText('Dados do cliente')).toBeInTheDocument();
    expect(screen.getByLabelText('Dados do veículo')).toBeInTheDocument();
    expect(screen.getByText('Honda')).toBeInTheDocument();
    expect(screen.getByText('R$ 150,00')).toBeInTheDocument();
    expect(screen.getByText(/atenção ao porta-malas/i)).toBeInTheDocument();
  });

  it('exibe o estado vazio com a mensagem do backend', async () => {
    server.use(
      http.get('/api/v1/agenda', () =>
        HttpResponse.json({
          message: 'Nenhum evento encontrado para o período selecionado.',
          data: [],
          traceId: 'trace-vazio',
        }),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<AgendaPage />);

    await preencherFiltros(user);
    await user.click(screen.getByRole('button', { name: /buscar agenda/i }));

    expect(
      await screen.findByText(/nenhum evento encontrado para o período selecionado/i),
    ).toBeInTheDocument();
  });

  it('exibe o estado de erro quando a consulta falha (500)', async () => {
    server.use(http.get('/api/v1/agenda', () => HttpResponse.json({}, { status: 500 })));

    const user = userEvent.setup();
    renderComProviders(<AgendaPage />);

    await preencherFiltros(user);
    await user.click(screen.getByRole('button', { name: /buscar agenda/i }));

    const alerta = await screen.findByRole('alert');
    expect(within(alerta).getByText(/não foi possível/i)).toBeInTheDocument();
    expect(within(alerta).getByRole('button', { name: /tentar novamente/i })).toBeInTheDocument();
  });

  it('mostra o motivo do backend ao receber validação 400', async () => {
    server.use(
      http.get('/api/v1/agenda', () =>
        HttpResponse.json(
          {
            title: 'Parâmetros inválidos.',
            errors: { filialId: ['Filial não encontrada.'] },
          },
          { status: 400 },
        ),
      ),
    );

    const user = userEvent.setup();
    renderComProviders(<AgendaPage />);

    await preencherFiltros(user);
    await user.click(screen.getByRole('button', { name: /buscar agenda/i }));

    expect(await screen.findByText(/filial não encontrada/i)).toBeInTheDocument();
  });

  it('alterna do formato simples para o detalhado e re-busca', async () => {
    const user = userEvent.setup();
    renderComProviders(<AgendaPage />);

    await preencherFiltros(user);
    await user.click(screen.getByRole('button', { name: /buscar agenda/i }));

    // Começa em simples — linha compacta, sem bloco de cliente detalhado.
    expect(await screen.findByText('Lavagem Completa')).toBeInTheDocument();
    expect(screen.queryByLabelText('Dados do cliente')).not.toBeInTheDocument();

    // Alterna para detalhado: a query re-busca com formato=detalhado.
    await user.click(screen.getByRole('button', { name: /detalhado/i }));
    expect(await screen.findByLabelText('Dados do cliente')).toBeInTheDocument();
  });

  it('valida no cliente que o período não pode ultrapassar 31 dias', async () => {
    const user = userEvent.setup();
    renderComProviders(<AgendaPage />);

    await aguardarFiliais();
    await user.selectOptions(screen.getByLabelText('Filial'), IDS.filial);

    const inicio = screen.getByLabelText('Início do período');
    await user.clear(inicio);
    await user.type(inicio, emDias(0));

    const fim = screen.getByLabelText('Fim do período');
    await user.clear(fim);
    await user.type(fim, emDias(40));

    await user.click(screen.getByRole('button', { name: /buscar agenda/i }));

    expect(await screen.findByText(/não pode ultrapassar 31 dias/i)).toBeInTheDocument();
    // Sem consulta válida, segue no estado inicial.
    expect(screen.queryByText('Lavagem Completa')).not.toBeInTheDocument();
  });

  it('aplica o filtro de status na requisição da agenda', async () => {
    let statusRecebido: string | null = null;
    server.use(
      http.get('/api/v1/agenda', ({ request }) => {
        statusRecebido = new URL(request.url).searchParams.get('status');
        return HttpResponse.json({
          message: 'Agenda consultada com sucesso.',
          data: [],
          traceId: 'trace-filtro',
        });
      }),
    );

    const user = userEvent.setup();
    renderComProviders(<AgendaPage />);

    await preencherFiltros(user);
    await user.selectOptions(screen.getByLabelText(/status/i), 'CONCLUIDO');
    await user.click(screen.getByRole('button', { name: /buscar agenda/i }));

    await screen.findByText(/nenhum evento encontrado/i);
    expect(statusRecebido).toBe('CONCLUIDO');
  });

  it('permite informar a filial manualmente quando o catálogo está indisponível', async () => {
    server.use(http.get('/api/v1/filiais', () => HttpResponse.json({}, { status: 404 })));

    const user = userEvent.setup();
    renderComProviders(<AgendaPage />);

    // Sem catálogo, o campo de filial vira um input de texto.
    expect(await screen.findByText(/catálogo de filiais indisponível/i)).toBeInTheDocument();
    const filialInput = screen.getByLabelText('Filial');
    expect(filialInput).toHaveAttribute('type', 'text');

    await user.type(filialInput, IDS.filial);

    const inicio = screen.getByLabelText('Início do período');
    await user.clear(inicio);
    await user.type(inicio, emDias(0));
    const fim = screen.getByLabelText('Fim do período');
    await user.clear(fim);
    await user.type(fim, emDias(5));

    await user.click(screen.getByRole('button', { name: /buscar agenda/i }));

    expect(await screen.findByText('Lavagem Completa')).toBeInTheDocument();
  });
});
