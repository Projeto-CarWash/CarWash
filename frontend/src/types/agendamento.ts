/**
 * Tipos do módulo de agendamento (RF007).
 * Alinhados com o domínio backend (CarWash.Domain.Entities.Agendamento)
 * e preparados para os endpoints futuros.
 */

// ---------------------------------------------------------------------------
// Enums / Literal unions
// ---------------------------------------------------------------------------

export type StatusAgendamento = 'agendado' | 'cancelado' | 'finalizado';

// ---------------------------------------------------------------------------
// Resumos para seletores do wizard
// ---------------------------------------------------------------------------

/** Cliente resumido exibido no seletor de busca (Etapa 1). */
export interface ClienteResumido {
  id: string;
  nome: string;
  cpf?: string;
  cnpj?: string;
  celular: string;
}

/** Veículo vinculado a um cliente (Etapa 1). */
export interface VeiculoResumido {
  id: string;
  placa: string;
  modelo: string;
  cor: string;
  ano?: number;
}

/** Serviço ativo do catálogo (Etapa 2). */
export interface ServicoAtivo {
  id: string;
  nome: string;
  /** Preço em reais (ex: 89.90). */
  preco: number;
  /** Duração estimada em minutos. */
  duracao: number;
  descricao?: string;
}

// ---------------------------------------------------------------------------
// Estado do wizard
// ---------------------------------------------------------------------------

/** Estado global persistido entre etapas do wizard. */
export interface AgendamentoWizardState {
  // Etapa 1
  cliente: ClienteResumido | null;
  veiculo: VeiculoResumido | null;
  dataAgendamento: string; // formato YYYY-MM-DD
  horaInicio: string; // formato HH:mm

  // Etapa 2
  servicos: ServicoAtivo[];
}

// ---------------------------------------------------------------------------
// Payload para API
// ---------------------------------------------------------------------------

/** Payload enviado no POST /api/v1/agendamentos. */
export interface CriarAgendamentoPayload {
  clienteId: string;
  veiculoId: string;
  inicio: string; // ISO 8601
  fim: string; // ISO 8601 (calculado: inicio + duração total)
  servicoIds: string[];
  observacoes?: string;
}

/** Resposta de sucesso do POST /api/v1/agendamentos. */
export interface CriarAgendamentoResponse {
  id: string;
}

// ---------------------------------------------------------------------------
// Dashboard e Calendário
// ---------------------------------------------------------------------------

export interface EstatisticasMes {
  mes: number;
  nome: string;
  confirmados: number;
  pendentes: number;
  cancelados: number;
  total: number;
}

export interface AgendamentoSemana {
  id: string;
  titulo: string;
  cliente: string;
  inicio: string; // ISO 8601
  fim: string; // ISO 8601
  status: StatusAgendamento;
}
