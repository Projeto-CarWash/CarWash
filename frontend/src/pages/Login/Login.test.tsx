import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { HttpResponse, http } from 'msw';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';

import { AuthProvider } from '@/contexts/AuthProvider';
import { accessTokenStore } from '@/services/accessTokenStore';
import { server } from '@/test/mswServer';

import Login from './Login';

import type { LoginResponse } from '@/types/auth';
import type { ReactElement } from 'react';

/**
 * Suíte de testes da tela de Login (RF001).
 *
 * Estratégia de render: o Login depende de `useAuth()` (AuthProvider) e de
 * `useNavigate()` (Router). Usamos um helper que envolve com
 * QueryClientProvider + AuthProvider + MemoryRouter, montando rotas reais
 * (/login e /dashboard). Assim asserimos o REDIRECT pelo comportamento real:
 * quando o navigate('/dashboard') dispara, o conteúdo da página fake de
 * dashboard aparece — sem mockar useNavigate.
 *
 * O AuthProvider chama authService.refresh() no mount; o handler default de
 * /api/v1/auth/refresh (handlers.ts) devolve 401 = "sem sessão". Testes de
 * sessão restaurada sobrescrevem esse handler via server.use(...).
 */

const STORAGE_REMEMBER_EMAIL = 'carwash_remember_email';
const EMAIL_VALIDO = 'admin@carwash.local';
const SENHA_VALIDA = 'Senha@123';

/** LoginResponse de sucesso reaproveitável nos handlers de login/refresh. */
const loginResponseFixture: LoginResponse = {
  accessToken: 'jwt-access-token-fake',
  expiresAt: '2099-01-01T00:00:00.000Z',
  usuario: {
    id: '00000000-0000-4000-8000-000000000001',
    nome: 'Administrador',
    email: EMAIL_VALIDO,
    perfil: 'Admin',
  },
};

/** Marcador visível da rota /dashboard para asserir o redirect real. */
function DashboardFake() {
  return <h1>Painel do Dashboard</h1>;
}

/**
 * Renderiza o Login com todos os providers e rotas reais. Importação dinâmica
 * do AuthProvider mantida estática (top do arquivo) para simplicidade.
 */
function renderLogin(ui: ReactElement = <Login />) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  return render(
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <MemoryRouter initialEntries={['/login']}>
          <Routes>
            <Route path="/login" element={ui} />
            <Route path="/dashboard" element={<DashboardFake />} />
          </Routes>
        </MemoryRouter>
      </AuthProvider>
    </QueryClientProvider>,
  );
}

/** Preenche o formulário com credenciais válidas (sem submeter). */
async function preencherCredenciaisValidas(user: ReturnType<typeof userEvent.setup>) {
  await user.type(screen.getByLabelText(/^e-mail$/i), EMAIL_VALIDO);
  await user.type(screen.getByLabelText(/^senha$/i), SENHA_VALIDA);
}

beforeEach(() => {
  // Isolamento: token em memória e localStorage limpos antes de cada teste.
  accessTokenStore.clear();
  localStorage.clear();
});

afterEach(() => {
  accessTokenStore.clear();
  localStorage.clear();
});

