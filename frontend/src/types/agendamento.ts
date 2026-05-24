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

/* ----------------------------------------------------------------------------
 * RF015 — fluxo de confirmação em 2 etapas (card 133).
 *
 * O frontend submete o formulário em `POST /api/v1/agendamentos/pre-confirmacao`
 * (gera resumo + token, NÃO persiste) e, após revisão explícita, confirma em
 * `POST /api/v1/agendamentos/confirmar` (persiste de fato). O backend continua
 * sendo a fonte de verdade — o `hashResumo` permite detectar divergência.
 * -------------------------------------------------------------------------- */

/** Filial no resumo de pré-confirmação. */
export interface ResumoFilial {
  id: string;
  nome: string;
}

/** Cliente no resumo de pré-confirmação. */
export interface ResumoCliente {
  id: string;
  nome: string;
  documento: string;
}

/** Veículo no resumo de pré-confirmação. */
export interface ResumoVeiculo {
  id: string;
  placa: string;
  modelo: string;
  cor: string;
}

/** Serviço no resumo de pré-confirmação (preço/duração derivados pelo servidor). */
export interface ResumoServico {
  id: string;
  nome: string;
  duracaoMin: number;
  preco: number;
}

/**
 * Resumo do agendamento devolvido pela pré-confirmação. Reflete os recursos
 * resolvidos e os totais derivados pelo backend; `hashResumo` é reenviado na
 * confirmação para detectar alteração de dados entre as etapas.
 */
export interface ResumoConfirmacao {
  filial: ResumoFilial;
  cliente: ResumoCliente;
  veiculo: ResumoVeiculo;
  servicos: ResumoServico[];
  /** Início em ISO-8601 UTC com `Z`. */
  inicio: string;
  /** Fim derivado pelo servidor, em ISO-8601 UTC com `Z`. */
  fim: string;
  duracaoTotalMin: number;
  valorTotal: number;
  observacoes: string | null;
  hashResumo: string;
}

/** Resposta `200 OK` de `POST /api/v1/agendamentos/pre-confirmacao`. */
export interface PreConfirmacaoResponse {
  /** Token opaco que autoriza a confirmação subsequente. */
  tokenConfirmacao: string;
  /** Expiração da sessão de confirmação, em ISO-8601 UTC com `Z`. */
  expiraEm: string;
  resumo: ResumoConfirmacao;
  traceId: string;
}

/**
 * Corpo do `POST /api/v1/agendamentos/confirmar`.
 *
 * <p>Carrega os mesmos campos do agendamento mais o `tokenConfirmacao` da
 * prévia, a flag `confirmar` e uma `idempotencyKey` (GUID) estável — gerada
 * uma única vez por sessão de revisão para tornar o replay seguro.</p>
 */
export interface ConfirmarAgendamentoRequest extends CriarAgendamentoRequest {
  confirmar: true;
  tokenConfirmacao: string;
  idempotencyKey: string;
}
