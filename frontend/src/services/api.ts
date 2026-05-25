import axios, { AxiosError, AxiosHeaders, type InternalAxiosRequestConfig } from 'axios';

import { accessTokenStore } from './accessTokenStore';

const baseURL: string = import.meta.env.VITE_API_URL
  ? String(import.meta.env.VITE_API_URL).trim()
  : '';

const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 10_000,
  withCredentials: true,
});

const refreshClient = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 10_000,
  withCredentials: true,
});

api.interceptors.request.use((config) => {
  const token = accessTokenStore.get();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

type FailedConfig = InternalAxiosRequestConfig & { _retry?: boolean };

interface PendenteFila {
  resolve: (value: unknown) => void;
  reject: (reason?: unknown) => void;
  config: FailedConfig;
}

let refreshEmAndamento: Promise<string | null> | null = null;
const fila: PendenteFila[] = [];

async function executarRefresh(): Promise<string | null> {
  try {
    const { data } = await refreshClient.post<{ accessToken: string }>('/api/v1/auth/refresh');
    const token = data.accessToken;
    accessTokenStore.set(token);
    return token;
  } catch {
    accessTokenStore.clear();
    return null;
  }
}

function aplicarBearer(config: FailedConfig, token: string): FailedConfig {
  const headers = AxiosHeaders.from(config.headers);
  headers.set('Authorization', `Bearer ${token}`);
  config.headers = headers;
  return config;
}

function processarFila(token: string | null, error: unknown): void {
  while (fila.length > 0) {
    const item = fila.shift()!;
    if (token) {
      api.request(aplicarBearer(item.config, token)).then(item.resolve, item.reject);
    } else {
      item.reject(error);
    }
  }
}

function redirecionarParaLogin(): void {
  if (typeof window === 'undefined') {
    return;
  }
  if (window.location.pathname !== '/login') {
    window.location.href = '/login';
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!(error instanceof AxiosError) || !error.response || !error.config) {
      return Promise.reject(error instanceof Error ? error : new Error(String(error)));
    }

    const status = error.response.status;
    const config = error.config as FailedConfig;
    const isAuthEndpoint = typeof config.url === 'string' && config.url.startsWith('/api/v1/auth/');

    if (status === 401 && !config._retry && !isAuthEndpoint) {
      config._retry = true;

      if (!refreshEmAndamento) {
        refreshEmAndamento = executarRefresh();
        const token = await refreshEmAndamento;
        refreshEmAndamento = null;
        processarFila(token, error);

        if (!token) {
          redirecionarParaLogin();
          return Promise.reject(error);
        }

        return api.request(aplicarBearer(config, token));
      }

      return new Promise((resolve, reject) => {
        fila.push({ resolve, reject, config });
      });
    }

    return Promise.reject(error);
  },
);

export default api;
