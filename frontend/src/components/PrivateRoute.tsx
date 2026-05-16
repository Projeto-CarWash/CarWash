import { Navigate } from 'react-router-dom';

import { useAuth } from '../hooks/useAuth';

import type { ReactNode } from 'react';

interface PrivateRouteProps {
  children: ReactNode;
}

/**
 * Guarda de rota — redireciona para /login se o usuário não estiver autenticado.
 * Exibe um loading enquanto restaura sessão do localStorage.
 */
export default function PrivateRoute({ children }: PrivateRouteProps) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          height: '100vh',
          backgroundColor: 'var(--color-background)',
        }}
      >
        <div
          style={{
            width: 40,
            height: 40,
            border: '3px solid var(--color-border)',
            borderTopColor: 'var(--color-primary)',
            borderRadius: '50%',
            animation: 'spin 0.7s linear infinite',
          }}
        />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
