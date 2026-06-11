/* eslint-disable jsx-a11y/no-autofocus */
import { useQueryClient } from '@tanstack/react-query';
import { AlertCircle, CalendarRange, CalendarSearch, Loader2, RotateCw, X } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useClientesParaAgendamento, useFiliais } from '@/hooks/useAgendamentoQueries';
import { useAgenda, validarFiltrosAgenda } from '@/hooks/useAgendaQuery';
import { tratarErroApi } from '@/lib/apiError';
import { agendamentoService } from '@/services/agendamentoService';

import { classesStatus, rotuloStatus } from './agendaFormat';
import { AgendaItemDetalhadoCard } from './AgendaItemDetalhadoCard';
import { AgendaItemSimplesRow } from './AgendaItemSimplesRow';
import { AgendaSlotGroup } from './AgendaSlotGroup';

import type {
  AgendaFiltros,
  AgendaFormato,
  AgendaItemDetalhado,
  AgendaItemSimples,
  AgendaStatus,
  ConsultarAgendaResponse,
} from '@/types/agenda';

/**
 * Agrupa itens da agenda pelo par (inicio, fim) para renderizar
 * múltiplos agendamentos no mesmo slot de horário (RF008.1).
 */
function agruparPorHorario<T extends { inicio: string; fim: string }>(
  itens: T[],
): { chave: string; inicio: string; fim: string; itens: T[] }[] {
  const mapa = new Map<string, { inicio: string; fim: string; itens: T[] }>();

  for (const item of itens) {
    const chave = `${item.inicio}|${item.fim}`;
    const grupo = mapa.get(chave);
    if (grupo) {
      grupo.itens.push(item);
    } else {
      mapa.set(chave, { inicio: item.inicio, fim: item.fim, itens: [item] });
    }
  }

  return Array.from(mapa.entries()).map(([chave, grupo]) => ({
    chave,
    ...grupo,
  }));
}

/** Opções de status para o seletor de filtro. */
const STATUS_OPCOES: { valor: AgendaStatus; rotulo: string }[] = [
  { valor: 'AGENDADO', rotulo: 'Agendado' },
  { valor: 'EM_ANDAMENTO', rotulo: 'Em andamento' },
  { valor: 'CONCLUIDO', rotulo: 'Concluído' },
  { valor: 'CANCELADO', rotulo: 'Cancelado' },
];

/** Valor `datetime-local` para "agora" arredondado ao minuto. */
function agoraLocal(): string {
  const d = new Date();
  d.setSeconds(0, 0);
  const offset = d.getTimezoneOffset() * 60_000;
  return new Date(d.getTime() - offset).toISOString().slice(0, 16);
}

/** Valor `datetime-local` somando `dias` ao instante atual. */
function emDiasLocal(dias: number): string {
  const d = new Date();
  d.setSeconds(0, 0);
  d.setDate(d.getDate() + dias);
  const offset = d.getTimezoneOffset() * 60_000;
  return new Date(d.getTime() - offset).toISOString().slice(0, 16);
}

/** Type guard: item detalhado tem o objeto aninhado `cliente`. */
function ehDetalhado(item: AgendaItemSimples | AgendaItemDetalhado): item is AgendaItemDetalhado {
  return 'cliente' in item;
}

const classeCampo =
  'h-10 rounded-lg border border-border bg-white px-3 text-sm text-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none disabled:opacity-50 dark:border-zinc-700/60 dark:bg-zinc-950/40 dark:text-zinc-100';

/**
 * Tela de visualização de agenda (RF009 / card 132).
 *
 * <p>Consome `GET /api/v1/agenda` com alternância entre os formatos `simples`
 * e `detalhado`, filtros de período/filial/cliente/responsável/status e os
 * estados de carregando, erro e vazio. A janela de 31 dias e `inicio < fim`
 * são validadas no cliente como defesa de UX — o backend revalida.</p>
 */
