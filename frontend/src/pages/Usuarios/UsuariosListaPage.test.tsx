import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';

import { server } from '@/test/mswServer';

import { UsuariosListaPage } from './UsuariosListaPage';

import type { UsuarioResponse } from '@/types/user';

/**
 * Suíte da listagem de Usuários internos (RF014).
 *
 * Estratégia de render: a página usa `useNavigate` (/usuarios/novo) e `<Link>`
 * (/usuarios/:id). Para asserir os REDIRECTS pelo comportamento real (sem mockar
 * o router), montamos rotas reais com marcadores nas rotas-alvo.
 *
 * Paths COM prefixo `/api/v1` — o `userService` usa os caminhos corretos; os
 * handlers MSW abaixo replicam EXATAMENTE: GET/POST/PUT/PATCH em
 * `/api/v1/usuarios[...]`. baseURL nos testes é '' (VITE_API_URL vazio).
 */

const ADMIN: UsuarioResponse = {
  id: '11111111-1111-4111-8111-111111111111',
  nome: 'Ana Admin',
  email: 'ana@carwash.com',
  perfil: 'Admin',
  ativo: true,
  criadoEm: '2026-01-01T00:00:00.000Z',
  atualizadoEm: '2026-01-01T00:00:00.000Z',
};

const FUNC_INATIVO: UsuarioResponse = {
  id: '22222222-2222-4222-8222-222222222222',
  nome: 'Bruno Func',
  email: 'bruno@carwash.com',
  perfil: 'Funcionario',
  ativo: false,
  criadoEm: '2026-01-02T00:00:00.000Z',
  atualizadoEm: '2026-01-02T00:00:00.000Z',
};

/** Captura os query params recebidos pelo GET /api/v1/usuarios em cada chamada. */
interface ListaCall {
  busca: string | null;
  ativo: string | null;
  pagina: string | null;
  tamanhoPagina: string | null;
}

/**
 * Registra o handler de listagem. `responder` decide a resposta por página/params
 * e a cada chamada empurra os params recebidos em `calls`.
 */
function mockListar(
  responder: (params: ListaCall) => { itens: UsuarioResponse[]; total: number },
  calls?: ListaCall[],
) {
  server.use(
    http.get('/api/v1/usuarios', ({ request }) => {
      const sp = new URL(request.url).searchParams;
      const call: ListaCall = {
        busca: sp.get('busca'),
        ativo: sp.get('ativo'),
        pagina: sp.get('pagina'),
        tamanhoPagina: sp.get('tamanhoPagina'),
      };
      calls?.push(call);
      const { itens, total } = responder(call);
      return HttpResponse.json({
        itens,
        total,
        pagina: Number(call.pagina ?? 1),
        tamanhoPagina: 20,
      });
    }),
  );
}

/** Marcador da rota-alvo de "Novo usuário". */
function NovoFake() {
  return <h1>Pagina Novo Usuario</h1>;
}
/** Marcador da rota-alvo de detalhe (captura o :id na URL via texto). */
function DetalheFake() {
  return <h1>Pagina Detalhe Usuario</h1>;
}

