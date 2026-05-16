import type { AuthResponse, LoginCredentials } from '../types/auth';

// import api from './api';

/**
 * Serviço de autenticação — mockado para desenvolvimento
 * Preparado para consumir API REST via Axios quando o backend estiver pronto
 */
export const authService = {
  /**
   * Realiza login com email e senha
   * Mock: aceita admin@carwash.com / 123456
   */
  async login(credentials: LoginCredentials): Promise<AuthResponse> {
    // Simulação de latência de rede
    await new Promise((resolve) => setTimeout(resolve, 1500));

    // --- Mock: substituir por chamada real ---
    // const response = await api.post<AuthResponse>('/auth/login', credentials);
    // return response.data;

    if (
      credentials.email === 'admin@carwash.com' &&
      credentials.password === '123456'
    ) {
      return {
        token: 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwibmFtZSI6IkFkbWluIENhcldhc2giLCJlbWFpbCI6ImFkbWluQGNhcndhc2guY29tIiwicm9sZSI6ImFkbWluIiwiaWF0IjoxNzE2NTAwMDAwLCJleHAiOjE3MTY1ODY0MDB9.mock-signature',
        user: {
          id: '1',
          name: 'Admin CarWash',
          email: credentials.email,
          role: 'admin',
        },
      };
    }

    throw new Error('Credenciais inválidas. Verifique seu e-mail e senha.');
  },

  /**
   * Remove token e dados do usuário
   */
  logout(): void {
    localStorage.removeItem('carwash_token');
    localStorage.removeItem('carwash_user');
  },

  /**
   * Verifica se há token válido armazenado
   */
  isAuthenticated(): boolean {
    const token = localStorage.getItem('carwash_token');
    return token !== null && token.length > 0;
  },
};
