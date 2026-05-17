import api from './api';

import type { CriarUsuarioCommand, UsuarioResponse } from '@/types/user';

/**
 * Service de usuários internos (RF014).
 * O cliente axios central (`services/api.ts`) já anexa o Bearer token e
 * encaminha 401 → /login automaticamente.
 */
export const userService = {
  async create(command: CriarUsuarioCommand): Promise<UsuarioResponse> {
    const { data } = await api.post<UsuarioResponse>('/api/v1/usuarios', command);
    return data;
  },
};
