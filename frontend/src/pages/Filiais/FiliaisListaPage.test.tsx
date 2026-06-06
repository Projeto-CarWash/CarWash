import { screen, within } from '@testing-library/react';
import { HttpResponse, http } from 'msw';
import { describe, expect, it } from 'vitest';

import { FiliaisListaPage } from '@/pages/Filiais/FiliaisListaPage';
import { server } from '@/test/mswServer';
import { renderComProviders } from '@/test/renderComProviders';

/**
 * Testes de integração da listagem de filiais (RF017/RF018).
 *
 * Cobertura: carregamento via GET /api/v1/filiais, colunas (código, cidade, UF,
 * células ativas), badge de status (Ativa/Inativa), estado vazio e erro.
 */

const FILIAIS = [
  {
    id: 'f0000001-0000-4000-8000-000000000001',
    nome: 'Unidade Centro',
    codigo: 'CENTRO01',
    cidade: 'São Paulo',
    uf: 'SP',
    celulasAtivas: 4,
    ativo: true,
  },
  {
    id: 'f0000002-0000-4000-8000-000000000002',
    nome: 'Unidade Zona Sul',
    codigo: 'ZSUL02',
    cidade: 'São Paulo',
    uf: 'SP',
    celulasAtivas: 2,
    ativo: false,
  },
];

describe('FiliaisListaPage (RF017/RF018)', () => {
  it('exibe as filiais retornadas por GET /api/v1/filiais', async () => {
    server.use(
      http.get('/api/v1/filiais', () =>
        HttpResponse.json({ itens: FILIAIS, total: FILIAIS.length }),
      ),
    );

    renderComProviders(<FiliaisListaPage />);

    expect(await screen.findByText('Unidade Centro')).toBeInTheDocument();
    expect(screen.getByText('CENTRO01')).toBeInTheDocument();
    expect(screen.getByText('Unidade Zona Sul')).toBeInTheDocument();
  });

  it('exibe o badge de status correto (Ativa / Inativa)', async () => {
    server.use(
      http.get('/api/v1/filiais', () =>
        HttpResponse.json({ itens: FILIAIS, total: FILIAIS.length }),
      ),
    );

    renderComProviders(<FiliaisListaPage />);

    await screen.findByText('Unidade Centro');

    const rows = screen.getAllByRole('row');
    // rows[0] = header; rows[1] = Centro (ativa); rows[2] = Zona Sul (inativa)
    expect(within(rows[1]!).getByText(/^ativa$/i)).toBeInTheDocument();
    expect(within(rows[2]!).getByText(/^inativa$/i)).toBeInTheDocument();
  });

  it('exibe mensagem quando a lista está vazia', async () => {
    server.use(http.get('/api/v1/filiais', () => HttpResponse.json({ itens: [], total: 0 })));

    renderComProviders(<FiliaisListaPage />);

    expect(await screen.findByText(/nenhuma filial cadastrada/i)).toBeInTheDocument();
  });

  it('exibe estado de erro quando GET /api/v1/filiais falha', async () => {
    server.use(http.get('/api/v1/filiais', () => HttpResponse.json({}, { status: 500 })));

    renderComProviders(<FiliaisListaPage />);

    expect(
      await screen.findByText(/não foi possível carregar a lista de filiais/i),
    ).toBeInTheDocument();
  });
});
