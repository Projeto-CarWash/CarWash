/**
 * Tipos da feature de histórico de atendimentos por cliente (RF012).
 *
 * <p>Reutiliza os tipos de agenda (`AgendaItemDetalhado`, `AgendaStatus`) pois
 * o histórico consome o mesmo endpoint `GET /api/v1/agenda?formato=detalhado`
 * filtrado por `clienteId`. Sem criação de endpoints.</p>
 */

import type { AgendaItemDetalhado, AgendaStatus } from './agenda';

/** Filtros da tela de histórico. */
export interface HistoricoFiltros {
  /** ID do cliente selecionado — obrigatório para consulta. */
  clienteId: string;
  /** ID da filial a utilizar no filtro (obtido automaticamente). */
  filialId: string;
  /** Início do intervalo de datas (valor de `<input type="date">`). */
  dataInicio?: string;
  /** Fim do intervalo de datas (valor de `<input type="date">`). */
  dataFim?: string;
  /** Atalho de período em dias (ex.: 7, 15, 30, 60, 90). Mutuamente exclusivo com dataInicio/dataFim. */
  ultimosDias?: number;
  /** Status para filtrar (opcional). */
  status?: AgendaStatus | '';
}

/** Item da lista de histórico — mapeado a partir de `AgendaItemDetalhado`. */
export type HistoricoItem = AgendaItemDetalhado;

/**
 * Metadados de paginação. Preparado para uso futuro — o endpoint atual
 * (`GET /api/v1/agenda`) não retorna paginação. O componente só renderiza
 * paginação quando estes dados estiverem presentes na resposta.
 */
export interface HistoricoPaginacao {
  total: number;
  pagina: number;
  tamanhoPagina: number;
  totalPaginas: number;
}

/** Resposta normalizada da consulta de histórico. */
export interface HistoricoResponse {
  itens: HistoricoItem[];
  mensagem: string;
  paginacao?: HistoricoPaginacao;
}

/** Opções do filtro "últimos dias". */
export const OPCOES_ULTIMOS_DIAS = [
  { valor: 7, rotulo: 'Últimos 7 dias' },
  { valor: 15, rotulo: 'Últimos 15 dias' },
  { valor: 30, rotulo: 'Últimos 30 dias' },
  { valor: 60, rotulo: 'Últimos 60 dias' },
  { valor: 90, rotulo: 'Últimos 90 dias' },
] as const;