function renderLista() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/usuarios']}>
        <Routes>
          <Route path="/usuarios" element={<UsuariosListaPage />} />
          <Route path="/usuarios/novo" element={<NovoFake />} />
          <Route path="/usuarios/:id" element={<DetalheFake />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('UsuariosListaPage (RF014) — carregamento e render', () => {
  it('1. exibe "Carregando…" e depois as linhas (nome como link, email, perfil, badge) + total no cabeçalho', async () => {
    mockListar(() => ({ itens: [ADMIN, FUNC_INATIVO], total: 2 }));
    renderLista();

    // Loading inicial enquanto o GET não resolve.
    expect(screen.getByText('Carregando…')).toBeInTheDocument();

    // Linhas renderizadas.
    expect(await screen.findByRole('link', { name: 'Ana Admin' })).toBeInTheDocument();
    expect(screen.getByText('ana@carwash.com')).toBeInTheDocument();
    expect(screen.getByText('Admin')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Bruno Func' })).toBeInTheDocument();
    expect(screen.getByText('Funcionario')).toBeInTheDocument();
    // Badges de status.
    expect(screen.getByText('ATIVO')).toBeInTheDocument();
    expect(screen.getByText('INATIVO')).toBeInTheDocument();
    // Cabeçalho com total.
    expect(screen.getByText('2 usuário(s) no total')).toBeInTheDocument();
  });

  it('2. lista vazia exibe "Nenhum usuário encontrado." e cabeçalho "Nenhum usuário cadastrado"', async () => {
    mockListar(() => ({ itens: [], total: 0 }));
    renderLista();

    expect(await screen.findByText('Nenhum usuário encontrado.')).toBeInTheDocument();
    expect(screen.getByText('Nenhum usuário cadastrado')).toBeInTheDocument();
  });

  it('3. erro 500 no carregamento exibe o alerta de falha (role="alert")', async () => {
    server.use(http.get('/api/v1/usuarios', () => new HttpResponse(null, { status: 500 })));
    renderLista();

    const alerta = await screen.findByRole('alert');
    expect(alerta).toHaveTextContent('Não foi possível carregar a lista de usuários.');
  });
});

describe('UsuariosListaPage (RF014) — busca e filtro de status', () => {
  it('4a. digitar na busca reseta a página para 1 e envia o param `busca`', async () => {
    const calls: ListaCall[] = [];
    mockListar(() => ({ itens: [ADMIN], total: 1 }), calls);
    const user = userEvent.setup();
    renderLista();
    await screen.findByRole('link', { name: 'Ana Admin' });

    await user.type(screen.getByPlaceholderText(/buscar por nome/i), 'ana');

    // A última chamada deve conter o termo e pagina=1.
    await waitFor(() => {
      const ultima = calls.at(-1)!;
      expect(ultima.busca).toBe('ana');
      expect(ultima.pagina).toBe('1');
    });
  });

  it('4b. filtro "Ativos" envia ativo=true e "Inativos" envia ativo=false; "Todos" omite o param', async () => {
    const calls: ListaCall[] = [];
    mockListar(() => ({ itens: [ADMIN], total: 1 }), calls);
    const user = userEvent.setup();
    renderLista();
    await screen.findByRole('link', { name: 'Ana Admin' });

    // Default (Todos): sem param `ativo`.
    expect(calls.at(-1)!.ativo).toBeNull();

    await user.click(screen.getByRole('button', { name: 'Ativos' }));
    await waitFor(() => expect(calls.at(-1)!.ativo).toBe('true'));
    expect(calls.at(-1)!.pagina).toBe('1');

    await user.click(screen.getByRole('button', { name: 'Inativos' }));
    await waitFor(() => expect(calls.at(-1)!.ativo).toBe('false'));

    await user.click(screen.getByRole('button', { name: 'Todos' }));
    await waitFor(() => expect(calls.at(-1)!.ativo).toBeNull());
  });
});

describe('UsuariosListaPage (RF014) — paginação', () => {
  it('5. Anterior desabilitado na pág. 1; Próxima navega para pág. 2 (param `pagina`)', async () => {
    const calls: ListaCall[] = [];
    // 40 itens => 2 páginas (TAMANHO_PAGINA=20).
    mockListar((p) => {
      const pagina = Number(p.pagina ?? 1);
      const usuario = pagina === 1 ? ADMIN : FUNC_INATIVO;
      return { itens: [usuario], total: 40 };
    }, calls);
    const user = userEvent.setup();
    renderLista();
    await screen.findByRole('link', { name: 'Ana Admin' });

    expect(screen.getByText('Página 1 de 2')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /anterior/i })).toBeDisabled();
    expect(screen.getByRole('button', { name: /próxima/i })).toBeEnabled();

    await user.click(screen.getByRole('button', { name: /próxima/i }));

    // Refaz a query com pagina=2 e renderiza o item da página 2.
    expect(await screen.findByRole('link', { name: 'Bruno Func' })).toBeInTheDocument();
    await waitFor(() => expect(calls.at(-1)!.pagina).toBe('2'));
    expect(screen.getByText('Página 2 de 2')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /anterior/i })).toBeEnabled();
    expect(screen.getByRole('button', { name: /próxima/i })).toBeDisabled();
  });
});

