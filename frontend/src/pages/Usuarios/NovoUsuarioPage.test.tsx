import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { server } from '@/test/mswServer';

import { NovoUsuarioPage } from './NovoUsuarioPage';

/**
 * Suíte do formulário de criação de Usuário interno (RF014).
 *
 * Render com rotas reais: a página usa `useNavigate('/dashboard')` (após sucesso,
 * com setTimeout ~2s) e `navigate(-1)`. Para asserir o redirect pelo comportamento
 * real, montamos um marcador na rota /dashboard. O setTimeout do sucesso é
 * controlado com fake timers nos testes que precisam observá-lo.
 *
 * Paths COM prefixo `/api/v1` — POST /api/v1/usuarios. baseURL '' nos testes.
 */

function DashboardFake() {
  return <h1>Pagina Dashboard</h1>;
}

function renderNovo() {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/usuarios/novo']}>
        <Routes>
          <Route path="/usuarios/novo" element={<NovoUsuarioPage />} />
          <Route path="/dashboard" element={<DashboardFake />} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

/** Preenche todos os campos com valores válidos. */
async function preencherValido(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText('Nome completo'), 'Maria Souza');
  await user.type(screen.getByLabelText('E-mail'), 'Maria@CarWash.com');
  await user.type(screen.getByLabelText('Senha inicial'), 'senha1234');
  await user.type(screen.getByLabelText('Confirmar senha'), 'senha1234');
  // Perfil já vem "Funcionario" por default.
}

describe('NovoUsuarioPage (RF014) — validação client-side (onBlur)', () => {
  it('8a. nome vazio e só-espaços disparam mensagens do schema', async () => {
    const user = userEvent.setup();
    renderNovo();

    const nome = screen.getByLabelText('Nome completo');
    // Vazio: foca e desfoca para o onBlur disparar.
    await user.click(nome);
    await user.tab();
    expect(await screen.findByText('Nome é obrigatório.')).toBeInTheDocument();
    expect(nome).toHaveAttribute('aria-invalid', 'true');
    expect(nome).toHaveAttribute('aria-describedby', 'usuario-nome-error');

    // Só espaços: min(1) passa mas o refine de trim falha.
    await user.type(nome, '   ');
    await user.tab();
    expect(await screen.findByText('Nome não pode conter apenas espaços.')).toBeInTheDocument();
  });

  it('8b. e-mail inválido dispara "E-mail inválido."', async () => {
    const user = userEvent.setup();
    renderNovo();

    const email = screen.getByLabelText('E-mail');
    await user.type(email, 'nao-eh-email');
    await user.tab();

    expect(await screen.findByText('E-mail inválido.')).toBeInTheDocument();
    expect(email).toHaveAttribute('aria-invalid', 'true');
  });

  it('8c. senha fraca (curta / sem número / sem letra) cai na mensagem única', async () => {
    const user = userEvent.setup();
    renderNovo();
    const senha = screen.getByLabelText('Senha inicial');

    // Curta.
    await user.type(senha, 'ab1');
    await user.tab();
    expect(await screen.findByText('Senha não atende aos requisitos mínimos.')).toBeInTheDocument();

    // Sem número (8+ letras).
    await user.clear(senha);
    await user.type(senha, 'somenteletras');
    await user.tab();
    expect(await screen.findByText('Senha não atende aos requisitos mínimos.')).toBeInTheDocument();

    // Sem letra (8+ dígitos).
    await user.clear(senha);
    await user.type(senha, '12345678');
    await user.tab();
    expect(await screen.findByText('Senha não atende aos requisitos mínimos.')).toBeInTheDocument();
  });

  it('8d. confirmarSenha divergente dispara "As senhas não coincidem."', async () => {
    const user = userEvent.setup();
    renderNovo();

    await user.type(screen.getByLabelText('Senha inicial'), 'senha1234');
    await user.type(screen.getByLabelText('Confirmar senha'), 'senha9999');
    await user.tab();

    expect(await screen.findByText('As senhas não coincidem.')).toBeInTheDocument();
  });

  it('8e. toggles de senha/confirmação alternam o type e aria-pressed', async () => {
    const user = userEvent.setup();
    renderNovo();

    const senha = screen.getByLabelText('Senha inicial');
    expect(senha).toHaveAttribute('type', 'password');
    const verSenha = screen.getByRole('button', { name: 'Mostrar senha' });
    expect(verSenha).toHaveAttribute('aria-pressed', 'false');

    await user.click(verSenha);
    expect(senha).toHaveAttribute('type', 'text');
    expect(screen.getByRole('button', { name: 'Ocultar senha' })).toHaveAttribute(
      'aria-pressed',
      'true',
    );

    const confirmar = screen.getByLabelText('Confirmar senha');
    expect(confirmar).toHaveAttribute('type', 'password');
    await user.click(screen.getByRole('button', { name: 'Mostrar confirmação' }));
    expect(confirmar).toHaveAttribute('type', 'text');
  });
});

