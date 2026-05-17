import { useCallback, useMemo, useState } from 'react';

import { authService } from '@/services/authService';

import { AuthContext } from './AuthContext';

import type { LoginCommand, UsuarioLogado } from '@/types/auth';
import type { ReactNode } from 'react';

const STORAGE_TOKEN_KEY = 'carwash_token';
const STORAGE_USER_KEY = 'carwash_user';

interface AuthProviderProps {
  children: ReactNode;
}

interface RestoredSession {
  token: string | null;
  user: UsuarioLogado | null;
}

/**
 * Lê do localStorage uma única vez na inicialização do estado.
 * Mantemos fora do componente para não recriar a cada render.
 */
function restoreSession(): RestoredSession {
  if (typeof window === 'undefined') {
    return { token: null, user: null };
  }
  const storedToken = localStorage.getItem(STORAGE_TOKEN_KEY);
  const storedUser = localStorage.getItem(STORAGE_USER_KEY);
  if (!storedToken || !storedUser) {
    return { token: null, user: null };
  }
  try {
    return { token: storedToken, user: JSON.parse(storedUser) as UsuarioLogado };
  } catch {
    localStorage.removeItem(STORAGE_TOKEN_KEY);
    localStorage.removeItem(STORAGE_USER_KEY);
    return { token: null, user: null };
  }
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [{ token, user }, setSession] = useState<RestoredSession>(() => restoreSession());

  const login = useCallback(async (command: LoginCommand) => {
    const response = await authService.login(command);
    localStorage.setItem(STORAGE_TOKEN_KEY, response.accessToken);
    localStorage.setItem(STORAGE_USER_KEY, JSON.stringify(response.usuario));
    setSession({ token: response.accessToken, user: response.usuario });
  }, []);

  const logout = useCallback(() => {
    authService.logout();
    setSession({ token: null, user: null });
  }, []);

  const value = useMemo(
    () => ({
      user,
      token,
      isAuthenticated: token !== null,
      // Restauração é síncrona (lazy init do useState); manter o flag
      // por compatibilidade com PrivateRoute, mas já fica `false`.
      isLoading: false,
      login,
      logout,
    }),
    [user, token, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
