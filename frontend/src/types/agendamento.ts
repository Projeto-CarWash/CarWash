/**
 * Tipos do agendamento (RF007) alinhados ao contrato fechado pelo arquiteto
 * para `POST /api/v1/agendamentos`.
 *
 * <p>O backend é a fonte de verdade das regras críticas RN004/RN006/RN010/RN011
 * (DAT §4.2 — frontend não decide regra de negócio). O servidor deriva `fim`,
 * `duracaoTotalMin` e `valorTotal`; o cliente envia apenas o necessário.</p>
 *
 * <p>O backend serializa em camelCase (JsonSerializerDefaults.Web) e datas em
 * ISO-8601 UTC com sufixo `Z`.</p>
 */

/** Estados possíveis de um agendamento — espelha `StatusAgendamento` do domínio. */
export type StatusAgendamento = 'agendado' | 'cancelado' | 'finalizado';

/**
 * Corpo do `POST /api/v1/agendamentos`.
 *
 * <p>Não enviar `fim` nem preço — o servidor deriva a partir dos serviços.
 * `servicoIds` exige ao menos 1 item e não pode conter duplicatas.</p>
 */
export interface CriarAgendamentoRequest {
  filialId: string;
  clienteId: string;
  veiculoId: string;
  responsavelId?: string | null;
  /** Início em ISO-8601 UTC com `Z` (ex.: `2026-05-22T14:00:00.000Z`). */
  inicio: string;
  servicoIds: string[];
  observacoes?: string | null;
}

/** Item de serviço aplicado ao agendamento (preço/duração congelados na criação). */
export interface AgendamentoItem {
  id: string;
  servicoId: string;
  nomeServico: string;
  precoAplicado: number;
  duracaoAplicada: number;
}

/** Resposta `201 Created` do `POST /api/v1/agendamentos`. */
export interface AgendamentoResponse {
  id: string;
  filialId: string;
  clienteId: string;
  veiculoId: string;
  responsavelId: string | null;
  status: StatusAgendamento;
  inicio: string;
  fim: string;
  duracaoTotalMin: number;
  valorTotal: number;
  observacoes: string | null;
  versao: number;
  itens: AgendamentoItem[];
  criadoEm: string;
  mensagem: string;
  traceId: string;
}
