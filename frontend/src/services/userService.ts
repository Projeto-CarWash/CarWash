import api from './api';

import type { PerfilUsuario } from '@/types/auth';
import type { CriarUsuarioCommand, UsuarioResponse } from '@/types/user';

export interface AlterarUsuarioRequest {
  nome: string;
  email: string;
  perfil: PerfilUsuario;
}

export interface ListaUsuariosResponse {
  itens: UsuarioResponse[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}

/**
 * Service de usuários internos (RF014). O cliente axios central (services/api.ts)
 * anexa o Bearer e cuida do refresh transparente no 401.
 */
export const userService = {
  async create(command: CriarUsuarioCommand): Promise<UsuarioResponse> {
    const { data } = await api.post<UsuarioResponse>('/api/v1/usuarios', command);
    return data;
  },

  async list(params: {
    busca?: string;
    ativo?: boolean;
    pagina?: number;
    tamanhoPagina?: number;
  }): Promise<ListaUsuariosResponse> {
    const { data } = await api.get<ListaUsuariosResponse>('/api/v1/usuarios', { params });
    return data;
  },

  async getById(id: string): Promise<UsuarioResponse> {
    const { data } = await api.get<UsuarioResponse>(`/api/v1/usuarios/${id}`);
    return data;
  },

  async update(id: string, request: AlterarUsuarioRequest): Promise<UsuarioResponse> {
    const { data } = await api.put<UsuarioResponse>(`/api/v1/usuarios/${id}`, request);
    return data;
  },

  async updateStatus(
    id: string,
    ativo: boolean,
  ): Promise<{ id: string; ativo: boolean; atualizadoEm: string }> {
    const { data } = await api.patch<{ id: string; ativo: boolean; atualizadoEm: string }>(
      `/api/v1/usuarios/${id}/status`,
      { ativo },
    );
    return data;
  },
};
