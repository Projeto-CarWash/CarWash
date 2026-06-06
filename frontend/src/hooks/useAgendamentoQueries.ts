import { useMutation, useQuery } from '@tanstack/react-query';

import { agendamentoService } from '@/services/agendamentoService';
import { clienteService } from '@/services/clienteService';
import { filialService } from '@/services/filialService';
import { servicoService } from '@/services/servicoService';

import type {
  AgendamentoResponse,
  ConfirmarAgendamentoRequest,
  CriarAgendamentoRequest,
  PreConfirmacaoResponse,
} from '@/types/agendamento';

/**
 * Hooks de TanStack Query da feature de agendamento (RF007).
 *
 * <p>As listas de apoio (clientes, veículos, filiais, serviços) usam `useQuery`
 * com cache. A criação usa `useMutation` — sem retry, pois conflitos 409/422
 * devem ser exibidos, não mascarados por reenvio automático.</p>
 */

/** Lista de clientes para o seletor (endpoint `GET /clientes` já existe). */
export function useClientesParaAgendamento(busca: string) {
  return useQuery({
    queryKey: ['agendamento', 'clientes', busca],
    queryFn: () =>
      clienteService.listar({
        busca: busca.trim() || undefined,
        ativo: true,
        pagina: 1,
        tamanhoPagina: 50,
      }),
  });
}

/**
 * Catálogo de serviços.
 *
 * @remarks Depende de `GET /api/v1/servicos` — endpoint PENDENTE no backend
 * (ver `services/servicoService.ts`).
 */
export function useServicos() {
  return useQuery({
    queryKey: ['agendamento', 'servicos'],
    queryFn: () => servicoService.listar(),
  });
}

/**
 * Lista de filiais ativas para o seletor obrigatório do agendamento (RF019).
 *
 * @remarks `GET /api/v1/filiais?ativo=true` (endpoint oficial — ADR-0007 §4).
 */
export function useFiliais() {
  return useQuery({
    queryKey: ['agendamento', 'filiais'],
    queryFn: () => filialService.listar(),
  });
}

/**
 * Mutation de criação de agendamento em passo único — `POST /api/v1/agendamentos`.
 *
 * @deprecated Legado do card 131. O fluxo principal usa
 *   `usePreConfirmarAgendamento` + `useConfirmarAgendamento` (RF015).
 */
export function useCriarAgendamento() {
  return useMutation<AgendamentoResponse, unknown, CriarAgendamentoRequest>({
    mutationFn: (payload) => agendamentoService.criar(payload),
  });
}

/**
 * Mutation da etapa 1 do RF015 — `POST /api/v1/agendamentos/pre-confirmacao`.
 *
 * <p>Sem retry: 409 (conflito RN011 já na prévia) e 422 (recurso inativo) devem
 * ser exibidos ao usuário, não mascarados por reenvio automático.</p>
 */
export function usePreConfirmarAgendamento() {
  return useMutation<PreConfirmacaoResponse, unknown, CriarAgendamentoRequest>({
    mutationFn: (payload) => agendamentoService.preConfirmar(payload),
    retry: false,
  });
}

/**
 * Mutation da etapa 2 do RF015 — `POST /api/v1/agendamentos/confirmar`.
 *
 * <p>Sem retry: 409 (conflito/divergência) e 410 (token expirado) precisam ser
 * tratados pela UI — um reenvio cego com a mesma `idempotencyKey` mascararia o
 * conflito ou repetiria o erro.</p>
 */
export function useConfirmarAgendamento() {
  return useMutation<AgendamentoResponse, unknown, ConfirmarAgendamentoRequest>({
    mutationFn: (payload) => agendamentoService.confirmar(payload),
    retry: false,
  });
}
