export type GrauVinculo = 'PAI' | 'MAE' | 'CONJUGE' | 'FILHO' | 'SOCIO' | 'FUNCIONARIO' | 'OUTRO';

export interface Responsavel {
  id: string;
  nome: string;
  documento: string; // CPF or CNPJ
  email?: string | null;
  telefone?: string | null;
  grauVinculo: GrauVinculo;
  criadoEm: string;
}

export interface CriarResponsavelPayload {
  nome: string;
  documento: string;
  email?: string | null;
  telefone?: string | null;
  grauVinculo: GrauVinculo;
}
