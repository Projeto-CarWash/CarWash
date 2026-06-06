import { z } from 'zod';

import { isValidCnpj } from '@/lib/validators';

/**
 * Schema de cadastro de filial (RF017/RF018).
 *
 * <p>Validações locais para UX — a verdade é do backend (RAT03). Espelha o
 * contrato real do `POST /api/v1/filiais`: identificação (nome/código/CNPJ),
 * capacidade (células ativas) e endereço estruturado (CEP, logradouro, número,
 * complemento, bairro, cidade, UF). Normaliza antes do envio: aplica `trim`,
 * força maiúsculas em código/UF e reduz CEP/CNPJ a dígitos.</p>
 */

const UF_PATTERN =
  /^(?:AC|AL|AP|AM|BA|CE|DF|ES|GO|MA|MT|MS|MG|PA|PB|PR|PE|PI|RJ|RN|RS|RO|RR|SC|SP|SE|TO)$/;

export const filialSchema = z.object({
  nome: z
    .string()
    .trim()
    .min(1, 'Nome da filial é obrigatório.')
    .refine((v) => v.length >= 3 && v.length <= 120, {
      message: 'Nome da filial deve ter entre 3 e 120 caracteres.',
    }),

  codigo: z
    .string()
    .trim()
    .min(1, 'Código da filial é obrigatório.')
    .transform((v) => v.toUpperCase())
    .refine((v) => /^[A-Z0-9]{2,20}$/.test(v), {
      message: 'Código deve conter de 2 a 20 caracteres alfanuméricos (A-Z, 0-9).',
    }),

  // Opcional; quando preenchido, valida o CNPJ e mantém apenas dígitos.
  cnpj: z
    .string()
    .optional()
    .transform((v) => (v ?? '').replace(/\D/g, ''))
    .refine((v) => v.length === 0 || (v.length === 14 && isValidCnpj(v)), {
      message: 'CNPJ inválido. Verifique os dados informados.',
    }),

  celulasAtivas: z.preprocess(
    (v) => {
      if (typeof v === 'string') {
        const t = v.trim();
        return t === '' ? undefined : Number(t);
      }
      return v;
    },
    z
      .number({ error: 'Células ativas é obrigatório.' })
      .int('Células ativas deve ser um número inteiro entre 1 e 100.')
      .min(1, 'Células ativas deve ser um número inteiro entre 1 e 100.')
      .max(100, 'Células ativas deve ser um número inteiro entre 1 e 100.'),
  ),

  // ── Endereço estruturado ──────────────────────────────────────────────────
  cep: z
    .string()
    .min(1, 'CEP é obrigatório.')
    .refine((v) => v.replace(/\D/g, '').length === 8, { message: 'CEP deve conter 8 dígitos.' }),

  logradouro: z
    .string()
    .trim()
    .min(1, 'Logradouro é obrigatório.')
    .refine((v) => v.length >= 3 && v.length <= 150, {
      message: 'Logradouro deve ter entre 3 e 150 caracteres.',
    }),

  numero: z
    .string()
    .trim()
    .min(1, 'Número é obrigatório.')
    .max(20, 'Número deve ter no máximo 20 caracteres.'),

  complemento: z
    .string()
    .trim()
    .max(100, 'Complemento deve ter no máximo 100 caracteres.')
    .optional(),

  bairro: z
    .string()
    .trim()
    .min(1, 'Bairro é obrigatório.')
    .refine((v) => v.length >= 2 && v.length <= 100, {
      message: 'Bairro deve ter entre 2 e 100 caracteres.',
    }),

  cidade: z
    .string()
    .trim()
    .min(1, 'Cidade é obrigatória.')
    .refine((v) => v.length >= 2 && v.length <= 100, {
      message: 'Cidade deve ter entre 2 e 100 caracteres.',
    }),

  uf: z
    .string()
    .trim()
    .min(1, 'UF inválida. Informe a sigla com 2 letras.')
    .transform((v) => v.toUpperCase())
    .refine((v) => UF_PATTERN.test(v), {
      message: 'UF inválida. Informe a sigla com 2 letras.',
    }),
});

export type FilialFormInput = z.input<typeof filialSchema>;
export type FilialFormData = z.infer<typeof filialSchema>;

/**
 * Schema de edição de filial (RF018). O backend só permite ajustar a
 * quantidade de células ativas (`PATCH /api/v1/filiais/{id}/celulas-ativas`);
 * reaproveita a mesma regra do cadastro.
 */
export const editarFilialSchema = filialSchema.pick({ celulasAtivas: true });

export type EditarFilialFormInput = z.input<typeof editarFilialSchema>;
export type EditarFilialFormData = z.infer<typeof editarFilialSchema>;
