import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import { BrowserRouter } from 'react-router-dom';

import type { ReactElement, ReactNode } from 'react';

/**
 * Renderiza um componente com os providers que o app precisa em runtime
 * (TanStack Query + Router). Cada chamada cria um `QueryClient` isolado,
 * sem retry, para que os testes não compartilhem cache nem aguardem retries.
 */
export function renderComProviders(ui: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>{children}</BrowserRouter>
      </QueryClientProvider>
    );
  }

  return render(ui, { wrapper: Wrapper });
}
