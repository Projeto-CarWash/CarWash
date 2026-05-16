import { useContext } from 'react';

import { AuthContext } from '../contexts/AuthContext';

/**
 * Hook para acessar o contexto de autenticação.
 * Deve ser usado dentro de um <AuthProvider>.
 */
export function useAuth() {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error('useAuth deve ser usado dentro de um AuthProvider');
  }

  return context;
}
