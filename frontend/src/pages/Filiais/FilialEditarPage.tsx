import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { AlertCircle, ArrowLeft, Building2, Check, Loader2, X } from 'lucide-react';
import { useCallback, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useAlterarCelulasAtivas, useFilial } from '@/hooks/useFilialQueries';
import {
  editarFilialSchema,
  type EditarFilialFormData,
  type EditarFilialFormInput,
} from '@/schemas/filialSchema';

import type { ProblemDetails } from '@/types/auth';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'Dados da filial inválidos. Verifique os campos e tente novamente.',
  401: 'Autenticação obrigatória para executar esta operação.',
  403: 'Você não possui permissão para editar filiais.',
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
            setSuccessMsg('Filial atualizada com sucesso.');
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
            setGlobalError(mensagemErro(err));
          },
        },
      );
    },
    [id, salvando, salvarCelulas, navigate],
  );

  const errors = form.formState.errors;
  const salvarDesabilitado = salvando || Object.keys(errors).length > 0 || !form.formState.isValid;

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center text-zinc-500">
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
          className="mt-4 h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm text-zinc-300 hover:bg-zinc-800/50 hover:text-zinc-100"
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
          className="fixed right-5 top-5 z-[600] flex items-center gap-3 rounded-lg border border-green-500/30 bg-zinc-950 px-4 py-3 text-sm text-green-400 shadow-xl shadow-black/60 duration-300 animate-in fade-in slide-in-from-top-5"
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
            <h1 className="text-2xl font-bold tracking-tight text-zinc-50">Editar filial</h1>
            <p className="mt-1 text-sm text-zinc-400">
              Ajuste a capacidade e o status operacional da unidade.
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/filiais')}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm text-zinc-300 hover:bg-zinc-800/50 hover:text-zinc-100"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" /> Voltar
        </Button>
      </div>

      <Card className="max-w-3xl border border-zinc-800/60 bg-zinc-900/30">
        <CardHeader>
          <CardTitle className="text-lg text-zinc-100">{filial.nome}</CardTitle>
          <CardDescription className="text-zinc-400">
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

          {/* Status operacional (somente leitura) */}
          <div className="mb-6 rounded-xl border border-zinc-800/60 bg-zinc-950/40 px-4 py-3">
            <p className="text-[10px] font-bold tracking-[0.2em] text-zinc-500">STATUS</p>
            <span
              className={`mt-1 inline-flex items-center rounded-full px-2 py-0.5 text-[10px] font-bold uppercase tracking-wider ${
                filial.ativa ? 'bg-green-500/10 text-green-400' : 'bg-zinc-800/50 text-zinc-500'
              }`}
            >
              {filial.ativa ? 'Ativa' : 'Inativa'}
            </span>
          </div>

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="grid grid-cols-1 gap-5 md:grid-cols-2"
            aria-busy={salvando}
            noValidate
          >
            <div className="flex flex-col gap-2">
              <Label htmlFor="filial-celulas" className="text-zinc-300">
                Células ativas
              </Label>
              <Controller
                control={form.control}
                name="celulasAtivas"
                render={({ field, fieldState }) => (
                  <>
                    <Input
                      id="filial-celulas"
                      type="number"
                      inputMode="numeric"
                      min={1}
                      max={100}
                      value={(field.value as string | number | undefined) ?? ''}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                      ref={field.ref}
                      aria-invalid={!!fieldState.error}
                      aria-describedby={
                        fieldState.error ? 'filial-celulas-error' : 'filial-celulas-hint'
                      }
                      className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                        fieldState.error
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                      }`}
                    />
                    {fieldState.error ? (
                      <p id="filial-celulas-error" role="alert" className="text-xs text-red-400">
                        {fieldState.error.message}
                      </p>
                    ) : (
                      <p id="filial-celulas-hint" className="text-xs text-zinc-500">
                        Número inteiro entre 1 e 100.
                      </p>
                    )}
                  </>
                )}
              />
            </div>

            <div className="mt-4 flex items-center justify-end gap-3 md:col-span-2">
              <Button
                type="button"
                variant="outline"
                onClick={() => void navigate('/filiais')}
                disabled={salvando}
                className="h-10 rounded-full border-zinc-700/60 bg-transparent px-5 text-sm text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
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
