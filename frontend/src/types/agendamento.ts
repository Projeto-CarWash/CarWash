export type StatusAgendamento =
  | 'agendado'
  | 'em_andamento'
  | 'concluido'
  | 'cancelado'
  | 'finalizado'
  | 'pendente';

export interface ClienteResumido {
  id: string;
  nome: string;
  cpf?: string;
  cnpj?: string;
  celular: string;
}

export interface VeiculoResumido {
  id: string;
  placa: string;
  modelo: string;
  cor: string;
  ano?: number;
}

export interface ResponsavelResumido {
  id: string;
  nome: string;
  documento?: string;
}

export interface ServicoAtivo {
  id: string;
  nome: string;
  preco: number;
  duracao: number;
  descricao?: string;
}

export interface AgendamentoWizardState {
  filialId: string;
  filialNome: string;
  cliente: ClienteResumido | null;
  veiculo: VeiculoResumido | null;
  responsavel: ResponsavelResumido | null;
  dataAgendamento: string;
  horaInicio: string;
  servicos: ServicoAtivo[];
  /** Observações logísticas opcionais (máx. 1000 caracteres). */
  observacoesLogisticas?: string;
}

export interface CriarAgendamentoPayload {
  clienteId: string;
  veiculoId: string;
  filialId: string;
  /** Obrigatório (RF024) — responsável vinculado ao cliente. */
  responsavelId: string;
  inicio: string;
  servicoIds: string[];
  observacoes?: string;
  /** Observações logísticas opcionais (máx. 1000 caracteres). */
  observacoesLogisticas?: string | null;
}

export interface CriarAgendamentoResponse {
  id: string;
}

export interface EstatisticasMes {
  mes: number;
  nome: string;
  /** Contagem por status real do contrato da agenda (AGENDADO/EM_ANDAMENTO/CONCLUIDO/CANCELADO). */
  agendado: number;
  emAndamento: number;
  concluido: number;
  cancelado: number;
  total: number;
}

export interface AgendamentoSemana {
  id: string;
  titulo: string;
  cliente: string;
  inicio: string;
  fim: string;
  status: StatusAgendamento;
}

/** Dados do `GET /api/v1/agendamentos/{id}` (RF010) usados na edição. */
export interface AgendamentoDetalhe {
  id: string;
  filialId: string;
  clienteId: string;
  veiculoId: string;
  responsavelId: string | null;
  status: string;
  inicio: string;
  fim: string;
  observacoes: string | null;
}

/**
 * Campos editáveis do `PATCH /api/v1/agendamentos/{id}` (RF010). Todos
 * opcionais — apenas os enviados são alterados. Edição só com status AGENDADO.
 */
export interface EditarAgendamentoPayload {
  inicio?: string;
  fim?: string;
  responsavelId?: string | null;
  observacoes?: string | null;
}

export interface CriarAgendamentoRequest {
  filialId: string;
  clienteId: string;
  veiculoId: string;
  responsavelId: string | null;
  inicio: string;
  servicoIds: string[];
  observacoes?: string | null;
  /** Observações logísticas opcionais (máx. 1000 caracteres). */
  observacoesLogisticas?: string | null;
}

export interface ConfirmarAgendamentoRequest extends CriarAgendamentoRequest {
  confirmar: true;
  tokenConfirmacao: string;
  idempotencyKey: string;
}

export interface AgendamentoItemResponse {
  id: string;
  servicoId: string;
  nomeServico: string;
  precoAplicado: number;
  duracaoAplicada: number;
}

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
  /** Observações logísticas — campo a ser exposto pelo backend. */
  observacoesLogisticas?: string | null;
  versao: number;
  itens: AgendamentoItemResponse[];
  criadoEm: string;
  mensagem: string;
  traceId: string;
}

export interface ResumoConfirmacao {
  filial: { id: string; nome: string };
  cliente: { id: string; nome: string; documento: string };
  veiculo: { id: string; placa: string; modelo: string; cor: string };
  servicos: { id: string; nome: string; duracaoMin: number; preco: number }[];
  inicio: string;
  fim: string;
  duracaoTotalMin: number;
  valorTotal: number;
  observacoes: string | null;
  /** Observações logísticas — campo a ser exposto pelo backend. */
  observacoesLogisticas?: string | null;
  hashResumo: string;
}

export interface PreConfirmacaoResponse {
  tokenConfirmacao: string;
  expiraEm: string;
  resumo: ResumoConfirmacao;
  traceId: string;
}

export interface CancelarAgendamentoData {
  id: string;
  status: string;
  canceladoEm: string | null;
  canceladoPor: string | null;
  motivoCancelamento: string | null;
}

export interface CancelarAgendamentoResponse {
  message: string;
  data: CancelarAgendamentoData;
  traceId: string;
}

/** Resposta das transições de status iniciar/finalizar (RF010/RF013). */
export interface TransicaoAgendamentoData {
  id: string;
  status: string;
  atualizadoEm: string;
}

export interface TransicaoAgendamentoResponse {
  message: string;
  data: TransicaoAgendamentoData;
  traceId: string;
}
