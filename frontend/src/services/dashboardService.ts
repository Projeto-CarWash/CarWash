import api from './api';

import type { DashboardFiltros, DashboardMetricas } from '@/types/dashboard';

export const dashboardService = {
  /**
   * Obtém as métricas operacionais e financeiras do painel administrativo.
   */
  async obterMetricas(filtros: DashboardFiltros, signal?: AbortSignal): Promise<DashboardMetricas> {
    // O backend (DashboardMetricasEndpoints) espera `dataInicio`/`dataFim`.
    // Enviar `inicio`/`fim` resultava em 400 ("dataInicio é obrigatório") e o
    // painel exibia "Erro ao carregar dados do painel". (Bug encontrado no QA de UI.)
    const params: Record<string, string | undefined> = {
      dataInicio: filtros.inicio,
      dataFim: filtros.fim,
      filialId: filtros.filialId === '' ? undefined : (filtros.filialId ?? undefined),
      status: filtros.status === '' ? undefined : (filtros.status ?? undefined),
    };

    const { data } = await api.get<DashboardMetricas>('/api/v1/dashboard/metricas', {
      params,
      signal,
    });
    return data;
  },
};