describe('Login (RF001) — caminho feliz', () => {
  it('1. renderiza e-mail, senha, checkbox "Lembrar" e botão "Entrar"', () => {
    renderLogin();

    expect(screen.getByLabelText(/^e-mail$/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/^senha$/i)).toBeInTheDocument();
    expect(screen.getByRole('checkbox', { name: /lembrar meu e-mail/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /^entrar$/i })).toBeInTheDocument();
  });

  it('2. submit com credenciais válidas chama login e redireciona para /dashboard', async () => {
    server.use(http.post('/api/v1/auth/login', () => HttpResponse.json(loginResponseFixture)));
    const user = userEvent.setup();
    renderLogin();

    await preencherCredenciaisValidas(user);
    await user.click(screen.getByRole('button', { name: /^entrar$/i }));

    // Redirect real: a rota /dashboard renderiza seu marcador.
    expect(await screen.findByText('Painel do Dashboard')).toBeInTheDocument();
    // Token persistido em memória pelo authService.login.
    expect(accessTokenStore.get()).toBe(loginResponseFixture.accessToken);
  });

  it('3. normaliza o e-mail (.trim().toLowerCase()) no payload enviado', async () => {
    let payloadRecebido: { email: string; senha: string } | null = null;
    server.use(
      http.post('/api/v1/auth/login', async ({ request }) => {
        payloadRecebido = (await request.json()) as { email: string; senha: string };
        return HttpResponse.json(loginResponseFixture);
      }),
    );
    const user = userEvent.setup();
    renderLogin();

    await user.type(screen.getByLabelText(/^e-mail$/i), '  ADMIN@CarWash.Local  ');
    await user.type(screen.getByLabelText(/^senha$/i), SENHA_VALIDA);
    await user.click(screen.getByRole('button', { name: /^entrar$/i }));

    await screen.findByText('Painel do Dashboard');
    expect(payloadRecebido).toEqual({ email: EMAIL_VALIDO, senha: SENHA_VALIDA });
  });

  it('4a. "Lembrar" marcado grava o e-mail normalizado no localStorage', async () => {
    server.use(http.post('/api/v1/auth/login', () => HttpResponse.json(loginResponseFixture)));
    const user = userEvent.setup();
    renderLogin();

    await user.type(screen.getByLabelText(/^e-mail$/i), '  ADMIN@CarWash.Local  ');
    await user.type(screen.getByLabelText(/^senha$/i), SENHA_VALIDA);
    await user.click(screen.getByRole('checkbox', { name: /lembrar meu e-mail/i }));
    await user.click(screen.getByRole('button', { name: /^entrar$/i }));

    await screen.findByText('Painel do Dashboard');
    expect(localStorage.getItem(STORAGE_REMEMBER_EMAIL)).toBe(EMAIL_VALIDO);
  });

  it('4b. "Lembrar" desmarcado remove a chave do localStorage', async () => {
    // Pré-condição: chave existente que deve ser removida no submit sem lembrar.
    localStorage.setItem(STORAGE_REMEMBER_EMAIL, 'antigo@carwash.local');
    server.use(http.post('/api/v1/auth/login', () => HttpResponse.json(loginResponseFixture)));
    const user = userEvent.setup();
    renderLogin();

    // Com a chave preexistente, o checkbox monta marcado: desmarcamos.
    const checkbox = screen.getByRole('checkbox', { name: /lembrar meu e-mail/i });
    expect(checkbox).toBeChecked();
    await user.click(checkbox);

    // O e-mail já vem pré-preenchido; só precisamos da senha.
    await user.type(screen.getByLabelText(/^senha$/i), SENHA_VALIDA);
    await user.click(screen.getByRole('button', { name: /^entrar$/i }));

    await screen.findByText('Painel do Dashboard');
    expect(localStorage.getItem(STORAGE_REMEMBER_EMAIL)).toBeNull();
  });

  it('4c. no mount, e-mail lembrado pré-preenche o campo e marca o checkbox', () => {
    localStorage.setItem(STORAGE_REMEMBER_EMAIL, 'lembrado@carwash.local');
    renderLogin();

    expect(screen.getByLabelText(/^e-mail$/i)).toHaveValue('lembrado@carwash.local');
    expect(screen.getByRole('checkbox', { name: /lembrar meu e-mail/i })).toBeChecked();
  });

  it('5. toggle de senha alterna type password/text e aria-pressed', async () => {
    const user = userEvent.setup();
    renderLogin();

    const senha = screen.getByLabelText(/^senha$/i);
    expect(senha).toHaveAttribute('type', 'password');

    const toggle = screen.getByRole('button', { name: /mostrar senha/i });
    expect(toggle).toHaveAttribute('aria-pressed', 'false');

    await user.click(toggle);
    expect(senha).toHaveAttribute('type', 'text');
    const toggleAtivo = screen.getByRole('button', { name: /ocultar senha/i });
    expect(toggleAtivo).toHaveAttribute('aria-pressed', 'true');

    await user.click(toggleAtivo);
    expect(screen.getByLabelText(/^senha$/i)).toHaveAttribute('type', 'password');
  });

  it('6. sessão restaurada (refresh 200 no mount) redireciona direto sem submit', async () => {
    // Sobrescreve o refresh default para devolver sessão válida.
    server.use(http.post('/api/v1/auth/refresh', () => HttpResponse.json(loginResponseFixture)));
    renderLogin();

    expect(await screen.findByText('Painel do Dashboard')).toBeInTheDocument();
    // Não houve interação de submit; o redirect veio do effect de sessão.
    expect(screen.queryByRole('button', { name: /^entrar$/i })).not.toBeInTheDocument();
  });
});

describe('Login (RF001) — casos infelizes (validação client-side)', () => {
  it('7a. e-mail vazio (onBlur) exibe "E-mail é obrigatório." com aria-invalid/describedby', async () => {
    const user = userEvent.setup();
    renderLogin();

    const email = screen.getByLabelText(/^e-mail$/i);
    await user.click(email);
    await user.tab(); // dispara onBlur sem digitar

    expect(await screen.findByText('E-mail é obrigatório.')).toBeInTheDocument();
    expect(email).toHaveAttribute('aria-invalid', 'true');
    expect(email).toHaveAttribute('aria-describedby', 'login-email-error');
  });

  it('7b. e-mail inválido (onBlur) exibe "Informe um e-mail válido."', async () => {
    const user = userEvent.setup();
    renderLogin();

    const email = screen.getByLabelText(/^e-mail$/i);
    await user.type(email, 'nao-eh-email');
    await user.tab();

    expect(await screen.findByText('Informe um e-mail válido.')).toBeInTheDocument();
    expect(email).toHaveAttribute('aria-invalid', 'true');
  });

  it('7c. senha vazia (onBlur) exibe "Senha é obrigatória." com aria-invalid/describedby', async () => {
    const user = userEvent.setup();
    renderLogin();

    const senha = screen.getByLabelText(/^senha$/i);
    await user.click(senha);
    await user.tab();

    expect(await screen.findByText('Senha é obrigatória.')).toBeInTheDocument();
    expect(senha).toHaveAttribute('aria-invalid', 'true');
    expect(senha).toHaveAttribute('aria-describedby', 'login-senha-error');
  });
});

