import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { describe, expect, it } from 'vitest';

import { server } from '@/test/mswServer';

import { UsuarioDetalhePage } from './UsuarioDetalhePage';

import type { UsuarioResponse } from '@/types/user';

/**
 * Suíte do detalhe/edição de Usuário interno (RF014), rota /usuarios/:id.
 *
 * Render com rotas reais: a página usa `useParams` (id) e `useNavigate('/usuarios')`
 * no botão Voltar. Montamos a rota /usuarios com marcador para asserir o redirect
 * de Voltar e a entrada inicial em /usuarios/:id para popular o `id`.
 *
 * Paths COM prefixo `/api/v1`: GET /api/v1/usuarios/:id, PUT /api/v1/usuarios/:id,
 * PATCH /api/v1/usuarios/:id/status. baseURL '' nos testes.
 */

const USUARIO: UsuarioResponse = {
  id: 'abc-123',
  nome: 'Carlos Lima',
  email: 'carlos@carwash.com',
  perfil: 'Funcionario',
  ativo: true,
  criadoEm: '2026-01-01T00:00:00.000Z',
  atualizadoEm: '2026-01-01T00:00:00.000Z',
};

function ListaFake() {
  return <h1>Pagina Lista Usuarios</h1>;
}

/** Registra o GET /api/v1/usuarios/:id devolvendo o usuário informado. */
function mockGetById(usuario: UsuarioResponse) {
  server.use(http.get('/api/v1/usuarios/:id', () => HttpResponse.json(usuario)));
}

