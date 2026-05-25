import axios, { AxiosError, AxiosHeaders, type InternalAxiosRequestConfig } from 'axios';

import { accessTokenStore } from './accessTokenStore';

/**
 * Cliente HTTP central. O baseURL é vazio por padrão para que todas as
 * chamadas usem caminhos absolutos (`/api/v1/...`) — em dev o Vite faz
 * proxy de `/api` → backend (ver vite.config.ts); em hom/prod o nginx
 * encaminha para o serviço backend. `VITE_API_URL` pode sobrescrever.
 *
 * <p><strong>withCredentials:</strong> obrigatório para enviar o cookie
 * httpOnly de refresh token no domínio cross-origin (dev: 5173 → 8080).</p>
 */
function normalizarBaseUrl(raw: string | undefined): string {
  if (!raw) {
    return '';
  }

  const trimmed = raw.trim();
  if (trimmed === '/api' || trimmed === '/api/') {
    // Endpoints já usam caminho absoluto "/api/v1/...".
    return '';
  }

  return trimmed.endsWith('/') ? trimmed.slice(0, -1) : trimmed;
}

const baseURL: string = normalizarBaseUrl(import.meta.env.VITE_API_URL);

const api = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 10_000,
  withCredentials: true,
});

// Cliente "cru" para chamadas de refresh — sem interceptor para evitar
// recursão infinita (o interceptor de 401 chama /refresh, e essa chamada
// não deve passar pelo próprio interceptor).
const refreshClient = axios.create({
  baseURL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 10_000,
  withCredentials: true,
});

// Bearer token automático.
api.interceptors.request.use((config) => {
  const token = accessTokenStore.get();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Refresh on 401: enfileira concorrentes, tenta /refresh uma vez, replay.
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
