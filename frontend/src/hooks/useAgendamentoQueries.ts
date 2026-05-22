import { useMutation, useQuery } from '@tanstack/react-query';

import { agendamentoService } from '@/services/agendamentoService';
import { clienteService } from '@/services/clienteService';
import { filialService } from '@/services/filialService';
import { servicoService } from '@/services/servicoService';
import { veiculoService } from '@/services/veiculoService';

import type { AgendamentoResponse, CriarAgendamentoRequest } from '@/types/agendamento';

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
 * Lista de veículos do cliente selecionado.
 *
 * @remarks Depende de `GET /api/v1/veiculos` — endpoint PENDENTE no backend
 * (ver `services/veiculoService.ts`). A query só dispara quando há cliente.
 */
export function useVeiculosDoCliente(clienteId: string | undefined) {
  return useQuery({
    queryKey: ['agendamento', 'veiculos', clienteId],
    queryFn: () => veiculoService.listarPorCliente(clienteId!),
    enabled: Boolean(clienteId),
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
 * Lista de filiais.
 *
 * @remarks Depende de `GET /api/v1/filiais` — endpoint PENDENTE no backend
 * (ver `services/filialService.ts`).
 */
export function useFiliais() {
  return useQuery({
    queryKey: ['agendamento', 'filiais'],
    queryFn: () => filialService.listar(),
  });
}

/**
 * Mutation de criação de agendamento — `POST /api/v1/agendamentos`.
 *
 * <p>Endpoint entregue no card 131; totalmente funcional.</p>
 */
export function useCriarAgendamento() {
  return useMutation<AgendamentoResponse, unknown, CriarAgendamentoRequest>({
    mutationFn: (payload) => agendamentoService.criar(payload),
  });
}
