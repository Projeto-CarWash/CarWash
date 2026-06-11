/**
 * Tipos da visualização de agenda (RF009 / card 132) alinhados ao contrato
 * fechado pelo arquiteto para `GET /api/v1/agenda`.
 *
 * <p>O backend é a fonte de verdade das regras de consulta (janela de 31 dias,
 * `inicio < fim`). O frontend valida os mesmos limites apenas como defesa de
 * UX — DAT §4.2.</p>
 *
 * <p>O backend serializa em camelCase, datas em ISO-8601 UTC com sufixo `Z` e
 * `status` sempre em UPPERCASE.</p>
 */

/** Formato de exibição da agenda. */
export type AgendaFormato = 'simples' | 'detalhado';

/** Estados possíveis de um agendamento na resposta da agenda (UPPERCASE). */
export type AgendaStatus = 'AGENDADO' | 'EM_ANDAMENTO' | 'CONCLUIDO' | 'CANCELADO';

/** Item da agenda no formato `simples` — dados resumidos para lista/grade. */
export interface AgendaItemSimples {
  agendamentoId: string;
  /** Início em ISO-8601 UTC com `Z`. */
  inicio: string;
  /** Fim em ISO-8601 UTC com `Z`. */
  fim: string;
  titulo: string;
  status: AgendaStatus;
  clienteNome: string;
  veiculoPlaca: string;
  /** Resumo textual dos serviços (ex.: `Lavagem Completa + 1`). */
  servicosResumo: string;
}

/** Dados do cliente no item detalhado. */
export interface AgendaCliente {
  id: string;
  nome: string;
  cpfCnpj: string;
  /** Telefone fixo — pode vir `null`. */
  telefone: string | null;
  celular: string;
}

/** Dados do veículo no item detalhado. */
export interface AgendaVeiculo {
  id: string;
  placa: string;
  modelo: string;
  fabricante: string;
  cor: string;
}

/** Serviço aplicado ao agendamento no item detalhado. */
export interface AgendaServico {
  id: string;
  nome: string;
  duracaoMin: number;
  preco: number;
}

/** Item da agenda no formato `detalhado` — dados completos para cartões. */
export interface AgendaItemDetalhado {
  agendamentoId: string;
  status: AgendaStatus;
  filialId: string;
  /** Início em ISO-8601 UTC com `Z`. */
  inicio: string;
  /** Fim em ISO-8601 UTC com `Z`. */
  fim: string;
  duracaoTotalMin: number;
  valorTotal: number;
  cliente: AgendaCliente;
  veiculo: AgendaVeiculo;
  servicos: AgendaServico[];
  observacoes: string | null;
  /** Observações logísticas — campo a ser exposto pelo backend. */
  observacoesLogisticas?: string | null;
  criadoEm: string;
  atualizadoEm: string;
}

/** Envelope padrão da resposta `200` de `GET /api/v1/agenda`. */
export interface ConsultarAgendaResponse<TItem> {
  message: string;
  data: TItem[];
  traceId: string;
}

/**
 * Filtros da consulta de agenda. `formato`, `inicio`, `fim` e `filialId` são
 * obrigatórios; os demais são opcionais. `inicio`/`fim` são valores de
 * `<input type="datetime-local">` (hora local, sem fuso) e são convertidos
 * para ISO-8601 UTC pelo service.
 */
export interface AgendaFiltros {
  formato: AgendaFormato;
  /** Valor de `datetime-local` (`YYYY-MM-DDTHH:mm`). */
  inicio: string;
  /** Valor de `datetime-local` (`YYYY-MM-DDTHH:mm`). */
  fim: string;
  filialId: string;
  clienteId?: string;
  usuarioId?: string;
  status?: AgendaStatus | '';
}
