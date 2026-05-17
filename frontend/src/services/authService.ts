import { accessTokenStore } from './accessTokenStore';
import api from './api';

import type { LoginCommand, LoginResponse, RefreshResponse } from '@/types/auth';

/**
 * Adapter para os endpoints de autenticação (RF001).
 * Contrato definido em backend/src/CarWash.Application/Auth/*.
 *
 * Refresh token vive APENAS no cookie httpOnly <c>carwash_refresh_token</c>;
 * o frontend nunca o lê nem grava. O access token JWT fica em memória
 * (<see cref="accessTokenStore"/>) — recarregar a página obriga um <c>refresh()</c>.
 */
export const authService = {
  async login(command: LoginCommand): Promise<LoginResponse> {
    const { data } = await api.post<LoginResponse>('/api/v1/auth/login', command);
    accessTokenStore.set(data.accessToken);
    return data;
  },

  async refresh(): Promise<RefreshResponse | null> {
    try {
      const { data } = await api.post<RefreshResponse>('/api/v1/auth/refresh');
      accessTokenStore.set(data.accessToken);
      return data;
    } catch {
      accessTokenStore.clear();
      return null;
    }
  },

  async logout(): Promise<void> {
    try {
      await api.post('/api/v1/auth/logout');
    } finally {
      accessTokenStore.clear();
    }
  },

  isAuthenticated(): boolean {
    return accessTokenStore.get() !== null;
  },
};
