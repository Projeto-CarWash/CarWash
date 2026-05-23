import api from './api';

import type {
  AgendaFiltros,
  AgendaItemDetalhado,
  AgendaItemSimples,
  ConsultarAgendaResponse,
} from '@/types/agenda';

/**
 * Service da visualização de agenda (RF009 / card 132). O cliente axios central
 * (services/api.ts) anexa o Bearer e cuida do refresh transparente no 401.
 *
 * <p>Consome `GET /api/v1/agenda` — contrato fechado pelo arquiteto. O backend
 * é a fonte de verdade da validação (janela de 31 dias, `inicio < fim`); este
 * service apenas serializa as datas em ISO-8601 UTC e monta os query params.</p>
 */

/**
 * Converte um valor de `<input type="datetime-local">` (hora local, sem fuso)
 * para ISO-8601 UTC com sufixo `Z`, conforme exigido pelo contrato da API.
 */
function paraIsoUtc(datetimeLocal: string): string {
  return new Date(datetimeLocal).toISOString();
}

/** Resposta com itens no formato `simples`. */
type AgendaSimplesResponse = ConsultarAgendaResponse<AgendaItemSimples>;
/** Resposta com itens no formato `detalhado`. */
type AgendaDetalhadaResponse = ConsultarAgendaResponse<AgendaItemDetalhado>;

export const agendaService = {
  /**
   * Consulta a agenda no formato `simples`.
   *
   * @param filtros filtros já validados na UI (`formato` é forçado a `simples`).
   * @returns envelope `{ message, data, traceId }` com itens resumidos.
   */
  async consultarSimples(filtros: AgendaFiltros): Promise<AgendaSimplesResponse> {
    const { data } = await api.get<AgendaSimplesResponse>('/api/v1/agenda', {
      params: montarParams({ ...filtros, formato: 'simples' }),
    });
    return data;
  },

  /**
   * Consulta a agenda no formato `detalhado`.
   *
   * @param filtros filtros já validados na UI (`formato` é forçado a `detalhado`).
   * @returns envelope `{ message, data, traceId }` com itens completos.
   */
  async consultarDetalhada(filtros: AgendaFiltros): Promise<AgendaDetalhadaResponse> {
    const { data } = await api.get<AgendaDetalhadaResponse>('/api/v1/agenda', {
      params: montarParams({ ...filtros, formato: 'detalhado' }),
    });
    return data;
  },
};

/**
 * Monta os query params da requisição, omitindo os opcionais quando vazios
 * e convertendo as datas para ISO-8601 UTC.
 */
function montarParams(filtros: AgendaFiltros): Record<string, string> {
  const params: Record<string, string> = {
    formato: filtros.formato,
    inicio: paraIsoUtc(filtros.inicio),
    fim: paraIsoUtc(filtros.fim),
    filialId: filtros.filialId,
  };
  if (filtros.clienteId) {
    params.clienteId = filtros.clienteId;
  }
  if (filtros.usuarioId) {
    params.usuarioId = filtros.usuarioId;
  }
  if (filtros.status) {
    params.status = filtros.status;
  }
  return params;
}
