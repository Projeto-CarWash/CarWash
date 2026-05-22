import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { AlertCircle, ArrowLeft, Car, Check, Loader2, X } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { veiculoSchema, type VeiculoFormData } from '@/schemas/veiculoSchema';
import { clienteService, type ClienteDetalhe } from '@/services/clienteService';
import { veiculoService } from '@/services/veiculoService';

import type { ProblemDetails } from '@/types/auth';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'Dados do veículo inválidos. Verifique os campos e tente novamente.',
  401: 'Sessão expirada. Faça login novamente.',
  403: 'Você não possui permissão para cadastrar veículos.',
  409: 'Já existe veículo cadastrado com esta placa.',
  500: 'Não foi possível concluir o cadastro no momento. Tente novamente.',
};

export function NovoVeiculoPage() {
  const { id: clienteId = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [cliente, setCliente] = useState<ClienteDetalhe | null>(null);
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [carregandoCliente, setCarregandoCliente] = useState(true);

  const form = useForm<VeiculoFormData>({
    resolver: zodResolver(veiculoSchema),
    mode: 'onBlur',
    shouldFocusError: true,
    defaultValues: {
      placa: '',
      modelo: '',
      fabricante: '',
      cor: '',
      ano: '',
    },
  });

  // Fetch client details to show personalized header
  useEffect(() => {
    let cancelado = false;
    if (!clienteId) return;

    void (async () => {
      try {
        const c = await clienteService.obterPorId(clienteId);
        if (!cancelado) {
          setCliente(c);
        }
      } catch {
        if (!cancelado) {
          setGlobalError('Cliente não encontrado.');
        }
      } finally {
        if (!cancelado) {
          setCarregandoCliente(false);
        }
      }
    })();

    return () => {
      cancelado = true;
    };
  }, [clienteId]);

  const normalizePlaca = (val: string) => {
    return val
      .replace(/[^A-Za-z0-9]/g, '')
      .toUpperCase()
      .slice(0, 7);
  };

  const mapBackendFieldToFormField = (field: string): keyof VeiculoFormData | null => {
    const lower = field.toLowerCase();
    const map: Record<string, keyof VeiculoFormData> = {
      placa: 'placa',
      modelo: 'modelo',
      fabricante: 'fabricante',
      cor: 'cor',
      ano: 'ano',
    };
    return map[lower] ?? null;
  };

  const onSubmit = useCallback(
    async (data: VeiculoFormData) => {
      setGlobalError(null);
      setSuccessMsg(null);

      try {
        await veiculoService.cadastrar(clienteId, data);
        setSuccessMsg('Veículo cadastrado com sucesso! Redirecionando...');
        form.reset();
        setTimeout(() => {
          void navigate(`/clientes/${clienteId}`);
        }, 1000);
      } catch (err) {
        if (err instanceof AxiosError) {
          const status = err.response?.status;
          const dataErr = err.response?.data as ProblemDetails | undefined;

          if (status === 409) {
            setGlobalError(dataErr?.title ?? HTTP_ERROR_MESSAGES[409]!);
            form.setError('placa', {
              message: 'Já existe veículo cadastrado com esta placa.',
            });
            form.setFocus('placa');
            return;
          }

          if (status === 400 && dataErr?.errors) {
            setGlobalError(dataErr.title ?? HTTP_ERROR_MESSAGES[400]!);
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
    [clienteId, form, navigate],
  );

  const isSubmitting = form.formState.isSubmitting;

  if (carregandoCliente && !cliente && !globalError) {
    return <div className="px-8 py-8 text-sm text-zinc-500">Carregando…</div>;
  }

  return (
    <div className="px-8 py-8">
      <div className="mb-6 flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
            aria-hidden="true"
          >
            <Car className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-zinc-50">Novo veículo</h1>
            <p className="mt-1 text-sm text-zinc-400">
              {cliente
                ? `Cadastre um veículo para o cliente ${cliente.nome}`
                : 'Cadastre um veículo para o cliente.'}
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => void navigate(`/clientes/${clienteId}`)}
          disabled={isSubmitting}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm text-zinc-300 hover:bg-zinc-800/50 hover:text-zinc-100"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" />
          Voltar
        </Button>
      </div>

      <Card className="border border-zinc-800/60 bg-zinc-900/30">
        <CardHeader>
          <CardTitle className="text-lg text-zinc-100">Dados do veículo</CardTitle>
          <CardDescription className="text-zinc-400">
            A placa do veículo passará por validação e não pode ser duplicada no sistema.
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
            {/* Placa */}
            <div className="flex flex-col gap-2">
              <Label htmlFor="veiculo-placa" className="text-zinc-300">
                Placa
              </Label>
              <Controller
                control={form.control}
                name="placa"
                render={({ field, fieldState }) => (
                  <>
                    <Input
                      id="veiculo-placa"
                      type="text"
                      placeholder="Ex: AAA0A00 ou AAA0000"
                      value={field.value}
                      onChange={(e) => {
                        const val = normalizePlaca(e.target.value);
                        field.onChange(val);
                      }}
                      onBlur={field.onBlur}
                      ref={field.ref}
                      aria-invalid={!!fieldState.error}
                      aria-describedby={
                        fieldState.error ? 'veiculo-placa-error' : 'veiculo-placa-hint'
                      }
                      className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                        fieldState.error
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                      }`}
                    />
                    {fieldState.error ? (
                      <p id="veiculo-placa-error" role="alert" className="text-xs text-red-400">
                        {fieldState.error.message}
                      </p>
                    ) : (
                      <p id="veiculo-placa-hint" className="text-xs text-zinc-500">
                        Formatos aceitos: AAA0000 ou AAA0A00.
                      </p>
                    )}
                  </>
                )}
              />
            </div>

            {/* Fabricante */}
            <div className="flex flex-col gap-2">
              <Label htmlFor="veiculo-fabricante" className="text-zinc-300">
                Fabricante
              </Label>
              <Input
                id="veiculo-fabricante"
                type="text"
                placeholder="Ex: Volkswagen"
                aria-invalid={!!form.formState.errors.fabricante}
                aria-describedby={
                  form.formState.errors.fabricante ? 'veiculo-fabricante-error' : undefined
                }
                className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                  form.formState.errors.fabricante
                    ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                    : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                }`}
                {...form.register('fabricante')}
              />
              {form.formState.errors.fabricante && (
                <p id="veiculo-fabricante-error" role="alert" className="text-xs text-red-400">
                  {form.formState.errors.fabricante.message}
                </p>
              )}
            </div>

            {/* Modelo */}
            <div className="flex flex-col gap-2">
              <Label htmlFor="veiculo-modelo" className="text-zinc-300">
                Modelo
              </Label>
              <Input
                id="veiculo-modelo"
                type="text"
                placeholder="Ex: Gol 1.0"
                aria-invalid={!!form.formState.errors.modelo}
                aria-describedby={form.formState.errors.modelo ? 'veiculo-modelo-error' : undefined}
                className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                  form.formState.errors.modelo
                    ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                    : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                }`}
                {...form.register('modelo')}
              />
              {form.formState.errors.modelo && (
                <p id="veiculo-modelo-error" role="alert" className="text-xs text-red-400">
                  {form.formState.errors.modelo.message}
                </p>
              )}
            </div>

            {/* Cor */}
            <div className="flex flex-col gap-2">
              <Label htmlFor="veiculo-cor" className="text-zinc-300">
                Cor
              </Label>
              <Input
                id="veiculo-cor"
                type="text"
                placeholder="Ex: Preto"
                aria-invalid={!!form.formState.errors.cor}
                aria-describedby={form.formState.errors.cor ? 'veiculo-cor-error' : undefined}
                className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                  form.formState.errors.cor
                    ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                    : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                }`}
                {...form.register('cor')}
              />
              {form.formState.errors.cor && (
                <p id="veiculo-cor-error" role="alert" className="text-xs text-red-400">
                  {form.formState.errors.cor.message}
                </p>
              )}
            </div>

            {/* Ano */}
            <div className="flex flex-col gap-2">
              <Label htmlFor="veiculo-ano" className="text-zinc-300">
                Ano (Opcional)
              </Label>
              <Input
                id="veiculo-ano"
                type="text"
                maxLength={4}
                placeholder="Ex: 2020"
                aria-invalid={!!form.formState.errors.ano}
                aria-describedby={form.formState.errors.ano ? 'veiculo-ano-error' : undefined}
                className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                  form.formState.errors.ano
                    ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                    : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                }`}
                {...form.register('ano')}
              />
              {form.formState.errors.ano && (
                <p id="veiculo-ano-error" role="alert" className="text-xs text-red-400">
                  {form.formState.errors.ano.message}
                </p>
              )}
            </div>

            {/* Ações */}
            <div className="flex items-center justify-end gap-3 md:col-span-2 mt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => void navigate(`/clientes/${clienteId}`)}
                disabled={isSubmitting}
                className="h-10 rounded-full border-zinc-700/60 bg-transparent px-5 text-sm text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
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
                  'Salvar veículo'
                )}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
