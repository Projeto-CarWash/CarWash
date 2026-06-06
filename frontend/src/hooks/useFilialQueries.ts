import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { filialService } from '@/services/filialService';

import type { CriarFilialRequest, CriarFilialResponse, FilialDetalhe } from '@/types/filial';

/**
 * Hooks de TanStack Query da feature de Filiais (RF017/RF018).
 *
 * <p>A listagem de gerência usa `useQuery` (todas as filiais). A criação usa
 * `useMutation` sem retry — 409 (identificador duplicado) deve ser exibido, não
 * mascarado por reenvio automático. Em caso de sucesso, invalida tanto a lista
 * de gerência quanto a lista de filiais ativas do agendamento (RF019), para que
 * a filial recém-criada fique disponível imediatamente, sem refresh manual.</p>
 */

/** Lista completa de filiais (ativas e inativas) para a tela de gerência. */
export function useFiliaisLista() {
  return useQuery({
    queryKey: ['filiais', 'lista'],
    queryFn: () => filialService.listarTodas(),
  });
}

/** Detalhe de uma filial para a tela de edição — `GET /api/v1/filiais/{id}`. */
export function useFilial(id: string | undefined) {
  return useQuery({
    queryKey: ['filiais', 'detalhe', id],
    queryFn: () => filialService.obterPorId(id!),
    enabled: !!id,
  });
}

/** Mutation de cadastro de filial — `POST /api/v1/filiais`. */
export function useCriarFilial() {
  const queryClient = useQueryClient();
  return useMutation<CriarFilialResponse, unknown, CriarFilialRequest>({
    mutationFn: (payload) => filialService.cadastrar(payload),
    retry: false,
    onSuccess: () => {
      // Atualiza a listagem de gerência e o seletor de filiais do agendamento:
      // a filial ativa recém-criada precisa aparecer imediatamente (RF019/RN010).
      void queryClient.invalidateQueries({ queryKey: ['filiais'] });
      void queryClient.invalidateQueries({ queryKey: ['agendamento', 'filiais'] });
    },
  });
}

/** Invalida as listas de filiais (gerência + seletor do agendamento). */
function useInvalidarFiliais() {
  const queryClient = useQueryClient();
  return () => {
    void queryClient.invalidateQueries({ queryKey: ['filiais'] });
    void queryClient.invalidateQueries({ queryKey: ['agendamento', 'filiais'] });
  };
}

/** Mutation de ajuste de células ativas — `PATCH /api/v1/filiais/{id}/celulas-ativas` (RF018). */
export function useAlterarCelulasAtivas() {
  const invalidar = useInvalidarFiliais();
  return useMutation<FilialDetalhe, unknown, { id: string; celulasAtivas: number }>({
    mutationFn: ({ id, celulasAtivas }) => filialService.alterarCelulasAtivas(id, celulasAtivas),
    retry: false,
    onSuccess: invalidar,
  });
}