function renderDetalhe(id = USUARIO.id) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/usuarios/${id}`]}>
        <Routes>
          <Route path="/usuarios/:id" element={<UsuarioDetalhePage />} />
          <Route path="/usuarios" element={<ListaFake />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe('UsuarioDetalhePage (RF014) — carregamento', () => {
  it('11a. exibe "Carregando…" e depois popula nome/email/perfil via getById', async () => {
    mockGetById(USUARIO);
    renderDetalhe();

    expect(screen.getByText('Carregando…')).toBeInTheDocument();

    expect(await screen.findByDisplayValue('Carlos Lima')).toBeInTheDocument();
    expect(screen.getByDisplayValue('carlos@carwash.com')).toBeInTheDocument();
    expect(screen.getByLabelText('PERFIL')).toHaveValue('Funcionario');
    // Badge de status ativo no header.
    expect(screen.getByText('ATIVO')).toBeInTheDocument();
  });

  it('11b. id inexistente/erro exibe "Usuário não encontrado." (role="alert") com botão Voltar', async () => {
    server.use(http.get('/api/v1/usuarios/:id', () => new HttpResponse(null, { status: 404 })));
    const user = userEvent.setup();
    renderDetalhe();

    const alerta = await screen.findByRole('alert');
    expect(alerta).toHaveTextContent('Usuário não encontrado.');

    // Voltar redireciona para /usuarios.
    await user.click(screen.getByRole('button', { name: /voltar/i }));
    expect(await screen.findByText('Pagina Lista Usuarios')).toBeInTheDocument();
  });
});

describe('UsuarioDetalhePage (RF014) — editar e salvar', () => {
  it('12a. editar nome e salvar chama PUT e mostra sucesso (role="status")', async () => {
    mockGetById(USUARIO);
    let putBody: { nome?: string; email?: string; perfil?: string } | null = null;
    server.use(
      http.put('/api/v1/usuarios/:id', async ({ request }) => {
        putBody = (await request.json()) as typeof putBody;
        return HttpResponse.json({ ...USUARIO, nome: putBody!.nome! });
      }),
    );
    const user = userEvent.setup();
    renderDetalhe();
    const nome = await screen.findByDisplayValue('Carlos Lima');

    await user.clear(nome);
    await user.type(nome, 'Carlos Lima Filho');
    await user.click(screen.getByRole('button', { name: /salvar alterações/i }));

    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent('Dados atualizados com sucesso.');
    expect(putBody).toEqual({
      nome: 'Carlos Lima Filho',
      email: 'carlos@carwash.com',
      perfil: 'Funcionario',
    });
  });

  it('12b. erro no PUT exibe alerta de falha ao atualizar', async () => {
    mockGetById(USUARIO);
    server.use(http.put('/api/v1/usuarios/:id', () => new HttpResponse(null, { status: 500 })));
    const user = userEvent.setup();
    renderDetalhe();
    await screen.findByDisplayValue('Carlos Lima');

    await user.click(screen.getByRole('button', { name: /salvar alterações/i }));

    const alerta = await screen.findByRole('alert');
    expect(alerta).toHaveTextContent(
      'Não foi possível atualizar o usuário. Verifique os campos e tente novamente.',
    );
  });

  it('12c. botões ficam disabled durante o salvamento (isBusy)', async () => {
    mockGetById(USUARIO);
    let liberar: (() => void) | undefined;
    const bloqueio = new Promise<void>((resolve) => {
      liberar = resolve;
    });
    server.use(
      http.put('/api/v1/usuarios/:id', async () => {
        await bloqueio;
        return HttpResponse.json(USUARIO);
      }),
    );
    const user = userEvent.setup();
    renderDetalhe();
    await screen.findByDisplayValue('Carlos Lima');

    const salvar = screen.getByRole('button', { name: /salvar alterações/i });
    await user.click(salvar);

    // Enquanto o PUT não resolve: botão Salvar e Switch desabilitados.
    await waitFor(() => expect(screen.getByRole('button', { name: /salvando/i })).toBeDisabled());
    expect(screen.getByRole('switch')).toBeDisabled();

    liberar!();
    await screen.findByRole('status');
  });
});

describe('UsuarioDetalhePage (RF014) — toggle de status', () => {
  it('13a. inativar chama PATCH + refetch e mostra mensagem de inativado', async () => {
    // 1ª leitura: ativo. Após o PATCH, o refetch deve devolver inativo.
    let chamadasGet = 0;
    server.use(
      http.get('/api/v1/usuarios/:id', () => {
        chamadasGet += 1;
        return HttpResponse.json(chamadasGet === 1 ? USUARIO : { ...USUARIO, ativo: false });
      }),
    );
    let patchBody: { ativo?: boolean } | null = null;
    server.use(
      http.patch('/api/v1/usuarios/:id/status', async ({ request }) => {
        patchBody = (await request.json()) as typeof patchBody;
        return HttpResponse.json({
          id: USUARIO.id,
          ativo: false,
          atualizadoEm: '2026-02-01T00:00:00.000Z',
        });
      }),
    );
    const user = userEvent.setup();
    renderDetalhe();
    await screen.findByDisplayValue('Carlos Lima');
    expect(screen.getByText('ATIVO')).toBeInTheDocument();

    await user.click(screen.getByRole('switch', { name: 'Inativar usuário' }));

    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent(
      'Usuário inativado com sucesso. O acesso ao sistema foi bloqueado.',
    );
    expect(patchBody).toEqual({ ativo: false });
    // Refetch refletiu o novo status no badge.
    expect(screen.getByText('INATIVO')).toBeInTheDocument();
  });

  it('13b. ativar (usuário inativo) mostra mensagem de ativado', async () => {
    const inativo = { ...USUARIO, ativo: false };
    let chamadasGet = 0;
    server.use(
      http.get('/api/v1/usuarios/:id', () => {
        chamadasGet += 1;
        return HttpResponse.json(chamadasGet === 1 ? inativo : { ...inativo, ativo: true });
      }),
    );
    server.use(
      http.patch('/api/v1/usuarios/:id/status', () =>
        HttpResponse.json({
          id: USUARIO.id,
          ativo: true,
          atualizadoEm: '2026-02-01T00:00:00.000Z',
        }),
      ),
    );
    const user = userEvent.setup();
    renderDetalhe();
    await screen.findByDisplayValue('Carlos Lima');
    expect(screen.getByText('INATIVO')).toBeInTheDocument();

    await user.click(screen.getByRole('switch', { name: 'Ativar usuário' }));

    const status = await screen.findByRole('status');
    expect(status).toHaveTextContent(
      'Usuário ativado com sucesso. O acesso ao sistema foi restaurado.',
    );
  });

  it('13c. erro no PATCH exibe "Não foi possível alterar o status do usuário."', async () => {
    mockGetById(USUARIO);
    server.use(
      http.patch('/api/v1/usuarios/:id/status', () => new HttpResponse(null, { status: 500 })),
    );
    const user = userEvent.setup();
    renderDetalhe();
    await screen.findByDisplayValue('Carlos Lima');

    await user.click(screen.getByRole('switch', { name: 'Inativar usuário' }));

    const alerta = await screen.findByRole('alert');
    expect(alerta).toHaveTextContent('Não foi possível alterar o status do usuário.');
  });

  it('13d. botão X fecha a mensagem de sucesso', async () => {
    let chamadasGet = 0;
    server.use(
      http.get('/api/v1/usuarios/:id', () => {
        chamadasGet += 1;
        return HttpResponse.json(chamadasGet === 1 ? USUARIO : { ...USUARIO, ativo: false });
      }),
    );
    server.use(
      http.patch('/api/v1/usuarios/:id/status', () =>
        HttpResponse.json({
          id: USUARIO.id,
          ativo: false,
          atualizadoEm: '2026-02-01T00:00:00.000Z',
        }),
      ),
    );
    const user = userEvent.setup();
    renderDetalhe();
    await screen.findByDisplayValue('Carlos Lima');

    await user.click(screen.getByRole('switch', { name: 'Inativar usuário' }));
    await screen.findByRole('status');

    await user.click(screen.getByRole('button', { name: /fechar mensagem de sucesso/i }));
    expect(screen.queryByRole('status')).not.toBeInTheDocument();
  });
});

describe('UsuarioDetalhePage (RF014) — interações adicionais', () => {
  it('12d. editar e-mail e perfil envia os novos valores no PUT', async () => {
    mockGetById(USUARIO);
    let putBody: { nome?: string; email?: string; perfil?: string } | null = null;
    server.use(
      http.put('/api/v1/usuarios/:id', async ({ request }) => {
        putBody = (await request.json()) as typeof putBody;
        return HttpResponse.json({ ...USUARIO, email: putBody!.email!, perfil: 'Admin' });
      }),
    );
    const user = userEvent.setup();
    renderDetalhe();
    const email = await screen.findByDisplayValue('carlos@carwash.com');

    await user.clear(email);
    await user.type(email, 'carlos.lima@carwash.com');
    await user.selectOptions(screen.getByLabelText('PERFIL'), 'Admin');
    await user.click(screen.getByRole('button', { name: /salvar alterações/i }));

    await screen.findByRole('status');
    expect(putBody).toEqual({
      nome: 'Carlos Lima',
      email: 'carlos.lima@carwash.com',
      perfil: 'Admin',
    });
  });

  it('12e. botão Voltar da tela principal redireciona para /usuarios', async () => {
    mockGetById(USUARIO);
    const user = userEvent.setup();
    renderDetalhe();
    await screen.findByDisplayValue('Carlos Lima');

    await user.click(screen.getByRole('button', { name: /voltar/i }));
    expect(await screen.findByText('Pagina Lista Usuarios')).toBeInTheDocument();
  });

  it('12f. botão X fecha a mensagem de erro', async () => {
    mockGetById(USUARIO);
    server.use(http.put('/api/v1/usuarios/:id', () => new HttpResponse(null, { status: 500 })));
    const user = userEvent.setup();
    renderDetalhe();
    await screen.findByDisplayValue('Carlos Lima');

    await user.click(screen.getByRole('button', { name: /salvar alterações/i }));
    await screen.findByRole('alert');

    await user.click(screen.getByRole('button', { name: /fechar mensagem de erro/i }));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
  });
});
