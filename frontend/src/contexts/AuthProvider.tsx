import { useCallback, useEffect, useMemo, useState } from 'react';

import { accessTokenStore } from '@/services/accessTokenStore';
import { authService } from '@/services/authService';

import { AuthContext } from './AuthContext';

import type { LoginCommand, UsuarioLogado } from '@/types/auth';
import type { ReactNode } from 'react';

interface AuthProviderProps {
  children: ReactNode;
}

interface SessionState {
  user: UsuarioLogado | null;
  token: string | null;
}

/**
 * Provider de sessão. O access token vive em memória (<see cref="accessTokenStore" />)
 * e o refresh token vive em cookie httpOnly. No mount, o provider tenta
 * <c>authService.refresh()</c> para restaurar a sessão a partir do cookie —
 * se o usuário recarregou a página com cookie ainda válido, ele continua logado;
 * caso contrário, fica deslogado e o PrivateRoute redireciona para /login.
 */
export function AuthProvider({ children }: AuthProviderProps) {
  const [{ user, token }, setSession] = useState<SessionState>({ user: null, token: null });
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Boot: tenta restaurar sessão via cookie httpOnly. Roda uma vez.
  useEffect(() => {
    let cancelado = false;

    void (async () => {
      try {
        const refreshed = await authService.refresh();
        if (!cancelado) {
          if (refreshed) {
            setSession({ user: refreshed.usuario, token: refreshed.accessToken });
          }
        }
      } finally {
        if (!cancelado) {
          setIsLoading(false);
        }
      }
    })();

    return () => {
      cancelado = true;
    };
  }, []);

  // Sincronia: se o axios interceptor renovar/limpar o token, mantém o state alinhado.
  useEffect(() => {
    return accessTokenStore.subscribe((newToken) => {
      setSession((prev) => {
        if (prev.token === newToken) {
          return prev;
        }
        // Token apagado externamente (ex.: refresh falhou no 401 interceptor).
        if (newToken === null) {
          return { user: null, token: null };
        }
        return { ...prev, token: newToken };
      });
    });
  }, []);

  const login = useCallback(async (command: LoginCommand) => {
    const response = await authService.login(command);
    setSession({ user: response.usuario, token: response.accessToken });
  }, []);

  const logout = useCallback(async () => {
    await authService.logout();
    setSession({ user: null, token: null });
  }, []);

  const value = useMemo(
    () => ({
      user,
      token,
      isAuthenticated: token !== null,
      isLoading,
      login,
      logout,
    }),
    [user, token, isLoading, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}