describe('UsuariosListaPage (RF014) — switch inline de status', () => {
  it('6a. alterna o status via PATCH e atualiza o badge da linha', async () => {
    mockListar(() => ({ itens: [ADMIN], total: 1 }));
    let patchId: string | null = null;
    let patchBody: { ativo?: boolean } | null = null;
    server.use(
      http.patch('/api/v1/usuarios/:id/status', async ({ request, params }) => {
        patchId = String(params.id);
        patchBody = (await request.json()) as { ativo?: boolean };
        return HttpResponse.json({
          id: patchId,
          ativo: false,
          atualizadoEm: '2026-01-03T00:00:00.000Z',
        });
      }),
    );
    const user = userEvent.setup();
    renderLista();
    await screen.findByRole('link', { name: 'Ana Admin' });

    expect(screen.getByText('ATIVO')).toBeInTheDocument();

    // Switch da linha do admin (ativo => aria-label "Inativar Ana Admin").
    await user.click(screen.getByRole('switch', { name: 'Inativar Ana Admin' }));

    // Badge atualiza para INATIVO (item local atualizado).
    expect(await screen.findByText('INATIVO')).toBeInTheDocument();
    expect(screen.queryByText('ATIVO')).not.toBeInTheDocument();
    expect(patchId).toBe(ADMIN.id);
    expect(patchBody).toEqual({ ativo: false });
  });

  it('6b. erro no PATCH exibe alerta com o nome do usuário e mantém o status', async () => {
    mockListar(() => ({ itens: [ADMIN], total: 1 }));
    server.use(
      http.patch('/api/v1/usuarios/:id/status', () => new HttpResponse(null, { status: 500 })),
    );
    const user = userEvent.setup();
    renderLista();
    await screen.findByRole('link', { name: 'Ana Admin' });

    await user.click(screen.getByRole('switch', { name: 'Inativar Ana Admin' }));

    const alerta = await screen.findByRole('alert');
    expect(alerta).toHaveTextContent('Não foi possível inativar o usuário "Ana Admin".');
    // Status NÃO muda no erro.
    expect(screen.getByText('ATIVO')).toBeInTheDocument();
  });

  it('6c. switch fica disabled durante a chamada (alterandoStatusId)', async () => {
    mockListar(() => ({ itens: [ADMIN], total: 1 }));
    // PATCH pendurado até liberarmos — para observar o estado disabled.
    let liberar: (() => void) | undefined;
    const bloqueio = new Promise<void>((resolve) => {
      liberar = resolve;
    });
    server.use(
      http.patch('/api/v1/usuarios/:id/status', async () => {
        await bloqueio;
        return HttpResponse.json({
          id: ADMIN.id,
          ativo: false,
          atualizadoEm: '2026-01-03T00:00:00.000Z',
        });
      }),
    );
    const user = userEvent.setup();
    renderLista();
    await screen.findByRole('link', { name: 'Ana Admin' });

    const sw = screen.getByRole('switch', { name: 'Inativar Ana Admin' });
    await user.click(sw);

    // Enquanto o PATCH não resolve, o switch fica desabilitado.
    await waitFor(() => expect(screen.getByRole('switch')).toBeDisabled());

    liberar!();
    // Após resolver, badge muda e o switch reabilita.
    expect(await screen.findByText('INATIVO')).toBeInTheDocument();
    await waitFor(() => expect(screen.getByRole('switch')).toBeEnabled());
  });
});

describe('UsuariosListaPage (RF014) — navegação', () => {
  it('7a. "Novo usuário" navega para /usuarios/novo', async () => {
    mockListar(() => ({ itens: [ADMIN], total: 1 }));
    const user = userEvent.setup();
    renderLista();
    await screen.findByRole('link', { name: 'Ana Admin' });

    await user.click(screen.getByRole('button', { name: /novo usuário/i }));

    expect(await screen.findByText('Pagina Novo Usuario')).toBeInTheDocument();
  });

  it('7b. clicar no nome (Link) navega para /usuarios/:id', async () => {
    mockListar(() => ({ itens: [ADMIN], total: 1 }));
    const user = userEvent.setup();
    renderLista();

    const link = await screen.findByRole('link', { name: 'Ana Admin' });
    // Confere o href do Link e exercita a navegação real.
    expect(link).toHaveAttribute('href', `/usuarios/${ADMIN.id}`);
    await user.click(link);

    expect(await screen.findByText('Pagina Detalhe Usuario')).toBeInTheDocument();
  });
});
