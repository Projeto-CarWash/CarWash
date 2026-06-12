import { zodResolver } from '@hookform/resolvers/zod';
import axios from 'axios';
import { AlertCircle, ArrowLeft, Check, Eye, EyeOff, Loader2, UserPlus, X } from 'lucide-react';
import { useCallback, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { usuarioSchema, type UsuarioFormData } from '@/schemas/usuarioSchema';
import { userService } from '@/services/userService';

import type { ProblemDetails } from '@/types/auth';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'Dados do usuário inválidos. Verifique os campos e tente novamente.',
  401: 'Autenticação obrigatória para executar esta operação.',
  403: 'Você não possui permissão para cadastrar usuários.',
  409: 'Já existe usuário cadastrado com este e-mail.',
  500: 'Não foi possível criar o usuário agora. Tente novamente.',
};

/** Campos do formulário que aceitam erros mapeados do backend. */
const CAMPOS_FORM = ['nome', 'email', 'senha', 'perfil'] as const;
type CampoForm = (typeof CAMPOS_FORM)[number];

function isCampoForm(value: string): value is CampoForm {
  return (CAMPOS_FORM as readonly string[]).includes(value);
}

export function NovoUsuarioPage() {
  const navigate = useNavigate();
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [showSenha, setShowSenha] = useState(false);
  const [showConfirmar, setShowConfirmar] = useState(false);

  const form = useForm<UsuarioFormData>({
    resolver: zodResolver(usuarioSchema),
    mode: 'onBlur',
    shouldFocusError: true,
    defaultValues: {
      nome: '',
      email: '',
      senha: '',
      confirmarSenha: '',
      perfil: 'Funcionario',
    },
  });

  const onSubmit = useCallback(
    async (data: UsuarioFormData) => {
      setGlobalError(null);
      setSuccessMsg(null);

      try {
        await userService.create({
          nome: data.nome.trim(),
          email: data.email.trim().toLowerCase(),
          senha: data.senha,
          perfil: data.perfil,
        });

        setSuccessMsg('Usuário cadastrado com sucesso! Redirecionando...');
        form.reset();
        setTimeout(() => {
          void navigate('/dashboard');
        }, 2000);
      } catch (err) {
        if (axios.isAxiosError<ProblemDetails>(err)) {
          const status = err.response?.status;
          const data = err.response?.data;
          const title = data?.title;

          if (status === 409) {
            setGlobalError(title ?? HTTP_ERROR_MESSAGES[409]!);
            form.setError('email', { message: 'Já existe usuário com este e-mail.' });
            form.setFocus('email');
            return;
          }

          if (status === 400 && data?.errors) {
            // Mapeia errors[campo] = string[] para o campo correspondente do form.
            let mapeouAlgum = false;
            for (const [campo, mensagens] of Object.entries(data.errors)) {
              const chave = campo.charAt(0).toLowerCase() + campo.slice(1);
              if (isCampoForm(chave) && mensagens.length > 0) {
                form.setError(chave, { message: mensagens[0] });
                mapeouAlgum = true;
              }
            }
            setGlobalError(title ?? HTTP_ERROR_MESSAGES[400]!);
            if (!mapeouAlgum) {
              form.setFocus('nome');
            }
            return;
          }

          const msg = status && status in HTTP_ERROR_MESSAGES ? HTTP_ERROR_MESSAGES[status]! : null;
          if (msg) {
            setGlobalError(msg);
            return;
          }

          if (err.code === 'ECONNABORTED' || err.code === 'ERR_NETWORK') {
            setGlobalError('Não foi possível contatar o servidor. Verifique sua conexão.');
            return;
          }
        }
        setGlobalError(HTTP_ERROR_MESSAGES[500]!);
      }
    },
    [form, navigate],
  );

  const isSubmitting = form.formState.isSubmitting;

  return (
    <div className="px-8 py-8">
      <div className="mb-6 flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
            aria-hidden="true"
          >
            <UserPlus className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">
              Novo usuário interno
            </h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Cadastre administradores e funcionários do sistema (RF014).
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate(-1)}
          disabled={isSubmitting}
          className="h-9 rounded-full border-border bg-transparent px-4 text-sm text-foreground hover:bg-accent hover:text-foreground"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" />
          Voltar
        </Button>
      </div>

      <Card className="border border-border bg-card">
        <CardHeader>
          <CardTitle className="text-lg text-foreground">Dados do usuário</CardTitle>
          <CardDescription className="text-muted-foreground">
            Senha deve ter no mínimo 8 caracteres, contendo letras e números.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {globalError && (
            <div
              role="alert"
              aria-live="assertive"
              className="mb-6 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
            >
              <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
              <p className="flex-1 text-sm font-medium text-red-400">{globalError}</p>
              <button
                type="button"
                onClick={() => setGlobalError(null)}
                aria-label="Fechar mensagem de erro"
                className="shrink-0 text-red-500/60 transition-colors hover:text-red-400"
              >
                <X className="h-4 w-4" aria-hidden="true" />
              </button>
            </div>
          )}

          {successMsg && (
            <div
              role="status"
              aria-live="polite"
              className="mb-6 flex items-start gap-3 rounded-xl border border-green-500/30 bg-green-950/30 px-4 py-3"
            >
              <Check className="mt-0.5 h-4 w-4 shrink-0 text-green-500" aria-hidden="true" />
              <p className="flex-1 text-sm font-medium text-green-400">{successMsg}</p>
            </div>
          )}

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            noValidate
            className="grid grid-cols-1 gap-5 md:grid-cols-2"
            aria-busy={isSubmitting}
          >
            <div className="flex flex-col gap-2 md:col-span-2">
              <Label htmlFor="usuario-nome" className="text-foreground">
                Nome completo
              </Label>
              <Input
                id="usuario-nome"
                type="text"
                autoComplete="name"
                placeholder="Ex: Maria Souza"
                aria-invalid={!!form.formState.errors.nome}
                aria-describedby={form.formState.errors.nome ? 'usuario-nome-error' : undefined}
                className="h-10 rounded-lg border-border bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground"
                {...form.register('nome')}
              />
              {form.formState.errors.nome && (
                <p id="usuario-nome-error" role="alert" className="text-xs text-red-400">
                  {form.formState.errors.nome.message}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="usuario-email" className="text-foreground">
                E-mail
              </Label>
              <Input
                id="usuario-email"
                type="email"
                autoComplete="email"
                placeholder="usuario@carwash.com"
                aria-invalid={!!form.formState.errors.email}
                aria-describedby={form.formState.errors.email ? 'usuario-email-error' : undefined}
                className="h-10 rounded-lg border-border bg-background px-3 text-sm text-foreground placeholder:text-muted-foreground"
                {...form.register('email')}
              />
              {form.formState.errors.email && (
                <p id="usuario-email-error" role="alert" className="text-xs text-red-400">
                  {form.formState.errors.email.message}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="usuario-perfil" className="text-foreground">
                Perfil de acesso
              </Label>
              <select
                id="usuario-perfil"
                aria-invalid={!!form.formState.errors.perfil}
                aria-describedby={form.formState.errors.perfil ? 'usuario-perfil-error' : undefined}
                className="h-10 rounded-lg border border-border bg-background px-3 text-sm text-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none aria-invalid:border-red-500/60 aria-invalid:ring-3 aria-invalid:ring-red-500/20"
                {...form.register('perfil')}
              >
                <option value="Funcionario">Funcionário</option>
                <option value="Admin">Administrador</option>
              </select>
              {form.formState.errors.perfil && (
                <p id="usuario-perfil-error" role="alert" className="text-xs text-red-400">
                  {form.formState.errors.perfil.message}
                </p>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="usuario-senha" className="text-foreground">
                Senha inicial
              </Label>
              <div className="relative">
                <Input
                  id="usuario-senha"
                  type={showSenha ? 'text' : 'password'}
                  autoComplete="new-password"
                  placeholder="Mínimo 8 caracteres"
                  aria-invalid={!!form.formState.errors.senha}
                  aria-describedby={
                    form.formState.errors.senha ? 'usuario-senha-error' : 'usuario-senha-hint'
                  }
                  className="h-10 rounded-lg border-border bg-background px-3 pr-10 text-sm text-foreground placeholder:text-muted-foreground"
                  {...form.register('senha')}
                />
                <button
                  type="button"
                  onClick={() => setShowSenha((prev) => !prev)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
                  aria-label={showSenha ? 'Ocultar senha' : 'Mostrar senha'}
                  aria-pressed={showSenha}
                >
                  {showSenha ? (
                    <EyeOff className="h-4 w-4" aria-hidden="true" />
                  ) : (
                    <Eye className="h-4 w-4" aria-hidden="true" />
                  )}
                </button>
              </div>
              {form.formState.errors.senha ? (
                <p id="usuario-senha-error" role="alert" className="text-xs text-red-400">
                  {form.formState.errors.senha.message}
                </p>
              ) : (
                <p id="usuario-senha-hint" className="text-xs text-muted-foreground">
                  Use letras e números, sem espaços. Entre 8 e 128 caracteres.
                </p>
              )}
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="usuario-confirmar" className="text-foreground">
                Confirmar senha
              </Label>
              <div className="relative">
                <Input
                  id="usuario-confirmar"
                  type={showConfirmar ? 'text' : 'password'}
                  autoComplete="new-password"
                  placeholder="Repita a senha"
                  aria-invalid={!!form.formState.errors.confirmarSenha}
                  aria-describedby={
                    form.formState.errors.confirmarSenha ? 'usuario-confirmar-error' : undefined
                  }
                  className="h-10 rounded-lg border-border bg-background px-3 pr-10 text-sm text-foreground placeholder:text-muted-foreground"
                  {...form.register('confirmarSenha')}
                />
                <button
                  type="button"
                  onClick={() => setShowConfirmar((prev) => !prev)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-1 text-muted-foreground transition-colors hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring/50"
                  aria-label={showConfirmar ? 'Ocultar confirmação' : 'Mostrar confirmação'}
                  aria-pressed={showConfirmar}
                >
                  {showConfirmar ? (
                    <EyeOff className="h-4 w-4" aria-hidden="true" />
                  ) : (
                    <Eye className="h-4 w-4" aria-hidden="true" />
                  )}
                </button>
              </div>
              {form.formState.errors.confirmarSenha && (
                <p id="usuario-confirmar-error" role="alert" className="text-xs text-red-400">
                  {form.formState.errors.confirmarSenha.message}
                </p>
              )}
            </div>

            <div className="flex items-center justify-end gap-3 md:col-span-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => void navigate(-1)}
                disabled={isSubmitting}
                className="h-10 rounded-full border-border bg-transparent px-5 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                disabled={isSubmitting}
                className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-60"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
                    Salvando...
                  </>
                ) : (
                  'Salvar usuário'
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
