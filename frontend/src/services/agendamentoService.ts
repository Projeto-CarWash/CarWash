import api from './api';

import type {
  AgendamentoResponse,
  ConfirmarAgendamentoRequest,
  CriarAgendamentoRequest,
  PreConfirmacaoResponse,
} from '@/types/agendamento';

/**
 * Service de agendamentos. O cliente axios central (services/api.ts) anexa o
 * Bearer e cuida do refresh transparente no 401.
 *
 * <p>O fluxo vigente é o de confirmação em 2 etapas (RF015, card 133):
 * `preConfirmar` gera resumo + token sem persistir e `confirmar` efetiva o
 * agendamento. O servidor deriva `fim`, `duracaoTotalMin` e `valorTotal`;
 * portanto o payload NÃO carrega esses campos.</p>
 */
export const agendamentoService = {
  /**
   * Cria um agendamento em um único passo.
   *
   * @deprecated Legado do RF007 (card 131). O fluxo principal passou a usar
   *   `preConfirmar` + `confirmar` (RF015). Mantido apenas por compatibilidade.
   */
  async criar(payload: CriarAgendamentoRequest): Promise<AgendamentoResponse> {
    const { data } = await api.post<AgendamentoResponse>('/api/v1/agendamentos', payload);
    return data;
  },

  /**
   * Etapa 1 do RF015 — solicita a pré-confirmação. NÃO persiste o agendamento:
   * o backend valida os dados, deriva os totais e devolve um resumo com token.
   *
   * @param payload corpo já normalizado (`inicio` em ISO-8601 UTC com `Z`).
   * @returns resumo + `tokenConfirmacao` para a etapa de revisão.
   */
  async preConfirmar(payload: CriarAgendamentoRequest): Promise<PreConfirmacaoResponse> {
    const { data } = await api.post<PreConfirmacaoResponse>(
      '/api/v1/agendamentos/pre-confirmacao',
      payload,
    );
    return data;
  },

  /**
   * Etapa 2 do RF015 — confirma e persiste o agendamento.
   *
   * @param payload campos do agendamento + `tokenConfirmacao`, `confirmar` e
   *   uma `idempotencyKey` estável (replay devolve o mesmo `201`).
   * @returns o agendamento criado, incluindo totais derivados pelo servidor.
   */
  async confirmar(payload: ConfirmarAgendamentoRequest): Promise<AgendamentoResponse> {
    const { data } = await api.post<AgendamentoResponse>('/api/v1/agendamentos/confirmar', payload);
    return data;
  },
};
