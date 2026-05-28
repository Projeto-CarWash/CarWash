import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { AlertCircle, ArrowLeft, Car, Check, ChevronDown, Loader2, Search, ShieldX, X } from 'lucide-react';
import { useCallback, useEffect, useRef, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate, useParams } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { veiculoSchema, type VeiculoFormData } from '@/schemas/veiculoSchema';
import { clienteService, type ClienteResumo } from '@/services/clienteService';
import { veiculoService } from '@/services/veiculoService';

import type { ProblemDetails } from '@/types/auth';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'Dados do veículo inválidos. Verifique os campos e tente novamente.',
  401: 'Autenticação obrigatória para executar esta operação.',
  403: 'Você não possui permissão para cadastrar veículos.',
  404: 'Cliente não encontrado para vincular o veículo.',
  409: 'Já existe veículo cadastrado com esta placa.',
  500: 'Não foi possível concluir o cadastro no momento. Tente novamente.',
};

export function NovoVeiculoPage() {
  const { id: urlClienteId = '' } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [carregandoCliente, setCarregandoCliente] = useState(false);
  const [clientes, setClientes] = useState<ClienteResumo[]>([]);
  const [permissaoBloqueada, setPermissaoBloqueada] = useState(false);

  // Autocomplete state
  const [busca, setBusca] = useState('');
  const [isOpen, setIsOpen] = useState(false);
  const [selectedCliente, setSelectedCliente] = useState<{ id: string; nome: string } | null>(null);

  const dropdownRef = useRef<HTMLDivElement>(null);

  const form = useForm<VeiculoFormData>({
    resolver: zodResolver(veiculoSchema),
    mode: 'onChange',
    shouldFocusError: true,
    defaultValues: {
      clienteId: '',
      placa: '',
      modelo: '',
      fabricante: '',
      cor: '',
      observacoes: '',
    },
  });

  // Carrega a lista de todos os clientes ativos
  useEffect(() => {
    let active = true;
    const loadClientes = async () => {
      try {
        const res = await clienteService.listar({ ativo: true, tamanhoPagina: 100 });
        if (active) {
          setClientes(res.itens);
        }
      } catch (err) {
        console.error('Erro ao buscar clientes ativos:', err);
      }
    };
    void loadClientes();
    return () => {
      active = false;
    };
  }, []);

  // Se houver clienteId na URL, pré-seleciona
  useEffect(() => {
    if (!urlClienteId) return;
    let active = true;

    const loadSelected = async () => {
      setCarregandoCliente(true);
      try {
        const c = await clienteService.obterPorId(urlClienteId);
        if (active) {
          setSelectedCliente(c);
          form.setValue('clienteId', urlClienteId, { shouldValidate: true });
          setBusca(c.nome);
        }
      } catch {
        if (active) {
          setGlobalError(HTTP_ERROR_MESSAGES[404]!);
        }
      } finally {
        if (active) {
          setCarregandoCliente(false);
        }
      }
    };
    void loadSelected();
    return () => {
      active = false;
    };
  }, [urlClienteId, form]);

  // Click outside listener para fechar dropdown do autocomplete
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (isOpen && dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false);
        if (selectedCliente) {
          setBusca(selectedCliente.nome);
        } else {
          setBusca('');
        }
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isOpen, selectedCliente]);

  const normalizePlaca = (val: string) => {
    // Apenas letras e números, sem caracteres especiais, uppercase
    return val
      .replace(/[^A-Za-z0-9]/g, '')
      .toUpperCase()
      .slice(0, 7);
  };

  const mapBackendFieldToFormField = (field: string): keyof VeiculoFormData | null => {
    const lower = field.toLowerCase();
    const map: Record<string, keyof VeiculoFormData> = {
      clienteid: 'clienteId',
      placa: 'placa',
      modelo: 'modelo',
      fabricante: 'fabricante',
      cor: 'cor',
      observacoes: 'observacoes',
    };
    return map[lower] ?? null;
  };

  const onSubmit = useCallback(
    async (data: VeiculoFormData) => {
      setGlobalError(null);
      setSuccessMsg(null);
      setPermissaoBloqueada(false);

      try {
        await veiculoService.cadastrar(data.clienteId, data);

        // 201 — Confirma sucesso e atualiza fluxo do cliente
        setSuccessMsg('Veículo cadastrado com sucesso.');
        form.reset();
        setSelectedCliente(null);
        setBusca('');

        setTimeout(() => {
          void navigate(`/clientes/${data.clienteId}`);
        }, 1000);
      } catch (err) {
        if (err instanceof AxiosError) {
          const status = err.response?.status;
          const dataErr = err.response?.data as ProblemDetails | undefined;

          // 409 — Destaca placa e mantém dados digitados
          if (status === 409) {
            setGlobalError(HTTP_ERROR_MESSAGES[409]!);
            form.setError('placa', {
              type: 'manual',
              message: 'Já existe veículo cadastrado com esta placa.',
            });
            form.setFocus('placa');
            return;
          }

          // 400 — Mostra mensagens corretas por campo em cada erro local
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

          // 401 — Redireciona para login
          if (status === 401) {
            setGlobalError(HTTP_ERROR_MESSAGES[401]!);
            setTimeout(() => {
              void navigate('/login');
            }, 1500);
            return;
          }

          // 403 — Exibe bloqueio de permissão
          if (status === 403) {
            setPermissaoBloqueada(true);
            setGlobalError(
              dataErr?.title ?? HTTP_ERROR_MESSAGES[403]!,
            );
            return;
          }

          // 404 — Cliente não encontrado
          if (status === 404) {
            setGlobalError(HTTP_ERROR_MESSAGES[404]!);
            setSelectedCliente(null);
            setBusca('');
            form.setValue('clienteId', '', { shouldValidate: true });
            return;
          }

          // 500 — Mantém formulário e permite nova tentativa
          if (status === 500) {
            setGlobalError(HTTP_ERROR_MESSAGES[500]!);
            return;
          }

          // Outros erros HTTP conhecidos
          const msg = status && status in HTTP_ERROR_MESSAGES ? HTTP_ERROR_MESSAGES[status]! : null;
          if (msg) {
            setGlobalError(msg);
            return;
          }

          // Erros de rede/timeout
          if (err.code === 'ECONNABORTED' || err.code === 'ERR_NETWORK') {
            setGlobalError('Não foi possível contatar o servidor. Verifique sua conexão.');
            return;
          }
        }

        // Fallback — mantém formulário e permite nova tentativa
        setGlobalError(HTTP_ERROR_MESSAGES[500]!);
      }
    },
    [form, navigate],
  );

  const isSubmitting = form.formState.isSubmitting;
  const errors = form.formState.errors;
  const hasErrors = Object.keys(errors).length > 0;
  const isSubmitDisabled = isSubmitting || hasErrors || !form.formState.isValid || permissaoBloqueada;

  // Filtragem dos clientes para o autocomplete local
  const query = busca.trim().toLowerCase();
  const filteredClientes = query
    ? clientes.filter(
        (c) =>
          c.nome.toLowerCase().includes(query) ||
          (c.cpf ? c.cpf.replace(/\D/g, '').includes(query.replace(/\D/g, '')) : false) ||
          (c.cnpj ? c.cnpj.replace(/\D/g, '').includes(query.replace(/\D/g, '')) : false),
      )
    : clientes;

  if (carregandoCliente && !selectedCliente && !globalError) {
    return <div className="px-4 py-8 text-sm text-zinc-500 sm:px-8">Carregando…</div>;
  }

  // Tela de bloqueio de permissão (403)
  if (permissaoBloqueada) {
    return (
      <div className="px-4 py-8 sm:px-8">
        <div className="mb-6 flex items-center gap-3">
          <Button
            type="button"
            variant="outline"
            onClick={() => {
              if (selectedCliente) {
                void navigate(`/clientes/${selectedCliente.id}`);
              } else {
                void navigate('/clientes');
              }
            }}
            className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm text-zinc-300 hover:bg-zinc-800/50 hover:text-zinc-100"
          >
            <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" />
            Voltar
          </Button>
        </div>

        <Card className="border border-red-500/20 bg-red-950/10">
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-red-500/10">
              <ShieldX className="h-7 w-7 text-red-500" />
            </div>
            <h2 className="text-lg font-bold text-red-400">Acesso negado</h2>
            <p className="mt-2 max-w-md text-sm text-zinc-400">
              {globalError ?? HTTP_ERROR_MESSAGES[403]}
            </p>
            <Button
              type="button"
              variant="outline"
              onClick={() => void navigate('/clientes')}
              className="mt-6 h-9 rounded-full border-zinc-700/60 bg-transparent px-5 text-sm text-zinc-300 hover:bg-zinc-800/50"
            >
              Voltar para clientes
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="px-4 py-8 sm:px-8">
      {/* Toast de Sucesso */}
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

      <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
            aria-hidden="true"
          >
            <Car className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-2xl font-bold tracking-tight text-zinc-50">Novo veículo</h1>
            <p className="mt-1 text-sm text-zinc-400">
              Cadastre um veículo para um cliente existente no sistema.
            </p>
          </div>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() => {
            if (selectedCliente) {
              void navigate(`/clientes/${selectedCliente.id}`);
            } else {
              void navigate('/clientes');
            }
          }}
          disabled={isSubmitting}
          className="h-9 w-fit rounded-full border-zinc-700/60 bg-transparent px-4 text-sm text-zinc-300 hover:bg-zinc-800/50 hover:text-zinc-100"
        >
          <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" />
          Voltar
        </Button>
      </div>

      <Card className="border border-zinc-800/60 bg-zinc-900/30">
        <CardHeader>
          <CardTitle className="text-lg text-zinc-100">Dados do veículo</CardTitle>
          <CardDescription className="text-zinc-400">
            Todos os campos são validados conforme as regras de negócio do CarWash.
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
            noValidate
            className="grid grid-cols-1 gap-5 md:grid-cols-2"
            aria-busy={isSubmitting}
          >
            {/* Cliente (Dropdown/Autocomplete) */}
            <div className="flex flex-col gap-2 md:col-span-2 relative" ref={dropdownRef}>
              <Label htmlFor="veiculo-cliente-search" className="text-zinc-300">
                Cliente
              </Label>
              <div className="relative">
                <Search className="absolute left-3 top-3 h-4 w-4 text-zinc-500" />
                <Input
                  id="veiculo-cliente-search"
                  type="text"
                  placeholder="Pesquise o cliente por nome ou CPF/CNPJ..."
                  value={busca}
                  onChange={(e) => {
                    setBusca(e.target.value);
                    setIsOpen(true);
                    setGlobalError(null);
                    if (selectedCliente && e.target.value !== selectedCliente.nome) {
                      setSelectedCliente(null);
                      form.setValue('clienteId', '', { shouldValidate: true });
                    }
                  }}
                  onFocus={() => setIsOpen(true)}
                  aria-invalid={!!errors.clienteId}
                  aria-describedby={errors.clienteId ? 'veiculo-cliente-error' : undefined}
                  className={`h-10 rounded-lg border pl-9 pr-10 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                    errors.clienteId
                      ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                      : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                  }`}
                />
                {selectedCliente ? (
                  <button
                    type="button"
                    onClick={() => {
                      setSelectedCliente(null);
                      setBusca('');
                      form.setValue('clienteId', '', { shouldValidate: true });
                      setGlobalError(null);
                    }}
                    className="absolute right-3 top-3 text-zinc-400 hover:text-zinc-200"
                    aria-label="Limpar cliente selecionado"
                  >
                    <X className="h-4 w-4" />
                  </button>
                ) : (
                  <ChevronDown className="absolute right-3 top-3 h-4 w-4 text-zinc-500 pointer-events-none" />
                )}
              </div>

              {errors.clienteId && (
                <p id="veiculo-cliente-error" role="alert" className="text-xs text-red-400">
                  {errors.clienteId.message}
                </p>
              )}

              {/* Lista flutuante do Autocomplete */}
              {isOpen && (
                <div className="absolute top-[72px] left-0 right-0 z-50 max-h-60 overflow-y-auto rounded-lg border border-zinc-800 bg-zinc-950 shadow-xl">
                  {filteredClientes.length > 0 ? (
                    filteredClientes.map((c) => (
                      <button
                        key={c.id}
                        type="button"
                        onClick={() => {
                          setSelectedCliente(c);
                          setBusca(c.nome);
                          form.setValue('clienteId', c.id, { shouldValidate: true });
                          setIsOpen(false);
                          setGlobalError(null);
                        }}
                        className="flex w-full items-center justify-between px-4 py-2.5 text-left text-sm text-zinc-300 hover:bg-zinc-900 transition-colors"
                      >
                        <div>
                          <p className="font-semibold text-zinc-200">{c.nome}</p>
                          <p className="text-xs text-zinc-500">
                            {c.cpf ? `CPF: ${c.cpf}` : c.cnpj ? `CNPJ: ${c.cnpj}` : 'Sem documento'}
                          </p>
                        </div>
                        {selectedCliente?.id === c.id && <Check className="h-4 w-4 text-red-500" />}
                      </button>
                    ))
                  ) : (
                    <div className="px-4 py-3 text-sm text-zinc-500 italic">
                      Nenhum cliente encontrado
                    </div>
                  )}
                </div>
              )}
            </div>

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
                      maxLength={7}
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
              <Controller
                control={form.control}
                name="fabricante"
                render={({ field, fieldState }) => (
                  <>
                    <Input
                      id="veiculo-fabricante"
                      type="text"
                      placeholder="Ex: Volkswagen"
                      value={field.value}
                      maxLength={80}
                      onChange={field.onChange}
                      onBlur={(e) => {
                        const val = e.target.value.trim();
                        field.onChange(val);
                        field.onBlur();
                      }}
                      ref={field.ref}
                      aria-invalid={!!fieldState.error}
                      aria-describedby={fieldState.error ? 'veiculo-fabricante-error' : undefined}
                      className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                        fieldState.error
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                      }`}
                    />
                    {fieldState.error && (
                      <p
                        id="veiculo-fabricante-error"
                        role="alert"
                        className="text-xs text-red-400"
                      >
                        {fieldState.error.message}
                      </p>
                    )}
                  </>
                )}
              />
            </div>

            {/* Modelo */}
            <div className="flex flex-col gap-2">
              <Label htmlFor="veiculo-modelo" className="text-zinc-300">
                Modelo
              </Label>
              <Controller
                control={form.control}
                name="modelo"
                render={({ field, fieldState }) => (
                  <>
                    <Input
                      id="veiculo-modelo"
                      type="text"
                      placeholder="Ex: Gol 1.0"
                      value={field.value}
                      maxLength={80}
                      onChange={field.onChange}
                      onBlur={(e) => {
                        const val = e.target.value.trim();
                        field.onChange(val);
                        field.onBlur();
                      }}
                      ref={field.ref}
                      aria-invalid={!!fieldState.error}
                      aria-describedby={fieldState.error ? 'veiculo-modelo-error' : undefined}
                      className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                        fieldState.error
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                      }`}
                    />
                    {fieldState.error && (
                      <p id="veiculo-modelo-error" role="alert" className="text-xs text-red-400">
                        {fieldState.error.message}
                      </p>
                    )}
                  </>
                )}
              />
            </div>

            {/* Cor */}
            <div className="flex flex-col gap-2">
              <Label htmlFor="veiculo-cor" className="text-zinc-300">
                Cor
              </Label>
              <Controller
                control={form.control}
                name="cor"
                render={({ field, fieldState }) => (
                  <>
                    <Input
                      id="veiculo-cor"
                      type="text"
                      placeholder="Ex: Preto"
                      value={field.value}
                      maxLength={40}
                      onChange={field.onChange}
                      onBlur={(e) => {
                        const val = e.target.value.trim();
                        field.onChange(val);
                        field.onBlur();
                      }}
                      ref={field.ref}
                      aria-invalid={!!fieldState.error}
                      aria-describedby={fieldState.error ? 'veiculo-cor-error' : undefined}
                      className={`h-10 rounded-lg border px-3 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 ${
                        fieldState.error
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                      }`}
                    />
                    {fieldState.error && (
                      <p id="veiculo-cor-error" role="alert" className="text-xs text-red-400">
                        {fieldState.error.message}
                      </p>
                    )}
                  </>
                )}
              />
            </div>

            {/* Observações */}
            <div className="flex flex-col gap-2 md:col-span-2">
              <div className="flex items-center justify-between">
                <Label htmlFor="veiculo-observacoes" className="text-zinc-300">
                  Observações (Opcional)
                </Label>
                <Controller
                  control={form.control}
                  name="observacoes"
                  render={({ field }) => (
                    <span className="text-[11px] font-semibold text-zinc-500">
                      {field.value ? field.value.length : 0}/500
                    </span>
                  )}
                />
              </div>
              <Controller
                control={form.control}
                name="observacoes"
                render={({ field, fieldState }) => (
                  <>
                    <textarea
                      id="veiculo-observacoes"
                      rows={3}
                      maxLength={500}
                      placeholder="Alguma observação sobre o veículo..."
                      value={field.value}
                      onChange={field.onChange}
                      onBlur={(e) => {
                        const val = e.target.value.trim();
                        field.onChange(val);
                        field.onBlur();
                      }}
                      ref={field.ref}
                      aria-invalid={!!fieldState.error}
                      aria-describedby={fieldState.error ? 'veiculo-observacoes-error' : undefined}
                      className={`w-full rounded-lg border px-3 py-2 text-sm text-zinc-100 placeholder:text-zinc-500 focus-visible:ring-0 resize-none ${
                        fieldState.error
                          ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
                          : 'border-zinc-700/60 bg-zinc-950/40 focus-visible:border-zinc-600'
                      }`}
                    />
                    {fieldState.error && (
                      <p
                        id="veiculo-observacoes-error"
                        role="alert"
                        className="text-xs text-red-400"
                      >
                        {fieldState.error.message}
                      </p>
                    )}
                  </>
                )}
              />
            </div>

            {/* Ações */}
            <div className="flex flex-col-reverse items-stretch gap-3 sm:flex-row sm:items-center sm:justify-end md:col-span-2 mt-4">
              <Button
                type="button"
                variant="outline"
                onClick={() => {
                  if (selectedCliente) {
                    void navigate(`/clientes/${selectedCliente.id}`);
                  } else {
                    void navigate('/clientes');
                  }
                }}
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
