import { useContext } from 'react';

import { AuthContext } from '@/contexts/AuthContext';

/**
 * Acessa o contexto de autenticação. Use somente dentro de <AuthProvider>.
 */
export function useAuth() {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error('useAuth deve ser usado dentro de um AuthProvider.');
  }

  return context;
}
