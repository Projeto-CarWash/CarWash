import api from './api';

import type { AgendamentoResponse, CriarAgendamentoRequest } from '@/types/agendamento';

/**
 * Service de agendamentos (RF007). O cliente axios central (services/api.ts)
 * anexa o Bearer e cuida do refresh transparente no 401.
 *
 * <p>O endpoint `POST /api/v1/agendamentos` é entregue no card 131 — esta
 * função está 100% funcional. O servidor deriva `fim`, `duracaoTotalMin` e
 * `valorTotal`; portanto o payload NÃO carrega esses campos.</p>
 */
export const agendamentoService = {
  /**
   * Cria um agendamento.
   *
   * @param payload corpo já normalizado (`inicio` em ISO-8601 UTC com `Z`,
   *   `servicoIds` sem duplicatas).
   * @returns o agendamento criado, incluindo totais derivados pelo servidor.
   */
  async criar(payload: CriarAgendamentoRequest): Promise<AgendamentoResponse> {
    const { data } = await api.post<AgendamentoResponse>('/api/v1/agendamentos', payload);
    return data;
  },
};
