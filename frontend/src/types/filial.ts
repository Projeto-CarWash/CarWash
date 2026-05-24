/**
 * Tipos de filial (RF017/RF018) usados no formulário de agendamento.
 *
 * <p>O agendamento exige uma filial (RF019/RN010). O contrato de listagem
 * ainda é dependência pendente do backend (ver `services/filialService.ts`).</p>
 */

export interface FilialResumo {
  id: string;
  nome: string;
  cidade?: string;
  uf?: string;
  ativo: boolean;
}

export interface ListaFiliais {
  itens: FilialResumo[];
  total: number;
}
