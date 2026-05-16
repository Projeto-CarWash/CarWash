import { createContext, useCallback, useMemo, useState } from 'react';

import { authService } from '../services/authService';

import type { User } from '../types/auth';
import type { ReactNode } from 'react';

interface AuthContextData {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

// eslint-disable-next-line react-refresh/only-export-components
export const AuthContext = createContext<AuthContextData | undefined>(undefined);

interface AuthProviderProps {
  children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [token, setToken] = useState<string | null>(() => localStorage.getItem('carwash_token'));
  const [user, setUser] = useState<User | null>(() => {
    const storedUser = localStorage.getItem('carwash_user');
    if (storedUser) {
      try {
        return JSON.parse(storedUser) as User;
      } catch {
        localStorage.removeItem('carwash_token');
        localStorage.removeItem('carwash_user');
      }
    }
    return null;
  });
  const isLoading = false;

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
