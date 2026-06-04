import { AlertCircle, CalendarRange, CalendarSearch, Loader2, RotateCw } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';

import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useClientesParaAgendamento, useFiliais } from '@/hooks/useAgendamentoQueries';
import { useAgenda, validarFiltrosAgenda } from '@/hooks/useAgendaQuery';
import { tratarErroApi } from '@/lib/apiError';

import { AgendaItemDetalhadoCard } from './AgendaItemDetalhadoCard';
import { AgendaItemSimplesRow } from './AgendaItemSimplesRow';
import { AgendaSlotGroup } from './AgendaSlotGroup';

import type {
  AgendaFiltros,
  AgendaFormato,
  AgendaItemDetalhado,
  AgendaItemSimples,
  AgendaStatus,
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
  'h-10 rounded-lg border border-zinc-300 bg-white px-3 text-sm text-zinc-800 focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none disabled:opacity-50 dark:border-zinc-700/60 dark:bg-zinc-950/40 dark:text-zinc-100';

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
          <h1 className="text-2xl font-bold tracking-tight text-zinc-900 dark:text-zinc-50">
            Agenda
          </h1>
          <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
            Visualize os agendamentos em formato simples ou detalhado (RF009).
          </p>
        </div>
      </div>

      {/* Barra de filtros */}
      <form
        onSubmit={handleBuscar}
        aria-label="Filtros da agenda"
        className="mb-6 rounded-2xl border border-zinc-200/70 bg-white/60 p-4 dark:border-zinc-800/60 dark:bg-zinc-900/30"
      >
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {/* Período início */}
          <div className="flex flex-col gap-1.5">
            <Label htmlFor="ag-inicio" className="text-zinc-600 dark:text-zinc-300">
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
            <Label htmlFor="ag-fim" className="text-zinc-600 dark:text-zinc-300">
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
            <Label htmlFor="ag-filial" className="text-zinc-600 dark:text-zinc-300">
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
            <Label htmlFor="ag-cliente" className="text-zinc-600 dark:text-zinc-300">
              Cliente <span className="text-zinc-400">(opcional)</span>
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
            <Label htmlFor="ag-responsavel" className="text-zinc-600 dark:text-zinc-300">
              Responsável <span className="text-zinc-400">(opcional)</span>
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
            <Label htmlFor="ag-status" className="text-zinc-600 dark:text-zinc-300">
              Status <span className="text-zinc-400">(opcional)</span>
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
            className="inline-flex rounded-full border border-zinc-300 p-0.5 dark:border-zinc-700/60"
          >
            <button
              type="button"
              aria-pressed={formato === 'simples'}
              onClick={() => setFormato('simples')}
              className={`rounded-full px-4 py-1.5 text-sm font-medium transition-colors ${
                formato === 'simples'
                  ? 'bg-red-600 text-white'
                  : 'text-zinc-500 hover:text-zinc-800 dark:hover:text-zinc-200'
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
                  : 'text-zinc-500 hover:text-zinc-800 dark:hover:text-zinc-200'
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
            className="flex items-center justify-center gap-2 rounded-2xl border border-zinc-200/70 bg-white/40 px-4 py-16 text-sm text-zinc-500 dark:border-zinc-800/60 dark:bg-zinc-900/20 dark:text-zinc-400"
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
            <p className="mb-3 text-xs text-zinc-500 dark:text-zinc-400">
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
              className="relative z-10 mx-4 max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-2xl border border-zinc-200/70 bg-white p-6 shadow-2xl dark:border-zinc-800/60 dark:bg-zinc-900"
              role="dialog"
              aria-modal="true"
              aria-label="Detalhe do agendamento"
            >
              <button
                type="button"
                onClick={() => setItemSelecionado(null)}
                className="absolute right-4 top-4 rounded-full p-1 text-zinc-400 transition-colors hover:bg-zinc-100 hover:text-zinc-700 dark:hover:bg-zinc-800 dark:hover:text-zinc-200"
                aria-label="Fechar detalhe"
              >
                <AlertCircle className="h-5 w-5 rotate-45" aria-hidden="true" />
              </button>

              <h2 className="mb-4 text-lg font-bold text-zinc-900 dark:text-zinc-50">
                Detalhe do Agendamento
              </h2>

              {ehDetalhado(itemSelecionado) ? (
                <AgendaItemDetalhadoCard item={itemSelecionado} />
              ) : (
                <div className="space-y-3">
                  <div className="rounded-xl border border-zinc-200/60 bg-zinc-50/50 p-4 dark:border-zinc-800/40 dark:bg-zinc-950/30">
                    <p className="text-sm font-semibold text-zinc-900 dark:text-zinc-50">
                      {itemSelecionado.titulo}
                    </p>
                    <p className="mt-1 text-xs text-zinc-500 dark:text-zinc-400">
                      {itemSelecionado.servicosResumo}
                    </p>
                  </div>
                  <div className="grid grid-cols-2 gap-3">
                    <div className="rounded-xl border border-zinc-200/60 bg-zinc-50/50 p-3 dark:border-zinc-800/40 dark:bg-zinc-950/30">
                      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-zinc-400">
                        Cliente
                      </p>
                      <p className="text-sm text-zinc-700 dark:text-zinc-200">
                        {itemSelecionado.clienteNome}
                      </p>
                    </div>
                    <div className="rounded-xl border border-zinc-200/60 bg-zinc-50/50 p-3 dark:border-zinc-800/40 dark:bg-zinc-950/30">
                      <p className="text-[10px] font-bold uppercase tracking-[0.12em] text-zinc-400">
                        Placa
                      </p>
                      <p className="font-mono text-sm text-zinc-700 dark:text-zinc-200">
                        {itemSelecionado.veiculoPlaca}
                      </p>
                    </div>
                  </div>
                </div>
              )}

              <div className="mt-6 flex justify-end">
                <button
                  type="button"
                  onClick={() => setItemSelecionado(null)}
                  className="rounded-full bg-red-600 px-5 py-2 text-sm font-semibold text-white shadow-lg shadow-red-600/25 transition-colors hover:bg-red-700"
                >
                  Fechar
                </button>
              </div>
            </div>
          </div>
        )}
      </section>
    </div>
  );
}

/** Estado vazio reutilizável (sem consulta ainda ou consulta sem resultados). */
function EstadoVazio({ titulo, descricao }: { titulo: string; descricao: string }) {
  return (
    <div className="rounded-2xl border border-dashed border-zinc-300 bg-white/40 px-4 py-16 text-center dark:border-zinc-800/60 dark:bg-zinc-900/20">
      <CalendarSearch className="mx-auto mb-3 h-8 w-8 text-zinc-400" aria-hidden="true" />
      <p className="text-sm font-semibold text-zinc-700 dark:text-zinc-200">{titulo}</p>
      <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">{descricao}</p>
    </div>
  );
}
