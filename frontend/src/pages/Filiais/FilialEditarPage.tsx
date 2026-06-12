import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { AlertCircle, ArrowLeft, Building2, Check, Loader2, X } from 'lucide-react';
import { useCallback, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { CelulasAtivasField } from '@/components/filiais/CelulasAtivasField';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAlterarCelulasAtivas, useAlterarStatusFilial, useFilial } from '@/hooks/useFilialQueries';
import {
  editarFilialSchema,
  type EditarFilialFormData,
  type EditarFilialFormInput,
} from '@/schemas/filialSchema';

import type { ProblemDetails } from '@/types/auth';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'Valor de células ativas inválido. Informe um número inteiro entre 1 e 100.',
  401: 'Autenticação obrigatória para executar esta operação.',
  403: 'Você não possui permissão para alterar configuração da filial.',
  404: 'Filial não encontrada.',
  500: 'Não foi possível concluir a operação no momento. Tente novamente.',
};

/** Extrai uma mensagem apresentável a partir de um erro do axios. */
function mensagemErro(err: unknown): string {
  if (!(err instanceof AxiosError) || !err.response) {
    if (err instanceof AxiosError && (err.code === 'ECONNABORTED' || err.code === 'ERR_NETWORK')) {
      return 'Não foi possível contatar o servidor. Verifique sua conexão.';
    }
    return HTTP_ERROR_MESSAGES[500]!;
  }
  const status = err.response.status;
  const problem = err.response.data as ProblemDetails | undefined;
  const erroCampo = problem?.errors?.celulasAtivas?.[0] ?? problem?.errors?.CelulasAtivas?.[0];
  return erroCampo ?? problem?.title ?? HTTP_ERROR_MESSAGES[status] ?? HTTP_ERROR_MESSAGES[500]!;
}

/**
 * Edição de filial (RF018). O contrato do backend só permite ajustar a
 * quantidade de células ativas e o status (ativa/inativa); os demais campos
 * (nome, código, CNPJ, endereço) são exibidos como somente leitura.
 */
