import api from './api';

import type { LoginCommand, LoginResponse } from '@/types/auth';

/**
 * Adapter para o endpoint POST /api/v1/auth/login (RF001).
 * Contrato definido em backend/src/CarWash.Application/Auth/Login/*.
 */
export const authService = {
  async login(command: LoginCommand): Promise<LoginResponse> {
    const { data } = await api.post<LoginResponse>('/api/v1/auth/login', command);
    return data;
  },

  logout(): void {
    localStorage.removeItem('carwash_token');
    localStorage.removeItem('carwash_user');
  },

  isAuthenticated(): boolean {
    const token = localStorage.getItem('carwash_token');
    return !!token && token.length > 0;
  },
};
