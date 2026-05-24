import { createContext } from 'react';

import type { LoginCommand, UsuarioLogado } from '@/types/auth';

export interface AuthContextData {
  user: UsuarioLogado | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (command: LoginCommand) => Promise<void>;
  logout: () => Promise<void>;
}

/**
 * Context puro — exportado isoladamente para preservar fast-refresh
 * (regra react-refresh/only-export-components). O provider fica em
 * AuthProvider.tsx e consumidores usam o hook `useAuth`.
 */
export const AuthContext = createContext<AuthContextData | undefined>(undefined);
