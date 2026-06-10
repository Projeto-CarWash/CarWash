import api from './api';

import type { CriarResponsavelPayload, Responsavel } from '@/types/responsavel';

export const responsavelService = {
  /**
   * Lista todos os responsáveis vinculados a um cliente específico.
   * `GET /api/v1/clientes/{clienteId}/responsaveis`
   */
  async listarPorCliente(clienteId: string): Promise<Responsavel[]> {
    const { data } = await api.get<Responsavel[]>(`/api/v1/clientes/${clienteId}/responsaveis`);
    return data;
  },

  /**
   * Cadastra um novo responsável para um cliente específico.
   * `POST /api/v1/clientes/{clienteId}/responsaveis`
   */
  async criar(clienteId: string, payload: CriarResponsavelPayload): Promise<Responsavel> {
    const { data } = await api.post<Responsavel>(
      `/api/v1/clientes/${clienteId}/responsaveis`,
      payload,
    );
    return data;
  },
};
