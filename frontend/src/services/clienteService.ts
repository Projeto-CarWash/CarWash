import api from './api';

import type { ClienteFormData } from '@/schemas/clienteSchema';

export interface ClienteResumo {
  id: string;
  nome: string;
  cpf?: string;
  cnpj?: string;
  celular: string;
  email?: string;
  cidade: string;
  uf: string;
  ativo: boolean;
  criadoEm: string;
}

export interface ClienteDetalhe {
  id: string;
  nome: string;
  dataNascimento: string;
  cpf?: string;
  cnpj?: string;
  telefone?: string;
  celular: string;
  email?: string;
  endereco: {
    cep: string;
    logradouro: string;
    numero: string;
    complemento?: string;
    bairro: string;
    cidade: string;
    uf: string;
  };
  veiculos?: Array<{
    id: string;
    placa: string;
    modelo: string;
    fabricante: string;
    cor: string;
  }>;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
}

export interface ListaClientes {
  itens: ClienteResumo[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
}

function onlyDigits(value: string | undefined): string | undefined {
  if (!value) return undefined;
  const d = value.replace(/\D/g, '');
  return d.length > 0 ? d : undefined;
}

function ddmmaaaaParaIso(ddmmaaaa: string): string {
  const d = ddmmaaaa.replace(/\D/g, '');
  if (d.length !== 8) {
    throw new Error('Data de nascimento inválida.');
  }
  const day = d.slice(0, 2);
  const month = d.slice(2, 4);
  const year = d.slice(4, 8);
  return `${year}-${month}-${day}`;
}

function toCreatePayload(data: ClienteFormData) {
  const docDigits = onlyDigits(data.cpfCnpj) ?? '';
  const isCpf = docDigits.length === 11;
  return {
    nome: data.nome.trim(),
    dataNascimento: ddmmaaaaParaIso(data.dataNascimento),
    cpf: isCpf ? docDigits : undefined,
    cnpj: !isCpf ? docDigits : undefined,
    celular: onlyDigits(data.celular)!,
    telefone: onlyDigits(data.telefone),
    email: data.email?.trim() ? data.email.trim().toLowerCase() : undefined,
    endereco: {
      cep: onlyDigits(data.cep)!,
      logradouro: data.logradouro.trim(),
      numero: data.numero.trim(),
      complemento: data.complemento?.trim() ? data.complemento.trim() : undefined,
      bairro: data.bairro.trim(),
      cidade: data.cidade.trim(),
      uf: data.uf.trim().toUpperCase(),
    },
    veiculos: data.veiculos.map(v => ({
      placa: v.placa,
      modelo: v.modelo,
      fabricante: v.fabricante,
      cor: v.cor
    })),
  };
}

export const clienteService = {
  async criar(data: ClienteFormData): Promise<{ id: string }> {
    const payload = toCreatePayload(data);
    const { data: resp } = await api.post<{ id: string }>('/api/v1/clientes', payload);
    return { id: resp.id };
  },

  async listar(params: {
    busca?: string;
    ativo?: boolean;
    pagina?: number;
    tamanhoPagina?: number;
  }): Promise<ListaClientes> {
    const { data } = await api.get<ListaClientes>('/api/v1/clientes', { params });
    return data;
  },

  async obterPorId(id: string): Promise<ClienteDetalhe> {
    const { data } = await api.get<ClienteDetalhe>(`/api/v1/clientes/${id}`);
    return data;
  },

  async alterarStatus(id: string, ativo: boolean): Promise<ClienteDetalhe> {
    const { data } = await api.patch<ClienteDetalhe>(`/api/v1/clientes/${id}/status`, { ativo });
    return data;
  },
};
