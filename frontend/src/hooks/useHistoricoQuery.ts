/**
 * Hook de TanStack Query para o histórico de atendimentos por cliente (RF012).
 *
 * <p>A query só é habilitada quando `clienteId` está presente — sem cliente
 * selecionado, nenhuma chamada é feita.</p>
 */

import { useQuery } from '@tanstack/react-query';

import { historicoService } from '@/services/historicoService';

import type { HistoricoFiltros, HistoricoResponse } from '@/types/historico';

/**
 * Consulta o histórico de atendimentos de um cliente.
 *
 * @param filtros filtros da tela — a query só executa se `clienteId` existir.
 * @returns resultado da consulta com itens ordenados do mais recente ao mais antigo.
 */
export function useHistoricoCliente(filtros: HistoricoFiltros | null) {
  return useQuery<HistoricoResponse>({
    queryKey: [
      'historico',
      filtros?.clienteId,
      filtros?.filialId,
      filtros?.dataInicio,
      filtros?.dataFim,
      filtros?.ultimosDias,
      filtros?.status,
    ],
    queryFn: () => historicoService.consultarHistorico(filtros!),
    enabled: !!filtros?.clienteId,
    staleTime: 60_000,
  });
}
