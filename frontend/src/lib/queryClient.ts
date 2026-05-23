import { QueryClient } from '@tanstack/react-query';

/**
 * QueryClient central do app. Cache moderado para listas de apoio
 * (filiais, serviços, veículos) que mudam pouco durante uma sessão.
 *
 * <p>Não há retry em mutations — o usuário decide reenviar; e o backend é
 * a fonte de verdade para conflitos (409/422), reenvio automático poderia
 * mascarar o erro real.</p>
 */
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      retry: 1,
      refetchOnWindowFocus: false,
    },
    mutations: {
      retry: false,
    },
  },
});