export function FilialEditarPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const { data: filial, isLoading, isError, error } = useFilial(id);
  const { mutate: salvarCelulas, isPending: salvando } = useAlterarCelulasAtivas();
  const { mutate: alterarStatus, isPending: alterandoStatus } = useAlterarStatusFilial();

  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);

  const form = useForm<EditarFilialFormInput, unknown, EditarFilialFormData>({
    resolver: zodResolver(editarFilialSchema),
    mode: 'onChange',
    shouldFocusError: true,
    values: filial ? { celulasAtivas: String(filial.celulasAtivas) } : undefined,
  });

  const onSubmit = useCallback(
    (data: EditarFilialFormData) => {
      if (!id || salvando) return;
      setGlobalError(null);
      setSuccessMsg(null);

      salvarCelulas(
        { id, celulasAtivas: data.celulasAtivas },
        {
          onSuccess: () => {
            setSuccessMsg('Configuração de células ativas atualizada com sucesso.');
            setTimeout(() => void navigate('/filiais'), 1000);
          },
          onError: (err) => {
            const status = err instanceof AxiosError ? err.response?.status : undefined;
            if (status === 404) {
              setGlobalError(HTTP_ERROR_MESSAGES[404]!);
              setTimeout(() => void navigate('/filiais'), 1500);
              return;
            }
            if (status === 401) {
              setGlobalError(HTTP_ERROR_MESSAGES[401]!);
              setTimeout(() => void navigate('/login'), 1500);
              return;
            }
            // 400 — destaca o campo e mantém o valor digitado para correção.
            if (status === 400) {
              const msg = mensagemErro(err);
              form.setError('celulasAtivas', { type: 'server', message: msg });
              form.setFocus('celulasAtivas');
              return;
            }
            // 403, 500 e rede: preserva o valor digitado e permite nova tentativa.
            setGlobalError(mensagemErro(err));
          },
        },
      );
    },
    [id, salvando, salvarCelulas, navigate, form],
  );

  const errors = form.formState.errors;
  const salvarDesabilitado = salvando || Object.keys(errors).length > 0 || !form.formState.isValid;

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center text-muted-foreground">
        <Loader2 className="mr-2 h-5 w-5 animate-spin" aria-hidden="true" />
        Carregando filial...
      </div>
    );
  }

  if (isError || !filial) {
    const status = error instanceof AxiosError ? error.response?.status : undefined;
    return (
      <div className="px-8 py-8">
        <div
          role="alert"
          className="flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3"
        >
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
          <p className="flex-1 text-sm font-medium text-red-400">
            {status === 404 ? HTTP_ERROR_MESSAGES[404] : 'Não foi possível carregar a filial.'}
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/filiais')}
          className="mt-4 h-9 rounded-full border-border bg-transparent px-4 text-sm text-foreground hover:bg-accent hover:text-foreground"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" /> Voltar
        </Button>
      </div>
    );
  }

  return (
    <div className="px-8 py-8">
      {successMsg && (
        <div
          role="status"
          aria-live="polite"
          className="fixed right-5 top-5 z-[600] flex items-center gap-3 rounded-lg border border-green-500/30 bg-background px-4 py-3 text-sm text-green-400 shadow-xl shadow-black/60 duration-300 animate-in fade-in slide-in-from-top-5"
        >
          <span className="flex h-5 w-5 items-center justify-center rounded-full bg-green-500/20 text-green-500">
            <Check className="h-3 w-3" />
          </span>
          <span className="font-semibold">{successMsg}</span>
        </div>
      )}

      <div className="mb-6 flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
            aria-hidden="true"
          >
            <Building2 className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-foreground">Editar filial</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Ajuste a capacidade e o status operacional da unidade.
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/filiais')}
          className="h-9 rounded-full border-border bg-transparent px-4 text-sm text-foreground hover:bg-accent hover:text-foreground"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" /> Voltar
        </Button>
      </div>

      <Card className="max-w-3xl border border-border bg-card">
        <CardHeader>
          <CardTitle className="text-lg text-foreground">{filial.nome}</CardTitle>
          <CardDescription className="text-muted-foreground">
            Apenas células ativas e status podem ser alterados nesta tela.
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

          {/* Status operacional (RF017) — filial inativa não aceita novos agendamentos (RF019) */}
          <div className="mb-6 flex items-center justify-between gap-4 rounded-xl border border-border bg-background px-4 py-3">
            <div>
              <p className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground">STATUS</p>
              <span
                className={`mt-1 inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider ${
                  filial.ativa ? 'bg-green-500/10 text-green-400' : 'bg-muted text-muted-foreground'
                }`}
              >
                {filial.ativa ? 'Ativa' : 'Inativa'}
              </span>
            </div>
            <Button
              type="button"
              variant="outline"
              disabled={alterandoStatus}
              onClick={() => {
                setGlobalError(null);
                setSuccessMsg(null);
                alterarStatus(
                  { id: filial.id, ativo: !filial.ativa },
                  {
                    onSuccess: (atualizada) => {
                      setSuccessMsg(
                        atualizada.ativa
                          ? 'Filial ativada com sucesso.'
                          : 'Filial inativada com sucesso — não receberá novos agendamentos.',
                      );
                    },
                    onError: (err) => setGlobalError(mensagemErro(err)),
                  },
                );
              }}
              className="h-9 rounded-full border-border bg-transparent px-4 text-sm text-foreground hover:bg-accent hover:text-foreground"
            >
              {alterandoStatus ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
                  Alterando...
                </>
              ) : filial.ativa ? (
                'Inativar filial'
              ) : (
                'Ativar filial'
              )}
            </Button>
          </div>

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="grid grid-cols-1 gap-5 md:grid-cols-2"
            aria-busy={salvando}
            noValidate
          >
            <Controller
              control={form.control}
              name="celulasAtivas"
              render={({ field, fieldState }) => (
                <CelulasAtivasField
                  id="filial-celulas"
                  value={field.value as string | number | undefined}
                  onChange={field.onChange}
                  onBlur={field.onBlur}
                  inputRef={field.ref}
                  error={fieldState.error?.message}
                  disabled={salvando}
                />
              )}
            />

            <div className="mt-4 flex items-center justify-end gap-3 md:col-span-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => void navigate('/filiais')}
                disabled={salvando}
                className="h-10 rounded-full border-border bg-transparent px-5 text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                disabled={salvarDesabilitado}
                className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-60"
              >
                {salvando ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
                    Salvando...
                  </>
                ) : (
                  'Salvar alterações'
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
