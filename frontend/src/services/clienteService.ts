import api from './api';

import type { ClienteFormData, EditarClienteFormData } from '@/schemas/clienteSchema';

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
  veiculos?: {
    id: string;
    placa: string;
    marca: string;
    modelo: string;
    fabricante: string;
    cor: string;
  }[];
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

/** Converte a data ISO retornada pelo backend (yyyy-MM-dd) para o formato mascarado DD/MM/AAAA. */
export function isoParaDdmmaaaa(iso: string | undefined): string {
  if (!iso) return '';
  const [year, month, day] = iso.slice(0, 10).split('-');
  if (!year || !month || !day) return '';
  return `${day}/${month}/${year}`;
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
    veiculos: data.veiculos.map((v) => ({
      placa: v.placa,
      marca: v.marca,
      modelo: v.modelo,
      cor: v.cor,
      renavam: v.renavam ?? undefined,
      anoModelo: v.anoModelo ?? undefined,
      categoria: v.categoria ?? undefined,
      corHex: v.corHex ?? undefined,
      observacoesAtendimento: v.observacoesAtendimento ?? undefined,
    })),
    // Preferências & Fidelidade
    lembretes: data.lembretes?.length ? data.lembretes : undefined,
    canaisPreferenciais: data.canaisPreferenciais?.length ? data.canaisPreferenciais : undefined,
    observacoesGerais: data.observacoesGerais?.trim() ?? undefined,
    filiados: data.filiados?.length
      ? data.filiados.map((f) => ({
          cpf: f.cpf.replace(/\D/g, ''),
          nome: f.nome.trim(),
          telefone: f.telefone ? f.telefone.replace(/\D/g, '') : undefined,
          email: f.email?.trim() ?? undefined,
        }))
      : undefined,
  };
}

function toUpdatePayload(data: EditarClienteFormData) {
  // CPF/CNPJ e veículos não são enviados: o PUT não os edita (RF003 / decisão de produto).
  return {
    nome: data.nome.trim(),
    dataNascimento: ddmmaaaaParaIso(data.dataNascimento),
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

  async atualizar(id: string, data: EditarClienteFormData): Promise<ClienteDetalhe> {
    const payload = toUpdatePayload(data);
    const { data: resp } = await api.put<ClienteDetalhe>(`/api/v1/clientes/${id}`, payload);
    return resp;
  },

  async alterarStatus(id: string, ativo: boolean): Promise<ClienteDetalhe> {
    const { data } = await api.patch<ClienteDetalhe>(`/api/v1/clientes/${id}/status`, { ativo });
    return data;
  },
};
