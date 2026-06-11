import api from './api';

import type { DashboardFiltros, DashboardMetricas } from '@/types/dashboard';

/**
 * Envelope real retornado por `GET /api/v1/dashboard/metricas`
 * (DashboardMetricasResponse no backend). O painel consome uma forma "achatada"
 * (DashboardMetricas), então o service faz o mapeamento aqui.
 */
interface DashboardMetricasApiResponse {
  data: {
    operacional: {
      totalAtendimentos: number;
      pendentes: number;
      concluidos: number;
      cancelados: number;
      taxaConclusao: number;
      tempoMedioAtendimentoMin: number;
    };
    financeiro: {
      faturamentoTotal: number;
      ticketMedio: number;
    };
  };
}

export const dashboardService = {
  /**
   * Obtém as métricas operacionais e financeiras do painel administrativo.
   */
  async obterMetricas(filtros: DashboardFiltros, signal?: AbortSignal): Promise<DashboardMetricas> {
    // O backend espera `dataInicio`/`dataFim` (não `inicio`/`fim`) e responde num
    // envelope aninhado (data.operacional / data.financeiro). Enviar os nomes
    // errados causava 400; consumir a forma achatada direto quebrava o render.
    const params: Record<string, string | undefined> = {
      dataInicio: filtros.inicio,
      dataFim: filtros.fim,
      filialId: filtros.filialId === '' ? undefined : (filtros.filialId ?? undefined),
      status: filtros.status === '' ? undefined : (filtros.status ?? undefined),
    };

    const { data } = await api.get<DashboardMetricasApiResponse>('/api/v1/dashboard/metricas', {
      params,
      signal,
    });

    const op = data.data.operacional;
    const fin = data.data.financeiro;

    return {
      total: op.totalAtendimentos,
      pendentes: op.pendentes,
      concluidos: op.concluidos,
      cancelados: op.cancelados,
      ocupacao: op.taxaConclusao,
      tempoMedio: op.tempoMedioAtendimentoMin,
      faturamento: fin.faturamentoTotal,
      ticketMedio: fin.ticketMedio,
    };
  },
};