describe('NovoUsuarioPage (RF014) — submit', () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });
  afterEach(() => {
    vi.useRealTimers();
  });

  it('9. submit válido posta payload normalizado, mostra sucesso e redireciona após ~2s', async () => {
    let body: { nome?: string; email?: string; senha?: string; perfil?: string } | null = null;
    server.use(
      http.post('/api/v1/usuarios', async ({ request }) => {
        body = (await request.json()) as typeof body;
        return HttpResponse.json(
          {
            id: 'novo-1',
            nome: body!.nome,
            email: body!.email,
            perfil: body!.perfil,
            ativo: true,
            criadoEm: '2026-01-01T00:00:00.000Z',
            atualizadoEm: '2026-01-01T00:00:00.000Z',
          },
          { status: 201 },
        );
      }),
    );
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderNovo();

    await preencherValido(user);
    await user.click(screen.getByRole('button', { name: /salvar usuário/i }));

    // Mensagem de sucesso.
    expect(
      await screen.findByText('Usuário cadastrado com sucesso! Redirecionando...'),
    ).toBeInTheDocument();

    // Payload normalizado: nome.trim(), email lower/trim, senha intacta, perfil.
    await waitFor(() => expect(body).not.toBeNull());
    expect(body).toEqual({
      nome: 'Maria Souza',
      email: 'maria@carwash.com',
      senha: 'senha1234',
      perfil: 'Funcionario',
    });

    // Avança o timer de 2s do redirect.
    await import('@testing-library/react').then(({ act }) =>
      act(async () => {
        await vi.advanceTimersByTimeAsync(2000);
      }),
    );
    expect(await screen.findByText('Pagina Dashboard')).toBeInTheDocument();
  });

  it('10a. 409 exibe globalError (title do backend), erro no campo e-mail e mantém na página', async () => {
    server.use(
      http.post('/api/v1/usuarios', () =>
        HttpResponse.json(
          { title: 'E-mail já cadastrado no sistema.', status: 409 },
          { status: 409 },
        ),
      ),
    );
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderNovo();

    await preencherValido(user);
    await user.click(screen.getByRole('button', { name: /salvar usuário/i }));

    expect(await screen.findByText('E-mail já cadastrado no sistema.')).toBeInTheDocument();
    expect(screen.getByText('Já existe usuário com este e-mail.')).toBeInTheDocument();
    // Sem redirect.
    expect(screen.queryByText('Pagina Dashboard')).not.toBeInTheDocument();
  });

  it('10b. 409 sem title usa a mensagem padrão do mapa', async () => {
    server.use(http.post('/api/v1/usuarios', () => new HttpResponse(null, { status: 409 })));
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderNovo();

    await preencherValido(user);
    await user.click(screen.getByRole('button', { name: /salvar usuário/i }));

    expect(
      await screen.findByText('Já existe usuário cadastrado com este e-mail.'),
    ).toBeInTheDocument();
  });

  it('10c. 400 com errors (PascalCase) mapeia para os campos do form + globalError', async () => {
    server.use(
      http.post('/api/v1/usuarios', () =>
        HttpResponse.json(
          {
            title: 'Validação falhou.',
            errors: {
              Nome: ['Nome reservado pelo sistema.'],
              Email: ['Domínio não permitido.'],
            },
          },
          { status: 400 },
        ),
      ),
    );
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderNovo();

    await preencherValido(user);
    await user.click(screen.getByRole('button', { name: /salvar usuário/i }));

    expect(await screen.findByText('Validação falhou.')).toBeInTheDocument();
    expect(screen.getByText('Nome reservado pelo sistema.')).toBeInTheDocument();
    expect(screen.getByText('Domínio não permitido.')).toBeInTheDocument();
  });

  it('10d. 500 exibe a mensagem do mapa de erros HTTP', async () => {
    server.use(http.post('/api/v1/usuarios', () => new HttpResponse(null, { status: 500 })));
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderNovo();

    await preencherValido(user);
    await user.click(screen.getByRole('button', { name: /salvar usuário/i }));

    expect(
      await screen.findByText('Não foi possível criar o usuário agora. Tente novamente.'),
    ).toBeInTheDocument();
  });

  it('10e. erro de rede exibe "Não foi possível contatar o servidor..."', async () => {
    server.use(http.post('/api/v1/usuarios', () => HttpResponse.error()));
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderNovo();

    await preencherValido(user);
    await user.click(screen.getByRole('button', { name: /salvar usuário/i }));

    expect(
      await screen.findByText('Não foi possível contatar o servidor. Verifique sua conexão.'),
    ).toBeInTheDocument();
  });

  it('10f. botão X fecha o globalError', async () => {
    server.use(http.post('/api/v1/usuarios', () => new HttpResponse(null, { status: 500 })));
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderNovo();

    await preencherValido(user);
    await user.click(screen.getByRole('button', { name: /salvar usuário/i }));
    await screen.findByText('Não foi possível criar o usuário agora. Tente novamente.');

    await user.click(screen.getByRole('button', { name: /fechar mensagem de erro/i }));
    expect(
      screen.queryByText('Não foi possível criar o usuário agora. Tente novamente.'),
    ).not.toBeInTheDocument();
  });
});