describe('Login (RF001) — casos infelizes (erros do backend)', () => {
  /** Submete credenciais válidas após registrar um handler de erro. */
  async function submeterComErro(status: number, body?: unknown) {
    server.use(
      http.post('/api/v1/auth/login', () =>
        body === undefined
          ? new HttpResponse(null, { status })
          : HttpResponse.json(body, { status }),
      ),
    );
    const user = userEvent.setup();
    renderLogin();
    await preencherCredenciaisValidas(user);
    await user.click(screen.getByRole('button', { name: /^entrar$/i }));
    return user;
  }

  it('8. 401 sem title exibe "Usuário ou senha inválidos." no alerta global', async () => {
    await submeterComErro(401);

    const alerta = await screen.findByRole('alert');
    expect(within(alerta).getByText('Usuário ou senha inválidos.')).toBeInTheDocument();
  });

  it('8b. 401 com title no corpo usa o title do ProblemDetails (status != 500)', async () => {
    await submeterComErro(401, { title: 'Conta temporariamente bloqueada.', status: 401 });

    const alerta = await screen.findByRole('alert');
    expect(within(alerta).getByText('Conta temporariamente bloqueada.')).toBeInTheDocument();
  });

  it('9a. 403 exibe "Acesso bloqueado. Usuário inativo."', async () => {
    await submeterComErro(403);

    const alerta = await screen.findByRole('alert');
    expect(within(alerta).getByText('Acesso bloqueado. Usuário inativo.')).toBeInTheDocument();
  });

  it('9b. 400 exibe "E-mail e senha são obrigatórios."', async () => {
    await submeterComErro(400);

    const alerta = await screen.findByRole('alert');
    expect(within(alerta).getByText('E-mail e senha são obrigatórios.')).toBeInTheDocument();
  });

  it('9c. 500 ignora o title do backend e usa a mensagem genérica', async () => {
    await submeterComErro(500, { title: 'Stacktrace vazado!', status: 500 });

    const alerta = await screen.findByRole('alert');
    expect(
      within(alerta).getByText('Não foi possível autenticar agora. Tente novamente em instantes.'),
    ).toBeInTheDocument();
    expect(within(alerta).queryByText('Stacktrace vazado!')).not.toBeInTheDocument();
  });

  it('10. erro de rede exibe "Não foi possível contatar o servidor..."', async () => {
    // HttpResponse.error() simula falha de rede (axios → code ERR_NETWORK).
    server.use(http.post('/api/v1/auth/login', () => HttpResponse.error()));
    const user = userEvent.setup();
    renderLogin();
    await preencherCredenciaisValidas(user);
    await user.click(screen.getByRole('button', { name: /^entrar$/i }));

    const alerta = await screen.findByRole('alert');
    expect(
      within(alerta).getByText('Não foi possível contatar o servidor. Verifique sua conexão.'),
    ).toBeInTheDocument();
  });

  it('11. após erro, o campo senha é limpo e recebe foco', async () => {
    await submeterComErro(401);

    await screen.findByRole('alert');
    const senha = screen.getByLabelText(/^senha$/i);
    expect(senha).toHaveValue('');
    expect(senha).toHaveFocus();
  });

  it('13. login com falha não redireciona (permanece em /login com alerta)', async () => {
    await submeterComErro(401);

    expect(await screen.findByRole('alert')).toBeInTheDocument();
    // Continua na tela de login: botão ainda presente, dashboard ausente.
    expect(screen.getByRole('button', { name: /^entrar$/i })).toBeInTheDocument();
    expect(screen.queryByText('Painel do Dashboard')).not.toBeInTheDocument();
  });
});

describe('Login (RF001) — estado de submitting', () => {
  it('12. durante o request: botão disabled + "Entrando..." e form aria-busy=true', async () => {
    let liberar: (() => void) | null = null;
    const bloqueio = new Promise<void>((resolve) => {
      liberar = resolve;
    });
    server.use(
      http.post('/api/v1/auth/login', async () => {
        await bloqueio; // segura a resposta para observar o estado intermediário
        return HttpResponse.json(loginResponseFixture);
      }),
    );
    const user = userEvent.setup();
    const { container } = renderLogin();

    await preencherCredenciaisValidas(user);
    await user.click(screen.getByRole('button', { name: /^entrar$/i }));

    // Estado intermediário: botão "Entrando..." disabled e form ocupado.
    const botao = await screen.findByRole('button', { name: /entrando/i });
    expect(botao).toBeDisabled();
    expect(container.querySelector('form')).toHaveAttribute('aria-busy', 'true');

    // Libera a resposta e confirma a finalização (redirect).
    liberar!();
    expect(await screen.findByText('Painel do Dashboard')).toBeInTheDocument();
  });
});
