import api from './api';
import { clienteService } from './clienteService';

import type { VeiculoFormData } from '@/schemas/veiculoSchema';
import type { ListaVeiculos, VeiculoListaItem } from '@/types/veiculo';

export interface ListarVeiculosParams {
  busca?: string;
  clienteId?: string;
  ativo?: boolean;
  pagina?: number;
  tamanhoPagina?: number;
}

// Teto de clientes carregados na agregação. O backend ainda não expõe um
// endpoint dedicado de veículos (GET /api/v1/veiculos), então a listagem é
// montada a partir dos clientes reais e seus veículos. Quando o endpoint
// existir, troque o corpo de `listar` por uma chamada direta a `GET /veiculos`.
const MAX_CLIENTES_AGREGACAO = 100;

function normalizarPlaca(valor: string): string {
  return valor.replace(/[^a-zA-Z0-9]/g, '').toLowerCase();
}

function filtrarPorBusca(itens: VeiculoListaItem[], busca: string): VeiculoListaItem[] {
  const termo = busca.trim().toLowerCase();
  if (!termo) return itens;
  const placaTermo = normalizarPlaca(termo);
  return itens.filter(
    (v) =>
      (placaTermo.length > 0 && normalizarPlaca(v.placa).includes(placaTermo)) ||
      v.modelo.toLowerCase().includes(termo) ||
      v.fabricante.toLowerCase().includes(termo) ||
      v.clienteNome.toLowerCase().includes(termo),
  );
}

export interface Veiculo {
  id: string;
  clienteId: string;
  placa: string;
  modelo: string;
  fabricante: string;
  cor: string;
  ano?: number | null;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
}

// Contrato alinhado ao slice CriarVeiculo do backend (Application/Veiculos/Criar):
// retorna o agregado Veiculo completo, não { id, mensagem, traceId }.
export type CriarVeiculoResponse = Veiculo;

export const veiculoService = {
  async listar(params: ListarVeiculosParams = {}): Promise<ListaVeiculos> {
    const pagina = params.pagina && params.pagina > 0 ? params.pagina : 1;
    const tamanhoPagina =
      params.tamanhoPagina && params.tamanhoPagina > 0 ? params.tamanhoPagina : 20;

    // Carrega os clientes reais e busca o detalhe de cada um (que traz os
    // veículos vinculados), achatando tudo numa lista única de veículos.
    const lista = await clienteService.listar({ tamanhoPagina: MAX_CLIENTES_AGREGACAO });
    const alvo = params.clienteId
      ? lista.itens.filter((c) => c.id === params.clienteId)
      : lista.itens;

    const detalhes = await Promise.all(
      alvo.map((c) => clienteService.obterPorId(c.id).catch(() => null)),
    );

    let itens: VeiculoListaItem[] = detalhes
      .filter((c): c is NonNullable<typeof c> => c !== null)
      .flatMap((c) =>
        c.veiculos.map<VeiculoListaItem>((v) => ({
          id: v.id,
          clienteId: c.id,
          clienteNome: c.nome,
          clienteAtivo: c.ativo,
          placa: v.placa,
          modelo: v.modelo,
          fabricante: v.fabricante,
          cor: v.cor,
          // Campos sem origem no contrato atual de cliente→veículo: o veículo
          // só existe vinculado e ativo. Quando o backend expuser GET /veiculos,
          // ano e o status real do veículo virão preenchidos.
          ano: null,
          ativo: true,
          criadoEm: c.criadoEm,
        })),
      );

    if (params.ativo !== undefined) {
      itens = itens.filter((v) => v.ativo === params.ativo);
    }

    if (params.busca) {
      itens = filtrarPorBusca(itens, params.busca);
    }

    itens.sort(
      (a, b) => a.clienteNome.localeCompare(b.clienteNome) || a.placa.localeCompare(b.placa),
    );

    const total = itens.length;
    const inicio = (pagina - 1) * tamanhoPagina;
    return {
      itens: itens.slice(inicio, inicio + tamanhoPagina),
      total,
      pagina,
      tamanhoPagina,
    };
  },

  async cadastrar(clienteId: string, dados: VeiculoFormData): Promise<CriarVeiculoResponse> {
    const payload = {
      placa: dados.placa, // já normalizado pelo transform do zod
      modelo: dados.modelo,
      fabricante: dados.fabricante,
      cor: dados.cor,
    };
    const { data } = await api.post<CriarVeiculoResponse>(
      `/api/v1/clientes/${clienteId}/veiculos`,
      payload,
    );
    return data;
  },
};
