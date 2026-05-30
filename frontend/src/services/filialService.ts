import api from './api';

import type { ListaFiliais } from '@/types/filial';

/**
 * Service de filiais (RF017/RF018).
 *
 * <p><strong>DEPENDÊNCIA PENDENTE (card 131):</strong> o endpoint
 * <code>GET /api/v1/filiais</code> ainda NÃO existe no backend. Alinhar com
 * `dev-dotnet-carwash` o contrato de listagem — o agendamento exige uma filial
 * (RF019/RN010).</p>
 *
 * <p>A função está escrita no padrão de `clienteService` e funcionará assim
 * que o endpoint for entregue — sem mock silencioso; até lá a chamada retorna
 * 404 e a UI exibe o estado de erro.</p>
 */
export const ENDPOINT_FILIAIS_PENDENTE = true as const;

export const filialService = {
  /**
   * Lista as filiais ativas.
   *
   * @remarks Depende de `GET /api/v1/filiais` (pendente — ver acima).
   */
  async listar(): Promise<ListaFiliais> {
    const { data } = await api.get<ListaFiliais>('/api/v1/filiais', {
      params: { ativo: true },
    });
    return data;
  },
};
