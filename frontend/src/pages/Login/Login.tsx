import { zodResolver } from '@hookform/resolvers/zod';
import axios from 'axios';
import { AlertCircle, Eye, EyeOff, Loader2 } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useAuth } from '@/hooks/useAuth';
import { loginSchema, type LoginFormData } from '@/schemas/loginSchema';

import type { ProblemDetails } from '@/types/auth';

const STORAGE_REMEMBER_EMAIL = 'carwash_remember_email';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'E-mail e senha são obrigatórios.',
  401: 'Usuário ou senha inválidos.',
  403: 'Acesso bloqueado. Usuário inativo.',
  500: 'Não foi possível autenticar agora. Tente novamente em instantes.',
};

function extractErrorMessage(error: unknown): string {
  if (axios.isAxiosError<ProblemDetails>(error)) {
    const status = error.response?.status;
    const titleFromBackend = error.response?.data?.title;
    if (titleFromBackend && status !== 500) {
      return titleFromBackend;
    }
    if (status && status in HTTP_ERROR_MESSAGES) {
      return HTTP_ERROR_MESSAGES[status]!;
    }
    if (error.code === 'ECONNABORTED' || error.code === 'ERR_NETWORK') {
      return 'Não foi possível contatar o servidor. Verifique sua conexão.';
    }
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'Erro inesperado ao autenticar. Tente novamente.';
}

function readRememberedEmail(): string {
  if (typeof window === 'undefined') return '';
  return localStorage.getItem(STORAGE_REMEMBER_EMAIL) ?? '';
}

export default function Login() {
  const navigate = useNavigate();
  const { login, isAuthenticated, isLoading: isRestoringSession } = useAuth();

  const rememberedEmail = useState(readRememberedEmail)[0];
  const [showPassword, setShowPassword] = useState(false);
  const [rememberMe, setRememberMe] = useState(() => rememberedEmail.length > 0);
  const [globalError, setGlobalError] = useState<string | null>(null);

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
    mode: 'onBlur',
    defaultValues: { email: rememberedEmail, senha: '' },
  });

  // Sessão já estabelecida: salta direto pro dashboard.
  useEffect(() => {
    if (!isRestoringSession && isAuthenticated) {
      void navigate('/dashboard', { replace: true });
    }
  }, [isAuthenticated, isRestoringSession, navigate]);

  const onSubmit = useCallback(
    async (data: LoginFormData) => {
      setGlobalError(null);
      try {
        await login({ email: data.email.trim().toLowerCase(), senha: data.senha });

        if (rememberMe) {
          localStorage.setItem(STORAGE_REMEMBER_EMAIL, data.email.trim().toLowerCase());
        } else {
          localStorage.removeItem(STORAGE_REMEMBER_EMAIL);
        }

        void navigate('/dashboard', { replace: true });
      } catch (err) {
        setGlobalError(extractErrorMessage(err));
        form.setValue('senha', '');
        form.setFocus('senha');
      }
    },
    [form, login, navigate, rememberMe],
  );

  const isSubmitting = form.formState.isSubmitting;

  return (
    <div className="flex min-h-screen items-center justify-center bg-background px-4 py-10">
      <Card className="w-full max-w-md gap-0 border border-border bg-card py-0 shadow-xl ring-ring">
        <CardContent className="flex flex-col gap-6 px-8 py-10">
          <div className="flex flex-col items-center gap-3 text-center">
            <div className="flex h-24 w-40 items-center justify-center overflow-hidden rounded-2xl bg-black ring-1 ring-border shadow-sm">
              <img src="/logo.png" alt="CarWash" className="h-full w-full object-contain" />
            </div>
            <div>
              <h1 className="text-xl font-semibold text-foreground">Acesse sua conta</h1>
              <p className="mt-1 text-sm text-muted-foreground">
                Informe suas credenciais para entrar no sistema.
              </p>
            </div>
          </div>

          {globalError && (
            <div
              role="alert"
              aria-live="assertive"
              className="flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
              <p className="text-sm font-medium text-red-400">{globalError}</p>
            </div>
          )}

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            noValidate
            className="flex flex-col gap-5"
            aria-busy={isSubmitting}
          >
            <div className="flex flex-col gap-2">
              <Label htmlFor="login-email" className="text-foreground">
                E-mail
              </Label>
              <Input
                id="login-email"
                type="email"
                autoComplete="email"
                placeholder="voce@empresa.com"
                aria-invalid={!!form.formState.errors.email}
                aria-describedby={form.formState.errors.email ? 'login-email-error' : undefined}
                className="h-10 rounded-lg border-border bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground"
                {...form.register('email')}
              />
              {form.formState.errors.email && (
                <p id="login-email-error" className="text-xs text-red-400">
                  {form.formState.errors.email.message}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="login-senha" className="text-foreground">
                Senha
              </Label>
              <div className="relative">
                <Input
                  id="login-senha"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="current-password"
                  placeholder="••••••••"
                  aria-invalid={!!form.formState.errors.senha}
                  aria-describedby={form.formState.errors.senha ? 'login-senha-error' : undefined}
                  className="h-10 rounded-lg border-border bg-background px-3 pr-10 text-sm text-foreground placeholder:text-muted-foreground"
                  {...form.register('senha')}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword((prev) => !prev)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
                  aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
                  aria-pressed={showPassword}
                  tabIndex={0}
                >
                  {showPassword ? (
                    <EyeOff className="h-4 w-4" aria-hidden="true" />
                  ) : (
                    <Eye className="h-4 w-4" aria-hidden="true" />
                  )}
                </button>
              </div>
              {form.formState.errors.senha && (
                <p id="login-senha-error" className="text-xs text-red-400">
                  {form.formState.errors.senha.message}
                </p>
              )}
            </div>

            <label
              htmlFor="login-remember"
              className="flex items-center gap-2 text-sm text-muted-foreground select-none"
            >
              <input
                id="login-remember"
                type="checkbox"
                checked={rememberMe}
                onChange={(e) => setRememberMe(e.target.checked)}
                className="h-4 w-4 cursor-pointer rounded border-border bg-background text-red-600 accent-red-600 focus:ring-red-600"
              />
              Lembrar meu e-mail neste dispositivo
            </label>

            <Button
              type="submit"
              disabled={isSubmitting}
              className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-60"
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
                  Entrando...
                </>
              ) : (
                'Entrar'
              )}
            </Button>
          </form>

          <p className="text-center text-xs text-muted-foreground">
            Problemas para acessar? Contate o administrador do sistema.
          </p>
        </CardContent>
      </Card>
    </div>
  );
}
