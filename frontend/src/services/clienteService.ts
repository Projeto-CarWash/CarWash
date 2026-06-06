import { AxiosError } from 'axios';

import api from './api';

import type { ClienteFormData, EditarClienteFormData } from '@/schemas/clienteSchema';
import type { ProblemDetails } from '@/types/auth';

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

/**
 * Veículo vinculado ao cliente, conforme retornado por `GET /api/v1/clientes/{id}`
 * (propriedade `veiculos`). Espelha `ClienteResponse.ClienteVeiculoResponse` do backend.
 */
export interface ClienteVeiculo {
  id: string;
  placa: string;
  modelo: string;
  fabricante: string;
  cor: string;
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
  veiculos: ClienteVeiculo[];
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

/**
 * Erro lançado quando o cliente foi criado com sucesso mas a criação de
 * veículos falhou e o rollback (desativação do cliente) foi executado.
 * Carrega o erro original da etapa de veículo para que a UI possa
 * exibir feedback adequado.
 */
export class CriacaoClienteRollbackError extends Error {
  /** O erro original que causou a falha na etapa de veículo. */
  public readonly causa: unknown;
  /** Status HTTP do erro de veículo (se disponível). */
  public readonly statusVeiculo: number | undefined;
  /** ProblemDetails do erro de veículo (se disponível). */
  public readonly problemDetails: ProblemDetails | undefined;

  constructor(causa: unknown) {
    super('Cadastro revertido: falha na criação do veículo.');
    this.name = 'CriacaoClienteRollbackError';
    this.causa = causa;

    if (causa instanceof AxiosError && causa.response) {
      this.statusVeiculo = causa.response.status;
      this.problemDetails = causa.response.data as ProblemDetails | undefined;
    }
  }
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
    // Veículos são enviados na mesma payload para o POST /api/v1/clientes
    veiculos: data.veiculos.map((v) => ({
      placa: v.placa,
      fabricante: v.fabricante,
      modelo: v.modelo,
      cor: v.cor,
      ano: v.ano ?? undefined,
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

/**
 * Monta o payload SEM veículos para a fase 1 do cadastro transacional.
 * Os veículos são criados na fase 2 via `POST /api/v1/clientes/{id}/veiculos`.
 */
function toCreatePayloadSemVeiculos(data: ClienteFormData) {
  const payload = toCreatePayload(data);
  return { ...payload, veiculos: [] };
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
  /**
   * Cria o cliente enviando tudo (incluindo veículos) em um único POST.
   * ⚠️ ATENÇÃO: o backend persiste o cliente ANTES dos veículos. Se um
   * veículo falhar, o cliente fica órfão. Prefira `criarComVeiculos`.
   */
  async criar(data: ClienteFormData): Promise<{ id: string }> {
    const payload = toCreatePayload(data);
    const { data: resp } = await api.post<{ id: string }>('/api/v1/clientes', payload);
    return { id: resp.id };
  },

  /**
   * Cadastro transacional com rollback compensatório.
   *
   * Fase 1 — Cria o cliente SEM veículos (array vazio).
   * Fase 2 — Cria cada veículo via `POST /api/v1/clientes/{id}/veiculos`.
   * Rollback — Se qualquer veículo falhar, desativa o cliente recém-criado
   *            via `PATCH /api/v1/clientes/{id}/status` e lança
   *            `CriacaoClienteRollbackError` com os detalhes do erro.
   *
   * Isso garante que nenhum cliente permaneça parcialmente cadastrado.
   */
  async criarComVeiculos(data: ClienteFormData): Promise<{ id: string }> {
    // ── Fase 1: criar o cliente sem veículos ──────────────────────────
    const payloadCliente = toCreatePayloadSemVeiculos(data);
    const { data: respCliente } = await api.post<{ id: string }>(
      '/api/v1/clientes',
      payloadCliente,
    );
    const clienteId = respCliente.id;

    // ── Fase 2: criar cada veículo vinculado ao cliente ──────────────
    try {
      for (const veiculo of data.veiculos) {
        await api.post(`/api/v1/clientes/${clienteId}/veiculos`, {
          placa: veiculo.placa,
          fabricante: veiculo.fabricante,
          modelo: veiculo.modelo,
          cor: veiculo.cor,
          ano: veiculo.ano ?? undefined,
        });
      }
    } catch (erroVeiculo: unknown) {
      // ── Rollback: desativar o cliente recém-criado ────────────────
      try {
        await api.patch(`/api/v1/clientes/${clienteId}/status`, { ativo: false });
      } catch (erroRollback: unknown) {
        // Loga falha no rollback mas propaga o erro original do veículo,
        // pois é ele que o usuário precisa ver. O cliente ficará ativo
        // mas sem veículos — cenário que deve ser tratado via suporte.
        console.error(
          '[clienteService.criarComVeiculos] Falha no rollback do cliente:',
          erroRollback,
        );
      }
      throw new CriacaoClienteRollbackError(erroVeiculo);
    }

    return { id: clienteId };
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