export function AgendaPage() {
  const [formato, setFormato] = useState<AgendaFormato>('simples');

  // Item selecionado para exibição em modal de detalhe (RF008.1).
  const [itemSelecionado, setItemSelecionado] = useState<
    AgendaItemDetalhado | AgendaItemSimples | null
  >(null);

  const navigate = useNavigate();
  const queryClient = useQueryClient();

  // Estados de cancelamento e edição
  const [cancelModalOpen, setCancelModalOpen] = useState(false);
  const [cancelItem, setCancelItem] = useState<AgendaItemSimples | AgendaItemDetalhado | null>(
    null,
  );
  const [motivoCancelamento, setMotivoCancelamento] = useState('');
  const [motivoError, setMotivoError] = useState<string | null>(null);
  const [cancelLoading, setCancelLoading] = useState(false);

  const [editModalOpen, setEditModalOpen] = useState(false);
  const [editItem, setEditItem] = useState<AgendaItemSimples | AgendaItemDetalhado | null>(null);
  const [observacoesEdit, setObservacoesEdit] = useState('');
  const [editLoading, setEditLoading] = useState(false);
  const [editError, setEditError] = useState<string | null>(null);

  const [toast, setToast] = useState<{ tipo: 'sucesso' | 'erro'; mensagem: string } | null>(null);

  useEffect(() => {
    let timer: ReturnType<typeof setTimeout> | undefined;
    if (toast) {
      timer = setTimeout(() => setToast(null), 5000);
    }
    return () => {
      if (timer) clearTimeout(timer);
    };
  }, [toast]);

  function handleEditarClick(item: AgendaItemSimples | AgendaItemDetalhado) {
    if (item.status !== 'AGENDADO') {
      const msg =
        item.status === 'CONCLUIDO'
          ? 'Agendamento finalizado não pode ser editado.'
          : 'Agendamento no status atual não permite edição.';
      setToast({ tipo: 'erro', mensagem: msg });
      return;
    }
    setEditItem(item);
    setObservacoesEdit(ehDetalhado(item) ? (item.observacoes ?? '') : '');
    setEditError(null);
    setEditModalOpen(true);
  }

  function handleCancelarClick(item: AgendaItemSimples | AgendaItemDetalhado) {
    if (item.status !== 'AGENDADO' && item.status !== 'EM_ANDAMENTO') {
      setToast({ tipo: 'erro', mensagem: 'Este agendamento não pode ser cancelado.' });
      return;
    }
    setCancelItem(item);
    setMotivoCancelamento('');
    setMotivoError(null);
    setCancelModalOpen(true);
  }

  async function handleConfirmarCancelamento() {
    if (!cancelItem) return;
    const motivo = motivoCancelamento.trim();
    if (!motivo) {
      setMotivoError('O motivo do cancelamento é obrigatório.');
      return;
    }
    if (motivo.length < 5) {
      setMotivoError('O motivo do cancelamento deve ter pelo menos 5 caracteres.');
      return;
    }
    if (motivo.length > 500) {
      setMotivoError('O motivo do cancelamento não pode ultrapassar 500 caracteres.');
      return;
    }

    setCancelLoading(true);
    setMotivoError(null);

    try {
      await agendamentoService.cancelar(cancelItem.agendamentoId, motivo);

      setCancelModalOpen(false);

      queryClient.setQueriesData<ConsultarAgendaResponse<AgendaItemSimples | AgendaItemDetalhado>>(
        { queryKey: ['agenda'] },
        (oldData: ConsultarAgendaResponse<AgendaItemSimples | AgendaItemDetalhado> | undefined) => {
          if (!oldData) return oldData;
          return {
            ...oldData,
            data: oldData.data.map((item: AgendaItemSimples | AgendaItemDetalhado) => {
              if (item.agendamentoId === cancelItem.agendamentoId) {
                return { ...item, status: 'CANCELADO' };
              }
              return item;
            }),
          };
        },
      );

      if (itemSelecionado?.agendamentoId === cancelItem.agendamentoId) {
        setItemSelecionado((prev) => (prev ? { ...prev, status: 'CANCELADO' } : null));
      }

      setToast({ tipo: 'sucesso', mensagem: 'Agendamento cancelado com sucesso.' });
      setCancelItem(null);
    } catch (err: unknown) {
      const erro = tratarErroApi(err);
      if (erro.status === 401) {
        setToast({
          tipo: 'erro',
          mensagem: 'Autenticação obrigatória para executar esta operação.',
        });
        void navigate('/login');
        setCancelModalOpen(false);
      } else if (erro.status === 403) {
        setToast({
          tipo: 'erro',
          mensagem: 'Você não possui permissão para cancelar ou editar agendamentos.',
        });
        setCancelModalOpen(false);
      } else if (erro.status === 404) {
        setToast({ tipo: 'erro', mensagem: 'Agendamento não encontrado.' });
        setCancelModalOpen(false);
        if (itemSelecionado?.agendamentoId === cancelItem.agendamentoId) {
          setItemSelecionado(null);
        }
      } else if (erro.status === 409) {
        setToast({
          tipo: 'erro',
          mensagem: erro.mensagem ?? 'Agendamento no status atual não permite a operação.',
        });
      } else if (erro.status === 400) {
        const msgErro =
          erro.errorsPorCampo.motivoCancelamento ??
          'Dados inválidos para a operação. Verifique e tente novamente.';
        setMotivoError(msgErro);
      } else {
        setToast({
          tipo: 'erro',
          mensagem: 'Não foi possível concluir a operação no momento. Tente novamente.',
        });
      }
    } finally {
      setCancelLoading(false);
    }
  }

  async function handleConfirmarEdicao() {
    if (!editItem) return;
    const obs = observacoesEdit.trim();

    setEditLoading(true);
    setEditError(null);

    try {
      await agendamentoService.atualizar(editItem.agendamentoId, { observacoes: obs || null });

      setEditModalOpen(false);

      queryClient.setQueriesData<ConsultarAgendaResponse<AgendaItemSimples | AgendaItemDetalhado>>(
        { queryKey: ['agenda'] },
        (oldData: ConsultarAgendaResponse<AgendaItemSimples | AgendaItemDetalhado> | undefined) => {
          if (!oldData) return oldData;
          return {
            ...oldData,
            data: oldData.data.map((item: AgendaItemSimples | AgendaItemDetalhado) => {
              if (item.agendamentoId === editItem.agendamentoId) {
                return {
                  ...item,
                  ...(ehDetalhado(item) ? { observacoes: obs || null } : {}),
                };
              }
              return item;
            }),
          };
        },
      );

      if (itemSelecionado?.agendamentoId === editItem.agendamentoId) {
        setItemSelecionado((prev) => (prev ? { ...prev, observacoes: obs || null } : null));
      }

      setToast({ tipo: 'sucesso', mensagem: 'Agendamento atualizado com sucesso.' });
      setEditItem(null);
    } catch (err: unknown) {
      const erro = tratarErroApi(err);
      if (erro.status === 401) {
        setToast({
          tipo: 'erro',
          mensagem: 'Autenticação obrigatória para executar esta operação.',
        });
        void navigate('/login');
        setEditModalOpen(false);
      } else if (erro.status === 403) {
        setToast({
          tipo: 'erro',
          mensagem: 'Você não possui permissão para cancelar ou editar agendamentos.',
        });
        setEditModalOpen(false);
      } else if (erro.status === 404) {
        setToast({ tipo: 'erro', mensagem: 'Agendamento não encontrado.' });
        setEditModalOpen(false);
        if (itemSelecionado?.agendamentoId === editItem.agendamentoId) {
          setItemSelecionado(null);
        }
      } else if (erro.status === 409) {
        const msg =
          editItem.status === 'CONCLUIDO'
            ? 'Agendamento finalizado não pode ser editado.'
            : 'Agendamento no status atual não permite edição.';
        setToast({ tipo: 'erro', mensagem: msg });
        setEditModalOpen(false);
      } else if (erro.status === 400) {
        setEditError('Dados inválidos para a operação. Verifique e tente novamente.');
      } else {
        setToast({
          tipo: 'erro',
          mensagem: 'Não foi possível concluir a operação no momento. Tente novamente.',
        });
      }
    } finally {
      setEditLoading(false);
    }
  }

  // Fecha o modal ao pressionar Escape (RF008.1)
  useEffect(() => {
    if (!itemSelecionado) return;
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') {
        setItemSelecionado(null);
      }
    }
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [itemSelecionado]);

  // Filtros "em edição" — só viram filtros aplicados ao clicar em "Buscar".
  const [periodoInicio, setPeriodoInicio] = useState<string>(agoraLocal);
  const [periodoFim, setPeriodoFim] = useState<string>(() => emDiasLocal(7));
  const [filialId, setFilialId] = useState('');
  const [filialManual, setFilialManual] = useState('');
  const [clienteId, setClienteId] = useState('');
  const [usuarioId, setUsuarioId] = useState('');
  const [status, setStatus] = useState<AgendaStatus | ''>('');
  const [erroLocal, setErroLocal] = useState<string | null>(null);

  const [filtrosAplicados, setFiltrosAplicados] = useState<AgendaFiltros | null>(null);

  const filiaisQuery = useFiliais();
  const clientesQuery = useClientesParaAgendamento('');

  const filiais = useMemo(() => filiaisQuery.data?.itens ?? [], [filiaisQuery.data]);
  const clientes = useMemo(() => clientesQuery.data?.itens ?? [], [clientesQuery.data]);
  const filiaisIndisponivel = filiaisQuery.isError;

  // A query da agenda usa sempre os filtros aplicados + o formato atual,
  // de modo que alternar o formato re-busca sem precisar clicar em "Buscar".
  const filtrosQuery: AgendaFiltros = useMemo(
    () =>
      filtrosAplicados
        ? { ...filtrosAplicados, formato }
        : { formato, inicio: '', fim: '', filialId: '' },
    [filtrosAplicados, formato],
  );

  const agendaQuery = useAgenda(filtrosQuery);

  function montarFiltros(): AgendaFiltros {
    const filialEscolhida = filiaisIndisponivel ? filialManual.trim() : filialId;
    return {
      formato,
      inicio: periodoInicio,
      fim: periodoFim,
      filialId: filialEscolhida,
      clienteId: clienteId || undefined,
      usuarioId: usuarioId.trim() || undefined,
      status: status || undefined,
    };
  }

  function handleBuscar(e: React.FormEvent) {
    e.preventDefault();
    const filtros = montarFiltros();
    const { valido, motivo } = validarFiltrosAgenda(filtros);

    if (!filtros.filialId) {
      setErroLocal('Selecione (ou informe) a filial para consultar a agenda.');
      setFiltrosAplicados(null);
      return;
    }
    if (!valido) {
      setErroLocal(motivo ?? 'Informe o período de início e fim.');
      setFiltrosAplicados(null);
      return;
    }
    setErroLocal(null);
    setFiltrosAplicados(filtros);
  }

  const itens = agendaQuery.data?.data ?? [];
  const mensagemBackend = agendaQuery.data?.message;
  const consultou = filtrosAplicados !== null;
  const erroApi = agendaQuery.isError ? tratarErroApi(agendaQuery.error) : null;

  return (
    <div className="px-4 py-6 sm:px-8 sm:py-8">
      {/* Cabeçalho */}
      <div className="mb-6 flex items-center gap-3">
        <span
          className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
          aria-hidden="true"
        >
          <CalendarRange className="h-5 w-5" />
        </span>
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground dark:text-zinc-50">
            Agenda
          </h1>
          <p className="mt-1 text-sm text-muted-foreground dark:text-zinc-400">
            Visualize os agendamentos em formato simples ou detalhado (RF009).
          </p>
        </div>
      </div>

      {/* Barra de filtros */}
      <form
        onSubmit={handleBuscar}
        aria-label="Filtros da agenda"
        className="mb-6 rounded-2xl border border-border bg-white/60 p-4 dark:border-zinc-800/60 dark:bg-zinc-900/30"
      >
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {/* Período início */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ag-inicio" className="text-muted-foreground dark:text-zinc-300">
              Início do período
            </Label>
            <Input
              id="ag-inicio"
              type="datetime-local"
              value={periodoInicio}
              onChange={(e) => setPeriodoInicio(e.target.value)}
              className="h-10 rounded-lg dark:[color-scheme:dark]"
            />
          </div>

          {/* Período fim */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ag-fim" className="text-muted-foreground dark:text-zinc-300">
              Fim do período
            </Label>
            <Input
              id="ag-fim"
              type="datetime-local"
              value={periodoFim}
              onChange={(e) => setPeriodoFim(e.target.value)}
              className="h-10 rounded-lg dark:[color-scheme:dark]"
            />
          </div>

          {/* Filial */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ag-filial" className="text-muted-foreground dark:text-zinc-300">
              Filial
            </Label>
            {filiaisIndisponivel ? (
              <>
                <Input
                  id="ag-filial"
                  type="text"
                  value={filialManual}
                  onChange={(e) => setFilialManual(e.target.value)}
                  placeholder="Informe o ID da filial (UUID)"
                  className="h-10 rounded-lg"
                />
                <p className="text-xs text-amber-600 dark:text-amber-400">
                  Catálogo de filiais indisponível — informe o ID manualmente.
                </p>
              </>
            ) : (
              <select
                id="ag-filial"
                value={filialId}
                onChange={(e) => setFilialId(e.target.value)}
                disabled={filiaisQuery.isLoading}
                className={classeCampo}
              >
                <option value="">
                  {filiaisQuery.isLoading ? 'Carregando filiais…' : 'Selecione uma filial'}
                </option>
                {filiais.map((f) => (
                  <option key={f.id} value={f.id}>
                    {f.nome}
                  </option>
                ))}
              </select>
            )}
          </div>

          {/* Cliente (opcional) */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ag-cliente" className="text-muted-foreground dark:text-zinc-300">
              Cliente <span className="text-muted-foreground">(opcional)</span>
            </Label>
            <select
              id="ag-cliente"
              value={clienteId}
              onChange={(e) => setClienteId(e.target.value)}
              disabled={clientesQuery.isLoading}
              className={classeCampo}
            >
              <option value="">Todos os clientes</option>
              {clientes.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nome}
                </option>
              ))}
            </select>
            {clientesQuery.isError && (
              <p className="text-xs text-amber-600 dark:text-amber-400">
                Não foi possível carregar a lista de clientes.
              </p>
            )}
          </div>

          {/* Responsável (opcional) */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ag-responsavel" className="text-muted-foreground dark:text-zinc-300">
              Responsável <span className="text-muted-foreground">(opcional)</span>
            </Label>
            <Input
              id="ag-responsavel"
              type="text"
              value={usuarioId}
              onChange={(e) => setUsuarioId(e.target.value)}
              placeholder="ID do responsável (UUID)"
              className="h-10 rounded-lg"
            />
          </div>

          {/* Status (opcional) */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ag-status" className="text-muted-foreground dark:text-zinc-300">
              Status <span className="text-muted-foreground">(opcional)</span>
            </Label>
            <select
              id="ag-status"
              value={status}
              onChange={(e) => setStatus(e.target.value as AgendaStatus | '')}
              className={classeCampo}
            >
              <option value="">Todos os status</option>
              {STATUS_OPCOES.map((s) => (
                <option key={s.valor} value={s.valor}>
                  {s.rotulo}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Toggle de formato + ação */}
        <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
          <div
            role="group"
            aria-label="Formato de visualização"
            className="inline-flex rounded-full border border-border p-0.5 dark:border-zinc-700/60"
          >
            <button
              type="button"
              aria-pressed={formato === 'simples'}
              onClick={() => setFormato('simples')}
              className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
                formato === 'simples'
                  ? 'bg-red-600 text-white'
                  : 'text-muted-foreground hover:text-foreground dark:hover:text-foreground'
              }`}
            >
              Simples
            </button>
            <button
              type="button"
              aria-pressed={formato === 'detalhado'}
              onClick={() => setFormato('detalhado')}
              className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
                formato === 'detalhado'
                  ? 'bg-red-600 text-white'
                  : 'text-muted-foreground hover:text-foreground dark:hover:text-foreground'
              }`}
            >
              Detalhado
            </button>
          </div>

          <div className="flex items-center gap-2">
            {consultou && (
              <Button
                type="button"
                variant="outline"
                onClick={() => void agendaQuery.refetch()}
                disabled={agendaQuery.isFetching}
                className="h-10 rounded-full px-4 text-sm"
              >
                <RotateCw
                  className={`mr-1 h-4 w-4 ${agendaQuery.isFetching ? 'animate-spin' : ''}`}
                  aria-hidden="true"
                />
                Atualizar
              </Button>
            )}
            <Button
              type="submit"
              className="h-10 rounded-full bg-red-600 px-6 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700"
            >
              <CalendarSearch className="mr-1 h-4 w-4" aria-hidden="true" />
              Buscar agenda
            </Button>
          </div>
        </div>

        {/* Erro de validação local (UX) */}
        {erroLocal && (
          <div
            role="alert"
            aria-live="assertive"
            className="mt-4 flex items-start gap-3 rounded-xl border border-red-500/30 bg-red-50 px-4 py-3 dark:bg-red-950/30"
          >
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
            <p className="text-sm font-medium text-red-600 dark:text-red-400">{erroLocal}</p>
          </div>
        )}
      </form>

      {/* Resultados */}
      <section aria-label="Resultados da agenda" aria-busy={agendaQuery.isFetching}>
        {!consultou && (
          <EstadoVazio
            titulo="Defina os filtros e busque a agenda"
            descricao="Informe o período e a filial e clique em “Buscar agenda” para visualizar os agendamentos."
          />
        )}

        {consultou && agendaQuery.isLoading && (
          <div
            role="status"
            className="flex items-center justify-center gap-2 rounded-2xl border border-border bg-white/40 px-4 py-16 text-sm text-muted-foreground dark:border-zinc-800/60 dark:bg-zinc-900/20 dark:text-zinc-400"
          >
            <Loader2 className="h-5 w-5 animate-spin text-red-500" aria-hidden="true" />
            Carregando agenda…
          </div>
        )}

        {consultou && erroApi && (
          <div
            role="alert"
            aria-live="assertive"
            className="rounded-2xl border border-red-500/30 bg-red-50 px-4 py-8 text-center dark:bg-red-950/30"
          >
            <AlertCircle className="mx-auto mb-2 h-6 w-6 text-red-500" aria-hidden="true" />
            <p className="text-sm font-medium text-red-600 dark:text-red-400">{erroApi.mensagem}</p>
            {Object.entries(erroApi.errorsPorCampo).length > 0 && (
              <ul className="mt-2 space-y-0.5 text-xs text-red-500/80">
                {Object.entries(erroApi.errorsPorCampo).map(([campo, msg]) => (
                  <li key={campo}>
                    <strong>{campo}:</strong> {msg}
                  </li>
                ))}
              </ul>
            )}
            <Button
              type="button"
              variant="outline"
              onClick={() => void agendaQuery.refetch()}
              className="mt-4 h-9 rounded-full px-4 text-sm"
            >
              Tentar novamente
            </Button>
          </div>
        )}

        {consultou && agendaQuery.isSuccess && itens.length === 0 && (
          <EstadoVazio
            titulo="Nenhum evento encontrado"
            descricao={mensagemBackend ?? 'Nenhum evento encontrado para o período selecionado.'}
          />
        )}

        {consultou && agendaQuery.isSuccess && itens.length > 0 && (
          <>
            <p className="mb-3 text-xs text-muted-foreground dark:text-zinc-400">
              {itens.length} agendamento(s) no período.
            </p>
            {formato === 'simples' ? (
              <div className="space-y-4">
                {agruparPorHorario(
                  itens.filter((i): i is AgendaItemSimples => !ehDetalhado(i)),
                ).map((grupo) => (
                  <AgendaSlotGroup
                    key={grupo.chave}
                    inicio={grupo.inicio}
                    fim={grupo.fim}
                    quantidade={grupo.itens.length}
                    modoGrade={false}
                  >
                    <ul className="space-y-2">
                      {grupo.itens.map((item) => (
                        <AgendaItemSimplesRow
                          key={item.agendamentoId}
                          item={item}
                          onClick={(i) => setItemSelecionado(i)}
                          onEditar={handleEditarClick}
                          onCancelar={handleCancelarClick}
                        />
                      ))}
                    </ul>
                  </AgendaSlotGroup>
                ))}
              </div>
            ) : (
              <div className="space-y-4">
                {agruparPorHorario(itens.filter(ehDetalhado)).map((grupo) => (
                  <AgendaSlotGroup
                    key={grupo.chave}
                    inicio={grupo.inicio}
                    fim={grupo.fim}
                    quantidade={grupo.itens.length}
                    modoGrade={true}
                  >
                    {grupo.itens.map((item) => (
                      <AgendaItemDetalhadoCard
                        key={item.agendamentoId}
                        item={item}
                        onClick={(i) => setItemSelecionado(i)}
                        onEditar={handleEditarClick}
                        onCancelar={handleCancelarClick}
                      />
                    ))}
                  </AgendaSlotGroup>
                ))}
              </div>
            )}
          </>
        )}

        {/* Modal de detalhe do agendamento selecionado (RF008.1) */}
        {itemSelecionado && (
          <div className="fixed inset-0 z-50 flex items-center justify-center">
            {/* Backdrop */}
            <button
              type="button"
              className="absolute inset-0 h-full w-full bg-black/60 backdrop-blur-sm cursor-default border-none outline-none focus:outline-none"
              aria-label="Fechar detalhe"
              onClick={() => setItemSelecionado(null)}
              tabIndex={-1}
            />

            {/* Dialog Content */}
            <div
              className="relative z-10 mx-4 max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl border border-border bg-white p-6 shadow-2xl dark:border-zinc-800/60 dark:bg-zinc-900"
              role="dialog"
              aria-modal="true"
              aria-label="Detalhe do agendamento"
            >
              <button
                type="button"
                onClick={() => setItemSelecionado(null)}
                className="absolute right-4 top-4 rounded-full p-1 text-muted-foreground transition-colors hover:bg-accent hover:text-foreground dark:hover:bg-muted dark:hover:text-foreground"
                aria-label="Fechar detalhe"
              >
                <AlertCircle className="h-5 w-5 rotate-45" aria-hidden="true" />
              </button>

              <div className="mb-4 flex items-center justify-between border-b border-border pb-3 dark:border-zinc-800/40">
                <h2 className="text-lg font-bold text-foreground dark:text-zinc-50">
                  Detalhe do Agendamento
                </h2>
                <span
                  className={`rounded-full px-2.5 py-1 text-[10px] font-bold tracking-[0.12em] ${classesStatus(
                    itemSelecionado.status,
                  )}`}
                >
                  {rotuloStatus(itemSelecionado.status).toUpperCase()}
                </span>
              </div>

              {ehDetalhado(itemSelecionado) ? (
                <AgendaItemDetalhadoCard item={itemSelecionado} />
              ) : (
                <div className="space-y-3">
                  <div className="rounded-xl border border-border bg-muted p-4 dark:border-zinc-800/40 dark:bg-zinc-950/30">
                    <p className="text-sm font-semibold text-foreground dark:text-zinc-50">
                      {itemSelecionado.titulo}
                    </p>
                    <p className="mt-1 text-xs text-muted-foreground dark:text-zinc-400">
                      {itemSelecionado.servicosResumo}
                    </p>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="rounded-xl border border-border bg-muted p-3 dark:border-zinc-800/40 dark:bg-zinc-950/30">
                      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-muted-foreground">
                        Cliente
                      </p>
                      <p className="text-sm text-muted-foreground dark:text-zinc-200">
                        {itemSelecionado.clienteNome}
                      </p>
                    </div>
                    <div className="rounded-xl border border-border bg-muted p-3 dark:border-zinc-800/40 dark:bg-zinc-950/30">
                      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-muted-foreground">
                        Placa
                      </p>
                      <p className="font-mono text-sm text-muted-foreground dark:text-zinc-200">
                        {itemSelecionado.veiculoPlaca}
                      </p>
                    </div>
                  </div>
                </div>
              )}

              {/* Ações do Agendamento */}
              <div className="mt-6 flex flex-col gap-3 border-t border-border pt-4 dark:border-zinc-800/40">
                {/* Se não editável e não cancelado, exibir motivo do bloqueio */}
                {itemSelecionado.status !== 'AGENDADO' &&
                  itemSelecionado.status !== 'CANCELADO' && (
                    <div className="flex items-center gap-2 rounded-lg bg-muted dark:bg-zinc-950/30 p-3 text-xs text-muted-foreground dark:text-zinc-400 border border-border dark:border-zinc-800/40">
                      <AlertCircle className="h-4 w-4 text-muted-foreground" />
                      <span>
                        {itemSelecionado.status === 'CONCLUIDO'
                          ? 'Agendamento finalizado não pode ser editado.'
                          : 'Agendamento no status atual não permite edição.'}
                      </span>
                    </div>
                  )}

                <div className="flex flex-wrap items-center justify-between gap-3">
                  <div className="flex gap-2">
                    {/* Cancelar (elegível para AGENDADO e EM_ANDAMENTO; senão escondido/desabilitado) */}
                    {itemSelecionado.status === 'AGENDADO' ||
                    itemSelecionado.status === 'EM_ANDAMENTO' ? (
                      <Button
                        type="button"
                        variant="outline"
                        className="h-10 rounded-full border-red-200 hover:bg-red-50 text-red-600 hover:text-red-700 dark:border-red-900/30 dark:hover:bg-red-950/20"
                        onClick={() => handleCancelarClick(itemSelecionado)}
                      >
                        Cancelar agendamento
                      </Button>
                    ) : null}

                    {/* Editar (elegível apenas para AGENDADO; se concluído ou em andamento, desabilitado com motivo) */}
                    {itemSelecionado.status === 'AGENDADO' ? (
                      <Button
                        type="button"
                        variant="outline"
                        className="h-10 rounded-full"
                        onClick={() => handleEditarClick(itemSelecionado)}
                      >
                        Editar agendamento
                      </Button>
                    ) : (
                      itemSelecionado.status !== 'CANCELADO' && (
                        <span
                          title={
                            itemSelecionado.status === 'CONCLUIDO'
                              ? 'Agendamento finalizado não pode ser editado.'
                              : 'Agendamento no status atual não permite edição.'
                          }
                        >
                          <Button
                            type="button"
                            variant="outline"
                            className="h-10 rounded-full opacity-50 cursor-not-allowed"
                            disabled
                          >
                            Editar agendamento
                          </Button>
                        </span>
                      )
                    )}
                  </div>

                  <button
                    type="button"
                    onClick={() => setItemSelecionado(null)}
                    className="rounded-full bg-muted hover:bg-accent dark:bg-zinc-800 dark:hover:bg-muted px-5 py-2 text-sm font-semibold text-foreground dark:text-zinc-200 transition-colors"
                  >
                    Fechar
                  </button>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* Modal de cancelamento */}
        {cancelModalOpen && cancelItem && (
          <div className="fixed inset-0 z-50 flex items-center justify-center">
            <button
              type="button"
              className="absolute inset-0 h-full w-full bg-black/60 backdrop-blur-sm cursor-default border-none outline-none focus:outline-none"
              aria-label="Fechar modal"
              onClick={() => !cancelLoading && setCancelModalOpen(false)}
              tabIndex={-1}
            />

            <div
              className="relative z-10 mx-4 w-full max-w-md rounded-2xl border border-border bg-white p-6 shadow-2xl dark:border-zinc-800/60 dark:bg-zinc-900 animate-in zoom-in-95 duration-150"
              role="dialog"
              aria-modal="true"
              aria-labelledby="cancel-title"
            >
              <h2 id="cancel-title" className="text-lg font-bold text-foreground dark:text-zinc-50">
                Confirmar cancelamento do agendamento
              </h2>

              <div className="mt-4 space-y-4">
                <div className="flex flex-col gap-1.5">
                  <Label
                    htmlFor="cancel-motivo"
                    className="text-sm font-semibold text-muted-foreground dark:text-zinc-300"
                  >
                    Motivo do cancelamento <span className="text-red-500">*</span>
                  </Label>
                  <textarea
                    id="cancel-motivo"
                    rows={4}
                    value={motivoCancelamento}
                    autoFocus
                    onChange={(e) => {
                      setMotivoCancelamento(e.target.value);
                      if (e.target.value.trim().length >= 5) {
                        setMotivoError(null);
                      }
                    }}
                    placeholder="Descreva o motivo do cancelamento..."
                    aria-describedby="cancel-description cancel-error"
                    disabled={cancelLoading}
                    className="w-full min-h-[100px] rounded-lg border border-border bg-white px-3 py-2 text-sm text-foreground focus-visible:border-red-500 focus-visible:ring-3 focus-visible:ring-red-500/20 focus-visible:outline-none disabled:opacity-50 dark:border-zinc-700/60 dark:bg-zinc-950/40 dark:text-zinc-100 resize-none"
                  />
                  <div className="flex justify-between items-center text-xs">
                    <span id="cancel-description" className="text-muted-foreground dark:text-zinc-500">
                      Mínimo de 5 caracteres.
                    </span>
                    <span
                      className={`font-medium ${motivoCancelamento.length > 500 ? 'text-red-500' : 'text-muted-foreground dark:text-zinc-400'}`}
                    >
                      {motivoCancelamento.length}/500
                    </span>
                  </div>
                  {motivoError && (
                    <p
                      id="cancel-error"
                      role="alert"
                      className="text-xs font-medium text-red-600 dark:text-red-400"
                    >
                      {motivoError}
                    </p>
                  )}
                </div>
              </div>

              <div className="mt-6 flex justify-end gap-3">
                <Button
                  type="button"
                  variant="outline"
                  disabled={cancelLoading}
                  onClick={() => setCancelModalOpen(false)}
                  className="rounded-full h-10 px-5"
                >
                  Voltar
                </Button>
                <Button
                  type="button"
                  disabled={
                    cancelLoading ||
                    motivoCancelamento.trim().length < 5 ||
                    motivoCancelamento.length > 500
                  }
                  onClick={handleConfirmarCancelamento}
                  className="rounded-full h-10 px-5 bg-red-600 text-white hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed shadow-lg shadow-red-600/25 flex items-center gap-2"
                >
                  {cancelLoading && <Loader2 className="h-4 w-4 animate-spin" />}
                  Confirmar cancelamento
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Modal de edição simplificada */}
        {editModalOpen && editItem && (
          <div className="fixed inset-0 z-50 flex items-center justify-center">
            <button
              type="button"
              className="absolute inset-0 h-full w-full bg-black/60 backdrop-blur-sm cursor-default border-none outline-none focus:outline-none"
              aria-label="Fechar modal"
              onClick={() => !editLoading && setEditModalOpen(false)}
              tabIndex={-1}
            />

            <div
              className="relative z-10 mx-4 w-full max-w-md rounded-2xl border border-border bg-white p-6 shadow-2xl dark:border-zinc-800/60 dark:bg-zinc-900 animate-in zoom-in-95 duration-150"
              role="dialog"
              aria-modal="true"
              aria-labelledby="edit-title"
            >
              <h2 id="edit-title" className="text-lg font-bold text-foreground dark:text-zinc-50">
                Editar agendamento
              </h2>

              <div className="mt-4 space-y-4">
                <div className="flex flex-col gap-1.5">
                  <Label
                    htmlFor="edit-obs"
                    className="text-sm font-semibold text-muted-foreground dark:text-zinc-300"
                  >
                    Observações do agendamento
                  </Label>
                  <textarea
                    id="edit-obs"
                    rows={4}
                    value={observacoesEdit}
                    autoFocus
                    onChange={(e) => setObservacoesEdit(e.target.value)}
                    placeholder="Adicione observações para este agendamento..."
                    disabled={editLoading}
                    className="w-full min-h-[100px] rounded-lg border border-border bg-white px-3 py-2 text-sm text-foreground focus-visible:border-red-500 focus-visible:ring-3 focus-visible:ring-red-500/20 focus-visible:outline-none disabled:opacity-50 dark:border-zinc-700/60 dark:bg-zinc-950/40 dark:text-zinc-100 resize-none"
                  />
                  {editError && (
                    <p role="alert" className="text-xs font-medium text-red-600 dark:text-red-400">
                      {editError}
                    </p>
                  )}
                </div>
              </div>

              <div className="mt-6 flex justify-end gap-3">
                <Button
                  type="button"
                  variant="outline"
                  disabled={editLoading}
                  onClick={() => setEditModalOpen(false)}
                  className="rounded-full h-10 px-5"
                >
                  Voltar
                </Button>
                <Button
                  type="button"
                  disabled={editLoading}
                  onClick={handleConfirmarEdicao}
                  className="rounded-full h-10 px-5 bg-red-600 text-white hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed shadow-lg shadow-red-600/25 flex items-center gap-2"
                >
                  {editLoading && <Loader2 className="h-4 w-4 animate-spin" />}
                  Salvar alterações
                </Button>
              </div>
            </div>
          </div>
        )}

        {/* Toast flutuante de sucesso / erro */}
        {toast && (
          <div
            role="alert"
            aria-live="polite"
            className={`fixed bottom-6 right-6 z-50 flex items-center gap-3 rounded-2xl border px-5 py-4 shadow-xl backdrop-blur-md transition-all animate-in fade-in slide-in-from-bottom-5 duration-300 ${
              toast.tipo === 'sucesso'
                ? 'border-green-500/30 bg-green-50/90 dark:bg-green-950/90 text-green-800 dark:text-green-200'
                : 'border-red-500/30 bg-red-50/90 dark:bg-red-950/90 text-red-800 dark:text-red-200'
            }`}
          >
            <div className="flex-1 text-sm font-semibold">{toast.mensagem}</div>
            <button
              type="button"
              onClick={() => setToast(null)}
              className="rounded-full p-1 hover:bg-black/5 dark:hover:bg-white/5 text-current/60"
              aria-label="Fechar notificação"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        )}
      </section>
    </div>
  );
}

/** Estado vazio reutilizável (sem consulta ainda ou consulta sem resultados). */
function EstadoVazio({ titulo, descricao }: { titulo: string; descricao: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-border bg-white/40 px-4 py-16 text-center dark:border-zinc-800/60 dark:bg-zinc-900/20">
      <CalendarSearch className="mx-auto mb-3 h-8 w-8 text-muted-foreground" aria-hidden="true" />
      <p className="text-sm font-semibold text-muted-foreground dark:text-zinc-200">{titulo}</p>
      <p className="mt-1 text-sm text-muted-foreground dark:text-zinc-400">{descricao}</p>
    </div>
  );
}
