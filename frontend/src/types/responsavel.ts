// Valores aceitos pelo backend (CarWash.Domain.Enums.GrauVinculo / ToDbValue).
// O handler faz match case-sensitive em FromDbValue, então enviar exatamente
// estas strings em maiúsculas é obrigatório.
export type GrauVinculo =
  | 'RESPONSAVEL_FINANCEIRO'
  | 'RESPONSAVEL_LEGAL'
  | 'PROCURADOR'
  | 'CONJUGE'
  | 'PAI_MAE'
  | 'OUTRO';

/** Ordem de exibição no seletor de grau de vínculo. */
export const GRAUS_VINCULO: readonly GrauVinculo[] = [
  'RESPONSAVEL_FINANCEIRO',
  'RESPONSAVEL_LEGAL',
  'PROCURADOR',
  'CONJUGE',
  'PAI_MAE',
  'OUTRO',
] as const;

/** Rótulos amigáveis (PT-BR) para cada grau de vínculo. */
export const VINCULO_LABELS: Record<GrauVinculo, string> = {
  RESPONSAVEL_FINANCEIRO: 'Responsável Financeiro',
  RESPONSAVEL_LEGAL: 'Responsável Legal',
  PROCURADOR: 'Procurador(a)',
  CONJUGE: 'Cônjuge',
  PAI_MAE: 'Pai/Mãe',
  OUTRO: 'Outro',
};

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
