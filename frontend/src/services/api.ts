import axios from 'axios';

/**
 * Cliente HTTP central. O baseURL é vazio por padrão para que todas as
 * chamadas usem caminhos absolutos (`/api/v1/...`) — em dev o Vite faz
 * proxy de `/api` → backend (ver vite.config.ts); em hom/prod o nginx
 * encaminha para o serviço backend. Se necessário, `VITE_API_URL` pode
 * sobrescrever (ex.: testes apontando para outra origem).
 */
const baseURL: string = import.meta.env.VITE_API_URL ?? '';

const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 10_000,
});

// Bearer token automático.
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('carwash_token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error: unknown) => Promise.reject(error instanceof Error ? error : new Error(String(error))),
);

// 401 → limpa sessão e força volta ao /login.
// Evita loop se já estamos na tela de login.
api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 401) {
      localStorage.removeItem('carwash_token');
      localStorage.removeItem('carwash_user');
      if (typeof window !== 'undefined' && window.location.pathname !== '/login') {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error instanceof Error ? error : new Error(String(error)));
  },
);

export default api;
