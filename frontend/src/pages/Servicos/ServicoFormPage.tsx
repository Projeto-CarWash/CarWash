import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { AlertCircle, ArrowLeft, Check, Loader2, Wrench, X } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { servicoSchema, type ServicoFormData, type ServicoFormInput } from '@/schemas/servicoSchema';
import { servicoService } from '@/services/servicoService';

import type { ProblemDetails } from '@/types/auth';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'Dados do serviço inválidos. Verifique os campos e tente novamente.',
  401: 'Autenticação obrigatória para executar esta operação.',
  403: 'Você não possui permissão para gerenciar o catálogo de serviços.',
  404: 'Serviço não encontrado.',
  409: 'Já existe serviço cadastrado com este nome.',
  500: 'Não foi possível concluir a operação no momento. Tente novamente.',
};

export function ServicoFormPage() {
  const { id } = useParams<{ id: string }>();
  const isEdicao = !!id;
  const navigate = useNavigate();

  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [carregandoDados, setCarregandoDados] = useState(isEdicao);

  const form = useForm<ServicoFormInput, any, ServicoFormData>({
    resolver: zodResolver(servicoSchema),
    mode: 'onChange',
    shouldFocusError: true,
    defaultValues: {
      nome: '',
      preco: '',
      duracaoMin: '',
    },
  });

  useEffect(() => {
    if (!isEdicao || !id) return;

    let active = true;
    const loadServico = async () => {
      try {
        const response = await servicoService.listar();
        const s = response.itens.find(x => x.id === id);
        
        if (active) {
          if (!s) {
            setGlobalError(HTTP_ERROR_MESSAGES[404] ?? 'Serviço não encontrado.');
            setTimeout(() => navigate('/servicos'), 1500);
          } else {
            form.reset({
              nome: s.nome,
              preco: s.preco.toString().replace('.', ','),
              duracaoMin: String(s.duracaoMin),
            });
          }
        }
      } catch {
        if (active) setGlobalError(HTTP_ERROR_MESSAGES[500] ?? 'Erro interno.');
      } finally {
        if (active) setCarregandoDados(false);
      }
    };

    void loadServico();

    return () => {
      active = false;
    };
  }, [id, isEdicao, form, navigate]);

  const mapBackendFieldToFormField = (field: string): keyof ServicoFormInput | null => {
    const lower = field.toLowerCase();
    const map: Record<string, keyof ServicoFormInput> = {
      nome: 'nome',
      preco: 'preco',
      duracaomin: 'duracaoMin',
    };
    return map[lower] ?? null;
  };

  const onSubmit = useCallback(
    async (data: ServicoFormData) => {
      setGlobalError(null);
      setSuccessMsg(null);

      try {
        if (isEdicao && id) {
          await servicoService.atualizar(id, data);
          setSuccessMsg('Serviço atualizado com sucesso.');
        } else {
          await servicoService.cadastrar(data);
          setSuccessMsg('Serviço cadastrado com sucesso.');
          form.reset();
        }

        setTimeout(() => {
          void navigate('/servicos');
        }, 1000);
      } catch (err) {
        if (err instanceof AxiosError) {
          const status = err.response?.status;
          const dataErr = err.response?.data as ProblemDetails | undefined;

          if (status === 409) {
            setGlobalError(HTTP_ERROR_MESSAGES[409]!);
            form.setError('nome', {
              type: 'manual',
              message: 'Já existe serviço cadastrado com este nome.',
            });
            form.setFocus('nome');
            return;
          }

          if (status === 400 && dataErr?.errors) {
            setGlobalError(HTTP_ERROR_MESSAGES[400]!);
            let firstFocused = false;

            for (const [field, messages] of Object.entries(dataErr.errors)) {
              const mappedKey = mapBackendFieldToFormField(field);
              if (mappedKey && messages?.[0]) {
                form.setError(mappedKey, { message: messages[0] });
                if (!firstFocused) {
                  form.setFocus(mappedKey);
                  firstFocused = true;
                }
              }
            }
            return;
          }

          if (status === 401) {
            setGlobalError(HTTP_ERROR_MESSAGES[401]!);
            setTimeout(() => {
              void navigate('/login');
            }, 1500);
            return;
          }
          
          if (status === 403) {
             setGlobalError(HTTP_ERROR_MESSAGES[403]!);
             return;
          }

          if (status === 404) {
            setGlobalError(HTTP_ERROR_MESSAGES[404]!);
            setTimeout(() => navigate('/servicos'), 1500);
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
    [form, navigate, isEdicao, id]
  );

  const isSubmitting = form.formState.isSubmitting;
  const errors = form.formState.errors;
  const hasErrors = Object.keys(errors).length > 0;
  const isSubmitDisabled = isSubmitting || hasErrors || !form.formState.isValid;

  if (carregandoDados) {
    return (
      <div className="flex h-full items-center justify-center text-zinc-500">
        <Loader2 className="mr-2 h-5 w-5 animate-spin" />
        Carregando serviço...
      </div>
    );
  }

  return (
    <div className="px-8 py-8">
      {successMsg && (
        <div
          role="status"
          aria-live="polite"
          className="fixed right-5 top-5 z-[600] flex items-center gap-3 rounded-lg border border-green-500/30 bg-zinc-950 px-4 py-3 text-sm text-green-400 shadow-xl shadow-black/60 animate-in fade-in slide-in-from-top-5 duration-300"
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
            <Wrench className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-zinc-50">
              {isEdicao ? 'Editar serviço' : 'Novo serviço'}
            </h1>
            <p className="mt-1 text-sm text-zinc-400">
              {isEdicao
                ? 'Atualize os dados e o valor deste serviço no catálogo.'
                : 'Cadastre um novo serviço para o catálogo do CarWash.'}
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate('/servicos')}
          disabled={isSubmitting}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm text-zinc-300 hover:bg-zinc-800/50 hover:text-zinc-100"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" />
          Voltar
        </Button>
      </div>

      <Card className="border border-zinc-800/60 bg-zinc-900/30 max-w-3xl">
        <CardHeader>
          <CardTitle className="text-lg text-zinc-100">Dados do serviço</CardTitle>
          <CardDescription className="text-zinc-400">
            Preencha corretamente os valores que serão exibidos aos clientes e agenda.
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

          <form
            onSubmit={form.handleSubmit(onSubmit)}
            className="grid grid-cols-1 gap-5 md:grid-cols-2"
            aria-busy={isSubmitting}
          >
            <div className="flex flex-col gap-2 md:col-span-2">
              <Label htmlFor="servico-nome" className="text-zinc-300">
                Nome do serviço
              </Label>
              <Controller
                control={form.control}
                name="nome"
                render={({ field, fieldState }) => (
                  <>
                    <Input
                      id="servico-nome"
                      type="text"
                      placeholder="Ex: Lavagem Completa Especial"
                      value={field.value}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                      ref={field.ref}
                      aria-invalid={!!fieldState.error}
                      aria-describedby={fieldState.error ? 'servico-nome-error' : undefined}
                      className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                        fieldState.error
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                      }`}
                    />
                    {fieldState.error && (
                      <p id="servico-nome-error" role="alert" className="text-xs text-red-400">
                        {fieldState.error.message}
                      </p>
                    )}
                  </>
                )}
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="servico-preco" className="text-zinc-300">
                Preço (R$)
              </Label>
              <Controller
                control={form.control}
                name="preco"
                render={({ field, fieldState }) => (
                  <>
                    <Input
                      id="servico-preco"
                      type="text"
                      inputMode="decimal"
                      placeholder="Ex: 89,90"
                      value={(field.value as string | number | undefined) ?? ''}
                      onChange={(e) => {
                         const val = e.target.value.replace(/[^0-9.,]/g, '');
                         field.onChange(val);
                      }}
                      onBlur={field.onBlur}
                      ref={field.ref}
                      aria-invalid={!!fieldState.error}
                      aria-describedby={fieldState.error ? 'servico-preco-error' : undefined}
                      className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                        fieldState.error
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                      }`}
                    />
                    {fieldState.error ? (
                      <p id="servico-preco-error" role="alert" className="text-xs text-red-400">
                        {fieldState.error.message}
                      </p>
                    ) : (
                      <p className="text-xs text-zinc-500">Utilize vírgula para centavos.</p>
                    )}
                  </>
                )}
              />
            </div>

            <div className="flex flex-col gap-2">
              <Label htmlFor="servico-duracao" className="text-zinc-300">
                Duração (Minutos)
              </Label>
              <Controller
                control={form.control}
                name="duracaoMin"
                render={({ field, fieldState }) => (
                  <>
                    <Input
                      id="servico-duracao"
                      type="number"
                      inputMode="numeric"
                      placeholder="Ex: 90"
                      value={(field.value as string | number | undefined) ?? ''}
                      onChange={field.onChange}
                      onBlur={field.onBlur}
                      ref={field.ref}
                      min={1}
                      max={1440}
                      aria-invalid={!!fieldState.error}
                      aria-describedby={fieldState.error ? 'servico-duracao-error' : undefined}
                      className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                        fieldState.error
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                      }`}
                    />
                    {fieldState.error && (
                      <p id="servico-duracao-error" role="alert" className="text-xs text-red-400">
                        {fieldState.error.message}
                      </p>
                    )}
                  </>
                )}
              />
            </div>

            <div className="flex items-center justify-end gap-3 md:col-span-2 mt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => void navigate('/servicos')}
                disabled={isSubmitting}
                className="h-10 rounded-full border-zinc-700/60 bg-transparent px-5 text-sm text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
              >
                Cancelar
              </Button>
              <Button
                type="submit"
                disabled={isSubmitDisabled}
                className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700 disabled:opacity-60"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" aria-hidden="true" />
                    Salvando...
                  </>
                ) : (
                  'Salvar serviço'
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
