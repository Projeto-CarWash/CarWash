/**
 * Tipos de serviço (RF006) usados no formulário de agendamento.
 *
 * <p>`precoBase` e `duracaoMin` alimentam o resumo inline (totais estimados).
 * O valor definitivo é congelado pelo backend na criação do agendamento.</p>
 *
 * <p>O contrato de listagem ainda é dependência pendente do backend
 * (ver `services/servicoService.ts`).</p>
 */

export interface ServicoResumo {
  id: string;
  nome: string;
  /** Preço de catálogo em reais. */
  precoBase: number;
  /** Duração estimada em minutos. */
  duracaoMin: number;
  ativo: boolean;
}

export interface ListaServicos {
  itens: ServicoResumo[];
  total: number;
}
