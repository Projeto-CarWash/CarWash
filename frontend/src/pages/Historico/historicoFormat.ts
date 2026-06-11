/**
 * Utilitários de formatação e mensagens do histórico de atendimentos (RF012).
 *
 * <p>Re-exporta utilitários de `agendaFormat.ts` para consistência visual
 * entre agenda e histórico, e define as mensagens de erro HTTP específicas
 * da feature conforme especificação do RF.</p>
 */

export {
  classesStatus,
  formatarData,
  formatarDataHora,
  formatarFaixaHorario,
  formatarHora,
  rotuloStatus,
} from '@/pages/Agenda/agendaFormat';

/** Mensagens de erro HTTP específicas do histórico (RF012). */
export const MENSAGENS_ERRO_HISTORICO: Record<number, string> = {
  400: 'Parâmetros de consulta inválidos. Verifique os filtros e tente novamente.',
  401: 'Autenticação obrigatória para executar esta operação.',
  403: 'Você não possui permissão para consultar histórico de atendimentos.',
  404: 'Cliente não encontrado para consulta de histórico.',
  500: 'Não foi possível concluir a consulta no momento. Tente novamente.',
};

/** Mensagem de sucesso com dados. */
export const MSG_SUCESSO = 'Histórico de atendimentos consultado com sucesso.';

/** Mensagem de resultado vazio. */
export const MSG_VAZIO = 'Nenhum atendimento encontrado para este cliente.';

/** Mensagem de filtro inválido (ultimosDias + intervalo simultaneamente). */
export const MSG_FILTRO_INVALIDO =
  'Não é possível combinar "Últimos dias" com intervalo de datas. Escolha apenas um tipo de período.';

/** Mensagem de dataInicio > dataFim. */
export const MSG_DATA_INVALIDA = 'A data de início deve ser menor ou igual à data de fim.';

/**
 * Retorna a mensagem de erro HTTP apropriada para o histórico.
 * Prioriza o mapa de mensagens específicas; fallback para mensagem genérica.
 */
export function mensagemErroHistorico(status: number | null): string {
  if (status !== null && status in MENSAGENS_ERRO_HISTORICO) {
    return MENSAGENS_ERRO_HISTORICO[status]!;
  }
  return MENSAGENS_ERRO_HISTORICO[500]!;
}
