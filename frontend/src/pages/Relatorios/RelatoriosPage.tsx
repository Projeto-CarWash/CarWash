import {
  AlertCircle,
  Building2,
  CalendarDays,
  CheckCircle2,
  Clock,
  DollarSign,
  LayoutDashboard,
  Loader2,
  Percent,
  RefreshCw,
  Timer,
  TrendingUp,
  XCircle,
} from 'lucide-react';
import { useCallback, useState } from 'react';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
import { useDashboardMetricas } from '@/hooks/useDashboardMetricas';
import { useFiliaisLista } from '@/hooks/useFilialQueries';
import { formatarDuracao, formatarReais } from '@/lib/format';

import type { DashboardFiltros } from '@/types/dashboard';

// Helper to get formatted date string for today and 30 days ago
function obterDataOffset(offsetDias: number): string {
  const d = new Date();
  d.setDate(d.getDate() + offsetDias);
  return d.toISOString().split('T')[0] ?? '';
}

export function RelatoriosPage() {
  // Filters State
  const [inicio, setInicio] = useState(() => obterDataOffset(-30));
  const [fim, setFim] = useState(() => obterDataOffset(0));
  const [filialId, setFilialId] = useState('');
  const [status, setStatus] = useState('');

  // Load filiais list for the dropdown filter
  const {
    data: filiaisData,
    isLoading: carregandoFiliais,
    isError: erroFiliais,
    refetch: recarregarFiliais,
  } = useFiliaisLista();

  // Validate filters
  const isInvalidPeriod = inicio && fim && new Date(inicio) > new Date(fim);
  const dataValida = !isInvalidPeriod && !!inicio && !!fim;

  // Memoize filters payload
  const filtros: DashboardFiltros = {
    inicio,
    fim,
    filialId: filialId || undefined,
    status: status || undefined,
  };

  // Load Dashboard Metrics
  const {
    data: metricas,
    isLoading: carregandoMetricas,
    isError: erroMetricas,
    refetch: recarregarMetricas,
    isFetching: recarregandoMetricas,
  } = useDashboardMetricas(filtros, dataValida);

  const isLoading = carregandoMetricas || carregandoFiliais;

  const handleRetry = useCallback(() => {
    if (erroFiliais) {
      void recarregarFiliais();
    }
    if (erroMetricas) {
      void recarregarMetricas();
    }
  }, [erroFiliais, erroMetricas, recarregarFiliais, recarregarMetricas]);

  return (
    <div className="px-6 py-6 space-y-6 md:px-8 md:py-8">
      {/* Header Section */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-foreground dark:text-zinc-50 flex items-center gap-2">
            <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-red-600/10 text-red-500">
              <LayoutDashboard className="h-5 w-5" />
            </span>
            Painel de Métricas
          </h1>
          <p className="mt-1 text-sm text-muted-foreground dark:text-zinc-400">
            Monitore o desempenho operacional e financeiro da rede CarWash.
          </p>
        </div>
      </div>

      {/* Filter Bar */}
      <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
        <CardContent className="p-4 grid grid-cols-1 gap-4 sm:grid-cols-2 md:grid-cols-4 items-end">
          {/* Start Date */}
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="filtro-inicio"
              className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground dark:text-zinc-400 uppercase"
            >
              Data Inicial
            </label>
            <input
              id="filtro-inicio"
              type="date"
              value={inicio}
              onChange={(e) => setInicio(e.target.value)}
              className="h-10 rounded-xl border border-border dark:border-zinc-700/60 bg-muted dark:bg-zinc-950/40 px-3 text-sm text-foreground dark:text-zinc-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50 dark:[color-scheme:dark]"
            />
          </div>

          {/* End Date */}
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="filtro-fim"
              className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground dark:text-zinc-400 uppercase"
            >
              Data Final
            </label>
            <input
              id="filtro-fim"
              type="date"
              value={fim}
              onChange={(e) => setFim(e.target.value)}
              className="h-10 rounded-xl border border-border dark:border-zinc-700/60 bg-muted dark:bg-zinc-950/40 px-3 text-sm text-foreground dark:text-zinc-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50 dark:[color-scheme:dark]"
            />
          </div>

          {/* Filial */}
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="filtro-filial"
              className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground dark:text-zinc-400 uppercase"
            >
              Filial
            </label>
            <div className="relative">
              <Building2 className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
              <select
                id="filtro-filial"
                value={filialId}
                onChange={(e) => setFilialId(e.target.value)}
                disabled={carregandoFiliais}
                className="h-10 w-full cursor-pointer appearance-none rounded-xl border border-border dark:border-zinc-700/60 bg-muted dark:bg-zinc-950/40 pl-9 pr-4 text-sm text-foreground dark:text-zinc-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50 dark:[color-scheme:dark] disabled:opacity-50"
              >
                <option value="">Todas as Filiais</option>
                {filiaisData?.itens.map((f) => (
                  <option key={f.id} value={f.id}>
                    {f.nome} {f.codigo ? `(${f.codigo})` : ''}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Status */}
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="filtro-status"
              className="text-[10px] font-bold tracking-[0.2em] text-muted-foreground dark:text-zinc-400 uppercase"
            >
              Status
            </label>
            <select
              id="filtro-status"
              value={status}
              onChange={(e) => setStatus(e.target.value)}
              className="h-10 w-full cursor-pointer appearance-none rounded-xl border border-border dark:border-zinc-700/60 bg-muted dark:bg-zinc-950/40 px-3 text-sm text-foreground dark:text-zinc-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50 dark:[color-scheme:dark]"
            >
              <option value="">Todos os Status</option>
              <option value="PENDENTE">Pendente</option>
              <option value="CONCLUIDO">Concluído</option>
              <option value="CANCELADO">Cancelado</option>
            </select>
          </div>
        </CardContent>
      </Card>

      {/* Validation Warning */}
      {isInvalidPeriod && (
        <div
          role="alert"
          className="flex items-center gap-3 rounded-xl border border-red-200 dark:border-red-500/30 bg-red-50 dark:bg-red-950/20 px-4 py-3 text-sm text-red-600 dark:text-red-400"
        >
          <AlertCircle className="h-4 w-4 shrink-0" />
          <span>A data inicial não pode ser maior que a data final.</span>
        </div>
      )}

      {/* Loading / Fetching State Indicator */}
      {recarregandoMetricas && !isLoading && (
        <div className="flex items-center gap-2 text-xs text-muted-foreground animate-pulse">
          <Loader2 className="h-3 w-3 animate-spin" />
          Atualizando métricas em segundo plano...
        </div>
      )}

      {/* Metrics Section */}
      {isLoading ? (
        /* Skeletons Layout */
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Card
              key={i}
              className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30 animate-pulse"
            >
              <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
                <div className="h-3 w-24 bg-zinc-200 dark:bg-zinc-800 rounded" />
                <div className="h-4 w-4 rounded bg-zinc-200 dark:bg-zinc-800" />
              </CardHeader>
              <CardContent>
                <div className="h-7 w-20 bg-zinc-200 dark:bg-zinc-800 rounded mt-1" />
              </CardContent>
            </Card>
          ))}
        </div>
      ) : erroMetricas || erroFiliais ? (
        /* Error Panel */
        <Card className="border-red-200 dark:border-red-900/50 bg-red-50/50 dark:bg-red-950/10">
          <CardContent className="flex flex-col items-center justify-center py-10 text-center gap-4">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-red-100 dark:bg-red-950 text-red-600 dark:text-red-400">
              <AlertCircle className="h-6 w-6" />
            </div>
            <div>
              <h3 className="font-semibold text-foreground dark:text-zinc-200">
                Erro ao carregar dados do painel
              </h3>
              <p className="text-sm text-muted-foreground dark:text-zinc-400 mt-1 max-w-md">
                Ocorreu um problema de comunicação com o servidor. Verifique sua conexão e tente
                novamente.
              </p>
            </div>
            <Button
              type="button"
              onClick={handleRetry}
              className="rounded-full bg-red-600 hover:bg-red-700 text-white font-semibold text-sm"
            >
              <RefreshCw className="mr-1.5 h-4 w-4" />
              Tentar novamente
            </Button>
          </CardContent>
        </Card>
      ) : metricas && metricas.total === 0 ? (
        /* Empty State */
        <Card className="border-border dark:border-zinc-800 bg-white dark:bg-zinc-900/30">
          <CardContent className="flex flex-col items-center justify-center py-12 text-center gap-4">
            <div className="flex h-12 w-12 items-center justify-center rounded-full bg-muted dark:bg-zinc-800 text-muted-foreground">
              <CalendarDays className="h-6 w-6" />
            </div>
            <div>
              <h3 className="font-semibold text-foreground dark:text-zinc-200">
                Nenhum dado encontrado
              </h3>
              <p className="text-sm text-muted-foreground dark:text-zinc-400 mt-1 max-w-sm">
                Nenhum dado encontrado para o período selecionado. Tente alterar os filtros de data
                ou filial.
              </p>
            </div>
          </CardContent>
        </Card>
      ) : metricas ? (
        /* Operational & Financial Grid */
        <div className="space-y-8">
          {/* Operational Metrics Block */}
          <div className="space-y-4">
            <h2 className="text-xs font-bold tracking-[0.2em] text-muted-foreground dark:text-zinc-500 uppercase">
              Métricas Operacionais
            </h2>
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
              {/* Total Bookings */}
              <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
                <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
                  <span className="text-[10px] font-bold tracking-wider text-muted-foreground dark:text-zinc-400 uppercase">
                    Total Atendimentos
                  </span>
                  <CalendarDays className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-black text-foreground dark:text-zinc-100">
                    {metricas.total.toLocaleString('pt-BR')}
                  </div>
                </CardContent>
              </Card>

              {/* Pending Bookings */}
              <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
                <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
                  <span className="text-[10px] font-bold tracking-wider text-muted-foreground dark:text-zinc-400 uppercase">
                    Pendentes
                  </span>
                  <Clock className="h-4 w-4 text-amber-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-black text-amber-500">
                    {metricas.pendentes.toLocaleString('pt-BR')}
                  </div>
                </CardContent>
              </Card>

              {/* Completed Bookings */}
              <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
                <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
                  <span className="text-[10px] font-bold tracking-wider text-muted-foreground dark:text-zinc-400 uppercase">
                    Concluídos
                  </span>
                  <CheckCircle2 className="h-4 w-4 text-emerald-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-black text-emerald-500">
                    {metricas.concluidos.toLocaleString('pt-BR')}
                  </div>
                </CardContent>
              </Card>

              {/* Canceled Bookings */}
              <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
                <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
                  <span className="text-[10px] font-bold tracking-wider text-muted-foreground dark:text-zinc-400 uppercase">
                    Cancelados
                  </span>
                  <XCircle className="h-4 w-4 text-red-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-black text-red-500">
                    {metricas.cancelados.toLocaleString('pt-BR')}
                  </div>
                </CardContent>
              </Card>

              {/* Occupancy Rate */}
              <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
                <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
                  <span className="text-[10px] font-bold tracking-wider text-muted-foreground dark:text-zinc-400 uppercase">
                    Taxa de Ocupação
                  </span>
                  <Percent className="h-4 w-4 text-sky-500" />
                </CardHeader>
                <CardContent className="space-y-2">
                  <div className="text-2xl font-black text-foreground dark:text-zinc-100">
                    {metricas.ocupacao.toLocaleString('pt-BR', { maximumFractionDigits: 1 })}%
                  </div>
                  <div className="w-full bg-muted dark:bg-zinc-800 h-1.5 rounded-full overflow-hidden">
                    <div
                      className="bg-sky-500 h-full rounded-full transition-all duration-500"
                      style={{ width: `${Math.min(100, metricas.ocupacao)}%` }}
                    />
                  </div>
                </CardContent>
              </Card>

              {/* Average Service Duration */}
              <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
                <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
                  <span className="text-[10px] font-bold tracking-wider text-muted-foreground dark:text-zinc-400 uppercase">
                    Tempo Médio
                  </span>
                  <Timer className="h-4 w-4 text-violet-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-black text-foreground dark:text-zinc-100">
                    {formatarDuracao(metricas.tempoMedio)}
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>

          {/* Financial Metrics Block */}
          <div className="space-y-4">
            <h2 className="text-xs font-bold tracking-[0.2em] text-muted-foreground dark:text-zinc-500 uppercase">
              Métricas Financeiras
            </h2>
            <div className="grid grid-cols-1 gap-6 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4">
              {/* Total Revenue */}
              <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
                <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
                  <span className="text-[10px] font-bold tracking-wider text-muted-foreground dark:text-zinc-400 uppercase">
                    Faturamento Total
                  </span>
                  <TrendingUp className="h-4 w-4 text-emerald-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-black text-emerald-600 dark:text-emerald-400">
                    {formatarReais(metricas.faturamento)}
                  </div>
                </CardContent>
              </Card>

              {/* Average Ticket */}
              <Card className="border border-border dark:border-zinc-800/60 bg-white dark:bg-zinc-900/30">
                <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
                  <span className="text-[10px] font-bold tracking-wider text-muted-foreground dark:text-zinc-400 uppercase">
                    Ticket Médio
                  </span>
                  <DollarSign className="h-4 w-4 text-emerald-500" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-black text-foreground dark:text-zinc-100">
                    {formatarReais(metricas.ticketMedio)}
                  </div>
                </CardContent>
              </Card>
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
