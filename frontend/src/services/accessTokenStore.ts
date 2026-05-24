/**
 * Store em memória do access token JWT. Mantemos o token fora do localStorage
 * porque o backend usa cookie httpOnly para o refresh, e mover o access para
 * localStorage o expõe a ataques XSS. Recarregar a página perde o access; o
 * AuthProvider faz `refresh()` no boot para restaurar a sessão a partir do
 * cookie httpOnly.
 */

type Listener = (token: string | null) => void;

let currentToken: string | null = null;
const listeners = new Set<Listener>();

export const accessTokenStore = {
  get(): string | null {
    return currentToken;
  },

  set(token: string | null): void {
    currentToken = token;
    listeners.forEach((listener) => listener(token));
  },

  clear(): void {
    currentToken = null;
    listeners.forEach((listener) => listener(null));
  },

  subscribe(listener: Listener): () => void {
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
    };
  },
};
