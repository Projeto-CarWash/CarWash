import { Loader2 } from 'lucide-react';
import { Navigate } from 'react-router-dom';

import { useAuth } from '@/hooks/useAuth';

import type { ReactNode } from 'react';

interface PrivateRouteProps {
  children: ReactNode;
}

/**
 * Guarda de rotas autenticadas. Redireciona para `/login` caso não haja sessão.
 * Exibe spinner enquanto a sessão é restaurada do localStorage.
 */
export default function PrivateRoute({ children }: PrivateRouteProps) {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div
        className="flex h-screen items-center justify-center bg-background"
        role="status"
        aria-live="polite"
        aria-label="Carregando sessão"
      >
        <Loader2 className="h-8 w-8 animate-spin text-primary" aria-hidden="true" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
