export type StatusAgendamento = 'agendado' | 'cancelado' | 'finalizado' | 'pendente';

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
}

export interface CriarAgendamentoResponse {
  id: string;
}

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
  inicio: string;
  fim: string;
  status: StatusAgendamento;
}

export interface CriarAgendamentoRequest {
  filialId: string;
  clienteId: string;
  veiculoId: string;
  responsavelId: string | null;
  inicio: string;
  servicoIds: string[];
  observacoes?: string | null;
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
