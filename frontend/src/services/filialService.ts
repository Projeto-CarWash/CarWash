import api from './api';

import type {
  CriarFilialRequest,
  CriarFilialResponse,
  FilialDetalhe,
  ListaFiliais,
} from '@/types/filial';

/**
 * Service de filiais (RF017/RF018/RF019).
 *
 * <p>Consome o endpoint oficial <code>/api/v1/filiais</code>
 * (ADR-0007 §4 — <code>FiliaisEndpoints.MapFiliais</code>), que devolve o
 * envelope <code>{ itens, total }</code> compatível com
 * <code>types/filial.ts</code>. O agendamento exige uma filial ativa
 * (RF019/RN010).</p>
 */
export const filialService = {
  /**
   * Lista apenas as filiais ativas, para o seletor obrigatório do agendamento.
   *
   * @remarks `GET /api/v1/filiais?ativo=true`.
   */
  async listar(): Promise<ListaFiliais> {
    const { data } = await api.get<ListaFiliais>('/api/v1/filiais', {
      params: { ativo: true },
    });
    return data;
  },

  /**
   * Lista todas as filiais (ativas e inativas), para a tela de gerência.
   *
   * @remarks `GET /api/v1/filiais`.
   */
  async listarTodas(): Promise<ListaFiliais> {
    const { data } = await api.get<ListaFiliais>('/api/v1/filiais');
    return data;
  },

  /**
   * Cadastra uma nova filial — `POST /api/v1/filiais` (RF017/RF018).
   *
   * <p>O payload já chega normalizado (trim, maiúsculas, CNPJ só com dígitos).
   * Os erros HTTP (400/401/403/409/500) são propagados para a UI tratar.</p>
   */
  async cadastrar(payload: CriarFilialRequest): Promise<CriarFilialResponse> {
    const { data } = await api.post<CriarFilialResponse>('/api/v1/filiais', payload);
    return data;
  },

  /**
   * Obtém o detalhe de uma filial — `GET /api/v1/filiais/{id}`.
   *
   * @remarks Expõe nome, células ativas, timezone, status e auditoria (RF018).
   */
  async obterPorId(id: string): Promise<FilialDetalhe> {
    const { data } = await api.get<FilialDetalhe>(`/api/v1/filiais/${id}`);
    return data;
  },

  /**
   * Altera a quantidade de células ativas — `PATCH /api/v1/filiais/{id}/celulas-ativas` (RF018).
   */
  async alterarCelulasAtivas(id: string, celulasAtivas: number): Promise<FilialDetalhe> {
    const { data } = await api.patch<FilialDetalhe>(`/api/v1/filiais/${id}/celulas-ativas`, {
      celulasAtivas,
    });
    return data;
  },

  /**
   * Ativa/inativa a filial — `PATCH /api/v1/filiais/{id}/status` (RF017).
   * Filial inativa não é aceita em novos agendamentos (RF019 → 409).
   */
  async alterarStatus(id: string, ativo: boolean): Promise<FilialDetalhe> {
    const { data } = await api.patch<FilialDetalhe>(`/api/v1/filiais/${id}/status`, { ativo });
    return data;
  },
};
