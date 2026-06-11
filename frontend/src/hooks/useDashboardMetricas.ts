import { keepPreviousData, useQuery } from '@tanstack/react-query';

import { dashboardService } from '@/services/dashboardService';

import type { DashboardFiltros, DashboardMetricas } from '@/types/dashboard';

/**
 * Hook de TanStack Query para carregar as métricas operacionais/financeiras do dashboard.
 *
 * <p>Integra o abort signal da query para cancelar requisições duplicadas/rápidas
 * e mantém dados anteriores visíveis durante novas requisições (`placeholderData: keepPreviousData`).</p>
 */
export function useDashboardMetricas(filtros: DashboardFiltros, enabled: boolean) {
  return useQuery<DashboardMetricas>({
    queryKey: [
      'dashboard',
      'metricas',
      filtros.inicio,
      filtros.fim,
      filtros.filialId ?? '',
      filtros.status ?? '',
    ],
    queryFn: ({ signal }) => dashboardService.obterMetricas(filtros, signal),
    placeholderData: keepPreviousData,
    enabled,
    staleTime: 5000,
  });
}
