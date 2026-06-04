/**
 * Tipos de filial (RF017/RF018/RF019).
 *
 * <p>Cobrem dois usos: a listagem/gerência de filiais (cadastro e consulta —
 * `GET/POST /api/v1/filiais`) e o resumo consumido pelo seletor obrigatório do
 * agendamento (RF019/RN010, `GET /api/v1/filiais?ativo=true`).</p>
 *
 * <p>O contrato segue o backend (ADR-0007 §4): o resumo da listagem expõe
 * apenas `id, nome, codigo, cidade, uf, ativo`; o endereço no cadastro é um
 * objeto estruturado (`EnderecoFilialRequest`).</p>
 */

export interface FilialResumo {
  id: string;
  nome: string;
  /** Código/identificador operacional da filial (exibido junto ao nome — RF019). */
  codigo?: string;
  cidade?: string;
  uf?: string;
  ativo: boolean;
}

export interface ListaFiliais {
  itens: FilialResumo[];
  total: number;
}

/**
 * Endereço estruturado da filial — espelha `EnderecoFilialRequest` no backend.
 *
 * <p>Quando enviado, todos os campos (exceto `complemento`) são obrigatórios.
 * `cep` vai apenas com dígitos.</p>
 */
export interface EnderecoFilialRequest {
  cep: string;
  logradouro: string;
  numero: string;
  complemento?: string | null;
  bairro: string;
  cidade: string;
  uf: string;
}

/**
 * Payload de cadastro de filial — `POST /api/v1/filiais`.
 *
 * <p>`cnpj` é enviado apenas com dígitos (sem máscara) e omitido (`null`)
 * quando não informado. `endereco` é um objeto estruturado.</p>
 */
export interface CriarFilialRequest {
  nome: string;
  codigo: string;
  cnpj?: string | null;
  celulasAtivas: number;
  timezone?: string | null;
  endereco: EnderecoFilialRequest;
}

/** Envelope canônico do `POST /api/v1/filiais` (ADR-0007 §4.1). */
export interface CriarFilialResponse {
  id: string;
  mensagem: string;
  traceId: string;
}

/**
 * Detalhe da filial — `GET /api/v1/filiais/{id}` e resposta do
 * `PATCH /api/v1/filiais/{id}/celulas-ativas` (`FilialResponse` no backend).
 *
 * <p>Atenção ao contrato: este detalhe expõe apenas nome, capacidade, timezone,
 * status e auditoria — não inclui código, CNPJ nem endereço (RF018).</p>
 */
export interface FilialDetalhe {
  id: string;
  nome: string;
  celulasAtivas: number;
  timezone: string;
  ativa: boolean;
  criadoEm: string;
  atualizadoEm: string;
}
