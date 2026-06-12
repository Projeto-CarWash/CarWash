import api from './api';

import type { CriarResponsavelPayload, Responsavel } from '@/types/responsavel';

/** Resposta dos endpoints de atualização/status — campo de id é `responsavelId`. */
interface ResponsavelApiResponse {
  responsavelId: string;
  clienteTitularId: string;
  nome: string;
  documento: string;
  telefone?: string | null;
  email?: string | null;
  grauVinculo: Responsavel['grauVinculo'];
  ativo: boolean;
  criadoEm: string;
}

function mapResponse(data: ResponsavelApiResponse): Responsavel {
  return {
    id: data.responsavelId,
    nome: data.nome,
    documento: data.documento,
    telefone: data.telefone,
    email: data.email,
    grauVinculo: data.grauVinculo,
    criadoEm: data.criadoEm,
  };
}

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

  /**
   * Atualiza o cadastro do responsável (RF023/RF024). O documento é imutável —
   * se enviado, o backend o ignora.
   * `PUT /api/v1/clientes/{clienteId}/responsaveis/{id}`
   */
  async atualizar(
    clienteId: string,
    responsavelId: string,
    payload: Omit<CriarResponsavelPayload, 'documento'>,
  ): Promise<Responsavel> {
    const { data } = await api.put<ResponsavelApiResponse>(
      `/api/v1/clientes/${clienteId}/responsaveis/${responsavelId}`,
      payload,
    );
    return mapResponse(data);
  },

  /**
   * Ativa/inativa o responsável (RF023/RF024). Inativo não pode ser
   * selecionado em novos agendamentos.
   * `PATCH /api/v1/clientes/{clienteId}/responsaveis/{id}/status`
   */
  async alterarStatus(
    clienteId: string,
    responsavelId: string,
    ativo: boolean,
  ): Promise<Responsavel> {
    const { data } = await api.patch<ResponsavelApiResponse>(
      `/api/v1/clientes/${clienteId}/responsaveis/${responsavelId}/status`,
      { ativo },
    );
    return mapResponse(data);
  },
};
