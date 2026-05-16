import { createContext, useCallback, useEffect, useMemo, useState } from 'react';

import { authService } from '../services/authService';

import type { ReactNode } from 'react';
import type { User } from '../types/auth';

interface AuthContextData {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

export const AuthContext = createContext<AuthContextData | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Restaura sessão do localStorage na montagem
  useEffect(() => {
    const storedToken = localStorage.getItem('carwash_token');
    const storedUser = localStorage.getItem('carwash_user');

    if (storedToken && storedUser) {
      try {
        setToken(storedToken);
        setUser(JSON.parse(storedUser) as User);
      } catch {
        localStorage.removeItem('carwash_token');
        localStorage.removeItem('carwash_user');
      }
    }

    setIsLoading(false);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await authService.login({ email, password });

    localStorage.setItem('carwash_token', response.token);
    localStorage.setItem('carwash_user', JSON.stringify(response.user));

    setToken(response.token);
    setUser(response.user);
  }, []);

  const logout = useCallback(() => {
    authService.logout();
    setToken(null);
    setUser(null);
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
