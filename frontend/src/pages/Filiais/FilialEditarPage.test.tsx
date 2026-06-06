import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { Route, Routes } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';

import { FilialEditarPage } from '@/pages/Filiais/FilialEditarPage';
import { server } from '@/test/mswServer';
import { renderComProviders } from '@/test/renderComProviders';

/**
 * Testes de integração da edição de filial (RF018).
 *
 * O backend só permite ajustar a quantidade de células ativas
 * (`PATCH /api/v1/filiais/{id}/celulas-ativas`); o status é somente leitura.
 */

const FILIAL = {
  id: 'f0000001-0000-4000-8000-000000000001',
  nome: 'Unidade Centro',
  celulasAtivas: 4,
  timezone: 'America/Sao_Paulo',
  ativa: true,
  criadoEm: '2026-01-01T00:00:00.000Z',
  atualizadoEm: '2026-01-01T00:00:00.000Z',
};

/** Renderiza a página com a rota `/filiais/:id/editar` resolvida. */
function renderEditar(id = FILIAL.id) {
  window.history.pushState({}, '', `/filiais/${id}/editar`);
  return renderComProviders(
    <Routes>
      <Route path="/filiais/:id/editar" element={<FilialEditarPage />} />
    </Routes>,
  );
}

describe('FilialEditarPage (RF018)', () => {
  it('carrega a filial e pré-preenche as células ativas', async () => {
    server.use(http.get('/api/v1/filiais/:id', () => HttpResponse.json(FILIAL)));

    renderEditar();

    expect(await screen.findByText('Unidade Centro')).toBeInTheDocument();
    const campo = await screen.findByLabelText(/células ativas/i);
    expect(campo).toHaveValue(4);
    // Status exibido somente leitura (sem botão de ativar/desativar).
    expect(screen.getByText(/^ativa$/i)).toBeInTheDocument();
  });

  it('envia o novo valor via PATCH /celulas-ativas e exibe sucesso', async () => {
    const recebido = vi.fn();
    server.use(
      http.get('/api/v1/filiais/:id', () => HttpResponse.json(FILIAL)),
      http.patch('/api/v1/filiais/:id/celulas-ativas', async ({ request }) => {
        recebido(await request.json());
        return HttpResponse.json({ ...FILIAL, celulasAtivas: 8 });
      }),
    );

    const user = userEvent.setup();
    renderEditar();

    const campo = await screen.findByLabelText(/células ativas/i);
    await user.clear(campo);
    await user.type(campo, '8');
    await user.click(screen.getByRole('button', { name: /salvar alterações/i }));

    expect(await screen.findByText(/filial atualizada com sucesso/i)).toBeInTheDocument();
    expect(recebido).toHaveBeenCalledWith({ celulasAtivas: 8 });
  });

  it('exibe a mensagem do backend ao receber 400 (faixa inválida)', async () => {
    server.use(
      http.get('/api/v1/filiais/:id', () => HttpResponse.json(FILIAL)),
      http.patch('/api/v1/filiais/:id/celulas-ativas', () =>
        HttpResponse.json(
          {
            title: 'Erro de validação.',
            errors: {
              celulasAtivas: [
                'Valor de células ativas inválido. Informe um número inteiro entre 1 e 100.',
              ],
            },
          },
          { status: 400 },
        ),
      ),
    );

    const user = userEvent.setup();
    renderEditar();

    const campo = await screen.findByLabelText(/células ativas/i);
    await user.clear(campo);
    await user.type(campo, '50');
    await user.click(screen.getByRole('button', { name: /salvar alterações/i }));

    await waitFor(() => {
      expect(screen.getByText(/valor de células ativas inválido/i)).toBeInTheDocument();
    });
  });

  it('exibe erro de carga quando a filial não existe (404)', async () => {
    server.use(
      http.get('/api/v1/filiais/:id', () =>
        HttpResponse.json({ title: 'Filial não encontrada.' }, { status: 404 }),
      ),
    );

    renderEditar();

    expect(await screen.findByText(/filial não encontrada/i)).toBeInTheDocument();
  });
});
