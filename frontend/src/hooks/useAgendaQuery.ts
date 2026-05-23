import { useQuery } from '@tanstack/react-query';

import { agendaService } from '@/services/agendaService';

import type {
  AgendaFiltros,
  AgendaItemDetalhado,
  AgendaItemSimples,
  ConsultarAgendaResponse,
} from '@/types/agenda';

/**
 * Hook de TanStack Query da visualização de agenda (RF009 / card 132).
 *
 * <p>A consulta só dispara quando os filtros obrigatórios estão preenchidos e
 * a janela é válida (`inicio < fim`, no máximo 31 dias) — defesa de UX; o
 * backend valida novamente. `staleTime: 0` garante revalidação a cada mudança
 * de filtro/formato, sem dados desatualizados (requisito do card).</p>
 */

/** Janela máxima permitida entre `inicio` e `fim`, em milissegundos (31 dias). */
const JANELA_MAXIMA_MS = 31 * 24 * 60 * 60 * 1000;

/** Resultado da validação dos filtros antes de habilitar a query. */
export interface ValidacaoFiltros {
  /** `true` quando todos os obrigatórios estão preenchidos e a janela é válida. */
  valido: boolean;
  /** Motivo do bloqueio (apresentável ao usuário), ou `null` quando válido. */
  motivo: string | null;
}

/**
 * Valida os filtros obrigatórios e a janela de datas no cliente.
 *
 * <p>Distingue "incompleto" (sem obrigatório preenchido — não é erro, só não
 * dispara) de "inválido" (janela errada — exibe motivo ao usuário).</p>
 */
export function validarFiltrosAgenda(filtros: AgendaFiltros): ValidacaoFiltros {
  const { inicio, fim, filialId } = filtros;

  if (!inicio || !fim || !filialId) {
    return { valido: false, motivo: null };
  }

  const inicioMs = new Date(inicio).getTime();
  const fimMs = new Date(fim).getTime();

  if (Number.isNaN(inicioMs) || Number.isNaN(fimMs)) {
    return { valido: false, motivo: 'Informe datas de início e fim válidas.' };
  }
  if (inicioMs >= fimMs) {
    return { valido: false, motivo: 'A data de início deve ser anterior à data de fim.' };
  }
  if (fimMs - inicioMs > JANELA_MAXIMA_MS) {
    return { valido: false, motivo: 'O período não pode ultrapassar 31 dias.' };
  }

  return { valido: true, motivo: null };
}

/** Chave de cache estável a partir dos filtros aplicados. */
function chaveAgenda(filtros: AgendaFiltros): unknown[] {
  return [
    'agenda',
    filtros.formato,
    filtros.inicio,
    filtros.fim,
    filtros.filialId,
    filtros.clienteId ?? '',
    filtros.usuarioId ?? '',
    filtros.status ?? '',
  ];
}

/**
 * Consulta a agenda conforme o `formato` dos filtros.
 *
 * @param filtros filtros aplicados (já memorizados pela página).
 * @returns query com `data` no formato correspondente. A query fica `enabled`
 *   apenas quando {@link validarFiltrosAgenda} retorna válido.
 */
export function useAgenda(filtros: AgendaFiltros) {
  const { valido } = validarFiltrosAgenda(filtros);

  return useQuery<ConsultarAgendaResponse<AgendaItemSimples | AgendaItemDetalhado>>({
    queryKey: chaveAgenda(filtros),
    queryFn: () =>
      filtros.formato === 'simples'
        ? agendaService.consultarSimples(filtros)
        : agendaService.consultarDetalhada(filtros),
    enabled: valido,
    staleTime: 0,
    refetchOnMount: 'always',
  });
}
