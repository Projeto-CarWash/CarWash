import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { describe, expect, it } from 'vitest';

import { ServicosListaPage } from '@/pages/Servicos/ServicosListaPage';
import { server } from '@/test/mswServer';
import { renderComProviders } from '@/test/renderComProviders';

/**
 * Testes de integração da página de listagem de serviços (RF006).
 *
 * Cobertura:
 *  - carregamento e exibição da lista via GET /api/v1/servicos
 *  - exibição de estado de erro quando a API falha
 *  - toggle de status (ativo/inativo) via PATCH /api/v1/servicos/:id/status
 */

/** Fixtures de serviços usados nos testes. */
const SERVICOS = [
  {
    id: 'aaaa0001-0000-4000-8000-000000000001',
    nome: 'Lavagem Simples',
    preco: 45.0,
    duracaoMin: 40,
    ativo: true,
    criadoEm: '2026-01-01T00:00:00.000Z',
    atualizadoEm: '2026-01-01T00:00:00.000Z',
  },
  {
    id: 'aaaa0002-0000-4000-8000-000000000002',
    nome: 'Polimento Completo',
    preco: 150.0,
    duracaoMin: 180,
    ativo: false,
    criadoEm: '2026-01-02T00:00:00.000Z',
    atualizadoEm: '2026-01-02T00:00:00.000Z',
  },
];

describe('ServicosListaPage (RF006)', () => {
  it('exibe os serviços retornados por GET /api/v1/servicos', async () => {
    server.use(
      http.get('/api/v1/servicos', () =>
        HttpResponse.json({ itens: SERVICOS, total: SERVICOS.length }),
      ),
    );

    renderComProviders(<ServicosListaPage />);

    // Aguarda o carregamento e verifica os nomes dos serviços na tabela.
    expect(await screen.findByText('Lavagem Simples')).toBeInTheDocument();
    expect(screen.getByText('Polimento Completo')).toBeInTheDocument();
  });

  it('exibe preço formatado em BRL e duração em minutos', async () => {
    server.use(
      http.get('/api/v1/servicos', () => HttpResponse.json({ itens: [SERVICOS[0]], total: 1 })),
    );

    renderComProviders(<ServicosListaPage />);

    await screen.findByText('Lavagem Simples');
    // R$ 45,00
    expect(screen.getByText(/R\$\s*45/)).toBeInTheDocument();
    // 40 min
    expect(screen.getByText(/40 min/)).toBeInTheDocument();
  });

  it('exibe badge de status correto (Ativo / Inativo)', async () => {
    server.use(
      http.get('/api/v1/servicos', () =>
        HttpResponse.json({ itens: SERVICOS, total: SERVICOS.length }),
      ),
    );

    renderComProviders(<ServicosListaPage />);

    await screen.findByText('Lavagem Simples');

    const rows = screen.getAllByRole('row');
    // rows[0] = header; rows[1] = Lavagem Simples (ativo); rows[2] = Polimento (inativo)
    expect(within(rows[1]!).getByText(/ativo/i)).toBeInTheDocument();
    expect(within(rows[2]!).getByText(/inativo/i)).toBeInTheDocument();
  });

  it('exibe mensagem de erro quando GET /api/v1/servicos falha (500)', async () => {
    server.use(http.get('/api/v1/servicos', () => HttpResponse.json({}, { status: 500 })));

    renderComProviders(<ServicosListaPage />);

    expect(
      await screen.findByText(/não foi possível carregar a lista de serviços/i),
    ).toBeInTheDocument();
  });

  it('exibe mensagem quando a lista está vazia', async () => {
    server.use(http.get('/api/v1/servicos', () => HttpResponse.json({ itens: [], total: 0 })));

    renderComProviders(<ServicosListaPage />);

    expect(await screen.findByText(/nenhum serviço cadastrado/i)).toBeInTheDocument();
  });

  it('altera o status do serviço via PATCH /api/v1/servicos/:id/status ao confirmar no modal', async () => {
    server.use(
      http.get('/api/v1/servicos', () => HttpResponse.json({ itens: [SERVICOS[0]], total: 1 })),
      http.patch('/api/v1/servicos/:id/status', ({ params }) => {
        const servico = SERVICOS.find((s) => s.id === params.id);
        if (!servico) return HttpResponse.json({}, { status: 404 });
        return HttpResponse.json({ ...servico, ativo: false }, { status: 200 });
      }),
    );

    const user = userEvent.setup();
    renderComProviders(<ServicosListaPage />);

    await screen.findByText('Lavagem Simples');

    // Clica no botão de alteração de status (ícone Power)
    const botaoPower = screen.getByRole('button', { name: /desativar/i });
    await user.click(botaoPower);

    // Confirma no modal
    const btnConfirmar = await screen.findByRole('button', { name: /confirmar/i });
    await user.click(btnConfirmar);

    // A UI deve refletir o novo status sem F5
    await waitFor(() => {
      expect(screen.getByText(/inativo/i)).toBeInTheDocument();
    });
  });
});
