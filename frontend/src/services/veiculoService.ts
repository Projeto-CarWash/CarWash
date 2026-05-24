import api from './api';

import type { VeiculoFormData } from '@/schemas/veiculoSchema';
import type { ListaVeiculos } from '@/types/veiculo';

export interface Veiculo {
  id: string;
  clienteId: string;
  placa: string;
  modelo: string;
  fabricante: string;
  marca: string;
  cor: string;
  observacoes?: string;
  ativo: boolean;
  criadoEm?: string;
  atualizadoEm?: string;
}

export interface CriarVeiculoResponse {
  id: string;
  mensagem: string;
  traceId: string;
}

/**
 * Service de veículos (RF004/RF005).
 *
 * <p><strong>DEPENDÊNCIA PENDENTE (card 131):</strong> o endpoint
 * <code>GET /api/v1/veiculos?clienteId=</code> ainda NÃO existe no backend.
 * Alinhar com `dev-dotnet-carwash` o contrato de listagem por cliente antes
 * de o formulário de agendamento operar de ponta a ponta.</p>
 *
 * <p>A função abaixo já está escrita no padrão de `clienteService` e passará
 * a funcionar assim que o endpoint for entregue — nenhum mock silencioso é
 * feito; sem o backend a chamada retorna 404 e a UI exibe o estado de erro.</p>
 */
export const ENDPOINT_VEICULOS_PENDENTE = true as const;

export const veiculoService = {
  async cadastrar(clienteId: string, dados: VeiculoFormData): Promise<CriarVeiculoResponse> {
    const payload = {
      placa: dados.placa, // already normalized by zod schema transform
      modelo: dados.modelo,
      fabricante: dados.fabricante,
      cor: dados.cor,
      observacoes: dados.observacoes ?? undefined,
    };
    const { data } = await api.post<CriarVeiculoResponse>(
      `/api/v1/clientes/${clienteId}/veiculos`,
      payload,
    );
    return data;
  },

  /**
   * Lista veículos de um cliente.
   *
   * @remarks Depende de `GET /api/v1/veiculos` (pendente — ver acima).
   */
  async listarPorCliente(clienteId: string): Promise<ListaVeiculos> {
    const { data } = await api.get<ListaVeiculos>('/api/v1/veiculos', {
      params: { clienteId, tamanhoPagina: 100 },
    });
    return data;
  },
};
