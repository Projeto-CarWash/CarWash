import api from './api';

import type { ListaServicos } from '@/types/servico';

/**
 * Service de serviços do catálogo (RF006).
 *
 * <p><strong>DEPENDÊNCIA PENDENTE (card 131):</strong> o endpoint
 * <code>GET /api/v1/servicos</code> ainda NÃO existe no backend. Alinhar com
 * `dev-dotnet-carwash` o contrato de listagem (campos `precoBase` e
 * `duracaoMin` são necessários para o resumo inline de totais estimados).</p>
 *
 * <p>A função está escrita no padrão de `clienteService` e funcionará assim
 * que o endpoint for entregue — sem mock silencioso; até lá a chamada retorna
 * 404 e a UI exibe o estado de erro.</p>
 */
export const ENDPOINT_SERVICOS_PENDENTE = true as const;

export const servicoService = {
  /**
   * Lista os serviços ativos do catálogo.
   *
   * @remarks Depende de `GET /api/v1/servicos` (pendente — ver acima).
   */
  async listar(): Promise<ListaServicos> {
    const { data } = await api.get<ListaServicos>('/api/v1/servicos', {
      params: { ativo: true },
    });
    return data;
  },
};
