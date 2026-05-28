import { zodResolver } from '@hookform/resolvers/zod';
import { AxiosError } from 'axios';
import { AlertCircle, ArrowLeft, Check, Edit, Loader2, Plus, Search, ShieldX, Wrench, X } from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { formatarDuracao, formatarReais } from '@/lib/format';
import { servicoSchema, type ServicoFormData } from '@/schemas/servicoSchema';
import { servicoService } from '@/services/servicoService';

import type { ProblemDetails } from '@/types/auth';
import type { ServicoResumo } from '@/types/servico';

const HTTP_ERROR_MESSAGES: Record<number, string> = {
  400: 'Dados do serviço inválidos. Verifique os campos e tente novamente.',
  401: 'Autenticação obrigatória para executar esta operação.',
  403: 'Você não possui permissão para gerenciar o catálogo de serviços.',
  409: 'Já existe um serviço cadastrado com este nome.',
  500: 'Não foi possível concluir a operação no momento. Tente novamente.',
};

export function ServicosListaPage() {
  const navigate = useNavigate();

  // State
  const [busca, setBusca] = useState('');
  const [itens, setItens] = useState<ServicoResumo[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erro, setErro] = useState<string | null>(null);

  // Modal / Form state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingServico, setEditingServico] = useState<ServicoResumo | null>(null);
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [successMsg, setSuccessMsg] = useState<string | null>(null);
  const [permissaoBloqueada, setPermissaoBloqueada] = useState(false);

  // Form Setup
  const form = useForm<ServicoFormData>({
    resolver: zodResolver(servicoSchema),
    mode: 'onBlur',
    shouldFocusError: true,
    defaultValues: {
      nome: '',
      preco: '',
      duracaoMin: '',
    },
  });

  // Fetch Services
  const carregarServicos = useCallback(async () => {
    setCarregando(true);
    try {
      const resp = await servicoService.listar();
      setItens(resp.itens);
      setErro(null);
    } catch (err) {
      if (err instanceof AxiosError) {
        if (err.response?.status === 401) {
          void navigate('/login');
          return;
        }
        if (err.response?.status === 403) {
          setPermissaoBloqueada(true);
          return;
        }
      }
      setErro('Não foi possível carregar a lista de serviços.');
    } finally {
      setCarregando(false);
    }
  }, [navigate]);

  useEffect(() => {
    void carregarServicos();
  }, [carregarServicos]);

  // Open modal for Create
  const handleOpenCreate = () => {
    setEditingServico(null);
    setGlobalError(null);
    form.reset({
      nome: '',
      preco: '',
      duracaoMin: '',
    });
    setIsModalOpen(true);
  };

  // Open modal for Edit
  const handleOpenEdit = (servico: ServicoResumo) => {
    setEditingServico(servico);
    setGlobalError(null);
    form.reset({
      nome: servico.nome,
      preco: String(servico.precoBase),
      duracaoMin: String(servico.duracaoMin),
    });
    setIsModalOpen(true);
  };

  // Toggle status (Active / Inactive)
  const handleToggleStatus = async (servico: ServicoResumo) => {
    const novoStatus = !servico.ativo;
    try {
      await servicoService.alterarStatus(servico.id, novoStatus);
      setSuccessMsg(`Serviço ${novoStatus ? 'ativado' : 'desativado'} com sucesso.`);
      setTimeout(() => setSuccessMsg(null), 3000);
      void carregarServicos();
    } catch (err) {
      if (err instanceof AxiosError) {
        if (err.response?.status === 401) {
          void navigate('/login');
          return;
        }
        if (err.response?.status === 403) {
          setGlobalError(HTTP_ERROR_MESSAGES[403]!);
          setTimeout(() => setGlobalError(null), 4000);
          return;
        }
      }
      setGlobalError('Não foi possível alterar o status do serviço.');
      setTimeout(() => setGlobalError(null), 4000);
    }
  };

  // Submit form handler
  const onSubmit = async (data: ServicoFormData) => {
    setGlobalError(null);

    try {
      if (editingServico) {
        // Edit flow
        await servicoService.atualizar(editingServico.id, {
          nome: data.nome.trim(),
          preco: Number(data.preco),
          duracaoMin: Number(data.duracaoMin),
        });
        setSuccessMsg('Serviço atualizado com sucesso.');
      } else {
        // Create flow
        await servicoService.criar({
          nome: data.nome.trim(),
          preco: Number(data.preco),
          duracaoMin: Number(data.duracaoMin),
        });
        setSuccessMsg('Serviço cadastrado com sucesso.');
      }

      setIsModalOpen(false);
      form.reset();
      void carregarServicos();
      setTimeout(() => setSuccessMsg(null), 3000);
    } catch (err) {
      if (err instanceof AxiosError) {
        const status = err.response?.status;
        const dataErr = err.response?.data as ProblemDetails | undefined;

        // 409 — Duplicate Name
        if (status === 409) {
          form.setError('nome', {
            type: 'manual',
            message: 'Já existe um serviço cadastrado com este nome.',
          });
          form.setFocus('nome');
          setGlobalError(HTTP_ERROR_MESSAGES[409]!);
          return;
        }

        // 400 — Validation errors
        if (status === 400 && dataErr?.errors) {
          setGlobalError(dataErr.title ?? HTTP_ERROR_MESSAGES[400]!);
          for (const [field, messages] of Object.entries(dataErr.errors)) {
            const fieldName = field.toLowerCase();
            if (fieldName === 'nome') {
              form.setError('nome', { message: messages[0] });
              form.setFocus('nome');
            } else if (fieldName === 'preco' || fieldName === 'precobase') {
              form.setError('preco', { message: messages[0] });
            } else if (fieldName === 'duracaomin') {
              form.setError('duracaoMin', { message: messages[0] });
            }
          }
          return;
        }

        // 401 — Unauthorized
        if (status === 401) {
          setGlobalError(HTTP_ERROR_MESSAGES[401]!);
          setTimeout(() => {
            void navigate('/login');
          }, 1500);
          return;
        }

        // 403 — Forbidden
        if (status === 403) {
          setGlobalError(HTTP_ERROR_MESSAGES[403]!);
          setIsModalOpen(false);
          setPermissaoBloqueada(true);
          return;
        }

        // 500 — Keep form state intact
        if (status === 500) {
          setGlobalError(HTTP_ERROR_MESSAGES[500]!);
          return;
        }
      }

      setGlobalError(HTTP_ERROR_MESSAGES[500]!);
    }
  };

  // Search filter
  const queryBusca = busca.trim().toLowerCase();
  const servicosFiltrados = itens.filter((s) => s.nome.toLowerCase().includes(queryBusca));

  const isSubmitting = form.formState.isSubmitting;

  // 403 Unauthorized UI block
  if (permissaoBloqueada) {
    return (
      <div className="px-8 py-8">
        <div className="mb-6 flex items-center gap-3">
          <Button
            type="button"
            variant="outline"
            onClick={() => void navigate('/dashboard')}
            className="h-9 rounded-full border-zinc-700/60 bg-transparent px-4 text-sm text-zinc-300 hover:bg-zinc-800/50 hover:text-zinc-100"
          >
            <ArrowLeft className="mr-1 h-4 w-4" aria-hidden="true" />
            Voltar para o Painel
          </Button>
        </div>

        <Card className="border border-red-500/20 bg-red-950/10">
          <CardContent className="flex flex-col items-center justify-center py-12 text-center">
            <div className="mb-4 flex h-14 w-14 items-center justify-center rounded-full bg-red-500/10">
              <ShieldX className="h-7 w-7 text-red-500" />
            </div>
            <h2 className="text-lg font-bold text-red-400">Acesso negado</h2>
            <p className="mt-2 max-w-md text-sm text-zinc-400">
              Você não possui permissão para gerenciar o catálogo de serviços.
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="px-8 py-8">
      {/* Success Notification */}
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

      {/* Header */}
      <div className="mb-6 flex items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <span
            className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
            aria-hidden="true"
          >
            <Wrench className="h-5 w-5" />
          </span>
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-zinc-50">Catálogo de Serviços</h1>
            <p className="mt-1 text-sm text-zinc-500">
              {itens.length === 0 ? 'Nenhum serviço cadastrado' : `${itens.length} serviço(s) no total`}
            </p>
          </div>
        </div>
        <Button
          type="button"
          onClick={handleOpenCreate}
          className="h-10 rounded-full bg-red-600 px-5 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700"
        >
          <Plus className="mr-1 h-4 w-4" /> Novo serviço
        </Button>
      </div>

      {/* Filter search bar */}
      <div className="mb-4 flex items-center gap-3">
        <div className="relative max-w-md flex-1">
          <Search
            className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500"
            aria-hidden="true"
          />
          <Input
            type="text"
            value={busca}
            onChange={(e) => setBusca(e.target.value)}
            placeholder="Buscar serviço por nome…"
            className="h-10 rounded-xl border-zinc-700/60 bg-zinc-900/50 pl-9 text-sm text-zinc-200 placeholder:text-zinc-600 focus-visible:ring-0"
          />
        </div>
      </div>

      {/* Global Listing error alert */}
      {erro && (
        <div
          role="alert"
          className="mb-4 rounded-xl border border-red-500/30 bg-red-950/30 px-4 py-3 text-sm text-red-400"
        >
          {erro}
        </div>
      )}

      {/* Services Table */}
      <div className="overflow-hidden rounded-2xl border border-zinc-800/60 bg-zinc-900/30">
        <table className="w-full text-left text-sm">
          <thead className="border-b border-zinc-800/60 bg-zinc-900/60 text-[10px] font-bold uppercase tracking-[0.2em] text-zinc-500">
            <tr>
              <th className="px-6 py-3">Nome</th>
              <th className="px-6 py-3">Preço</th>
              <th className="px-6 py-3">Duração Estimada</th>
              <th className="px-6 py-3">Status</th>
              <th className="px-6 py-3 text-right">Ações</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-zinc-800/60">
            {carregando && (
              <tr>
                <td colSpan={5} className="px-6 py-8 text-center text-zinc-500">
                  Carregando catálogo de serviços…
                </td>
              </tr>
            )}
            {!carregando && servicosFiltrados.length === 0 && (
              <tr>
                <td colSpan={5} className="px-6 py-8 text-center text-zinc-500">
                  Nenhum serviço encontrado.
                </td>
              </tr>
            )}
            {!carregando &&
              servicosFiltrados.map((s) => (
                <tr key={s.id} className="text-zinc-200 hover:bg-zinc-800/30">
                  <td className="px-6 py-3 font-semibold text-zinc-100">
                    {s.nome}
                  </td>
                  <td className="px-6 py-3 tabular-nums text-zinc-400">
                    {formatarReais(s.precoBase)}
                  </td>
                  <td className="px-6 py-3 tabular-nums text-zinc-400">
                    {formatarDuracao(s.duracaoMin)}
                  </td>
                  <td className="px-6 py-3">
                    <span
                      className={
                        s.ativo
                          ? 'rounded-full bg-green-500/10 px-2.5 py-1 text-[10px] font-bold tracking-[0.15em] text-green-400'
                          : 'rounded-full bg-zinc-700/40 px-2.5 py-1 text-[10px] font-bold tracking-[0.15em] text-zinc-500'
                      }
                    >
                      {s.ativo ? 'ATIVO' : 'INATIVO'}
                    </span>
                  </td>
                  <td className="px-6 py-3">
                    <div className="flex items-center justify-end gap-4">
                      {/* Active Status toggle switch */}
                      <button
                        type="button"
                        onClick={() => handleToggleStatus(s)}
                        title={s.ativo ? 'Desativar serviço' : 'Ativar serviço'}
                        aria-label={s.ativo ? 'Desativar serviço' : 'Ativar serviço'}
                        className={`relative inline-flex h-5 w-10 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-red-500/50 ${
                          s.ativo ? 'bg-red-600' : 'bg-zinc-700'
                        }`}
                      >
                        <span
                          className={`pointer-events-none inline-block h-4 w-4 transform rounded-full bg-white shadow ring-0 transition duration-200 ease-in-out ${
                            s.ativo ? 'translate-x-5' : 'translate-x-0'
                          }`}
                        />
                      </button>

                      {/* Edit action */}
                      <button
                        type="button"
                        onClick={() => handleOpenEdit(s)}
                        className="rounded p-1 text-zinc-400 hover:bg-zinc-800 hover:text-zinc-200"
                        title="Editar serviço"
                      >
                        <Edit className="h-4 w-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
          </tbody>
        </table>
      </div>

      {/* Creation / Editing Modal */}
      {isModalOpen && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm p-4"
          role="dialog"
          aria-modal="true"
        >
          <div className="relative w-full max-w-md rounded-2xl border border-zinc-800 bg-zinc-950 p-6 shadow-2xl animate-in zoom-in-95 duration-150">
            {/* Close modal button */}
            <button
              type="button"
              onClick={() => setIsModalOpen(false)}
              className="absolute right-4 top-4 rounded-lg p-1.5 text-zinc-500 hover:bg-zinc-900 hover:text-zinc-300"
              aria-label="Fechar modal"
            >
              <X className="h-4 w-4" />
            </button>

            {/* Modal Header */}
            <div className="mb-6 flex items-center gap-3">
              <span className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500">
                <Wrench className="h-5 w-5" />
              </span>
              <div>
                <h2 className="text-xl font-bold text-zinc-50">
                  {editingServico ? 'Editar Serviço' : 'Novo Serviço'}
                </h2>
                <p className="text-xs text-zinc-500">
                  {editingServico ? 'Edite os valores do serviço selecionado.' : 'Adicione um novo serviço ao catálogo.'}
                </p>
              </div>
            </div>

            {/* Global form error */}
            {globalError && (
              <div
                role="alert"
                className="mb-4 flex items-start gap-2.5 rounded-xl border border-red-500/30 bg-red-950/20 px-4 py-3"
              >
                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" />
                <p className="text-xs font-medium text-red-400">{globalError}</p>
              </div>
            )}

            {/* Modal Form */}
            <form onSubmit={form.handleSubmit(onSubmit)} noValidate className="space-y-4">
              {/* Nome */}
              <div className="flex flex-col gap-2">
                <Label htmlFor="servico-nome" className="text-zinc-300">
                  Nome do serviço
                </Label>
                <Input
                  id="servico-nome"
                  type="text"
                  placeholder="Ex: Lavagem Completa"
                  aria-invalid={!!form.formState.errors.nome}
                  aria-describedby={form.formState.errors.nome ? 'servico-nome-error' : undefined}
                  className="h-10 rounded-lg border-zinc-700/60 bg-zinc-900/20 px-3 text-sm text-zinc-100 placeholder:text-zinc-500"
                  {...form.register('nome')}
                />
                {form.formState.errors.nome && (
                  <p id="servico-nome-error" role="alert" className="text-xs text-red-400">
                    {form.formState.errors.nome.message}
                  </p>
                )}
              </div>

              {/* Preco */}
              <div className="flex flex-col gap-2">
                <Label htmlFor="servico-preco" className="text-zinc-300">
                  Preço base (R$)
                </Label>
                <Input
                  id="servico-preco"
                  type="text"
                  placeholder="0.00"
                  aria-invalid={!!form.formState.errors.preco}
                  aria-describedby={form.formState.errors.preco ? 'servico-preco-error' : undefined}
                  className="h-10 rounded-lg border-zinc-700/60 bg-zinc-900/20 px-3 text-sm text-zinc-100 placeholder:text-zinc-500"
                  {...form.register('preco')}
                />
                {form.formState.errors.preco && (
                  <p id="servico-preco-error" role="alert" className="text-xs text-red-400">
                    {form.formState.errors.preco.message}
                  </p>
                )}
              </div>

              {/* Duracao */}
              <div className="flex flex-col gap-2">
                <Label htmlFor="servico-duracao" className="text-zinc-300">
                  Duração estimada (minutos)
                </Label>
                <Input
                  id="servico-duracao"
                  type="text"
                  placeholder="Ex: 45"
                  aria-invalid={!!form.formState.errors.duracaoMin}
                  aria-describedby={form.formState.errors.duracaoMin ? 'servico-duracao-error' : undefined}
                  className="h-10 rounded-lg border-zinc-700/60 bg-zinc-900/20 px-3 text-sm text-zinc-100 placeholder:text-zinc-500"
                  {...form.register('duracaoMin')}
                />
                {form.formState.errors.duracaoMin && (
                  <p id="servico-duracao-error" role="alert" className="text-xs text-red-400">
                    {form.formState.errors.duracaoMin.message}
                  </p>
                )}
              </div>

              {/* Actions */}
              <div className="mt-6 flex items-center justify-end gap-3">
                <Button
                  type="button"
                  variant="outline"
                  onClick={() => setIsModalOpen(false)}
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
                    'Salvar'
                  )}
                </Button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
