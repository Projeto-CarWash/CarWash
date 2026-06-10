import api from './api';

import type { DashboardFiltros, DashboardMetricas } from '@/types/dashboard';

export const dashboardService = {
  /**
   * Obtém as métricas operacionais e financeiras do painel administrativo.
   */
  async obterMetricas(filtros: DashboardFiltros, signal?: AbortSignal): Promise<DashboardMetricas> {
    const params: Record<string, string | undefined> = {
      inicio: filtros.inicio,
      fim: filtros.fim,
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
