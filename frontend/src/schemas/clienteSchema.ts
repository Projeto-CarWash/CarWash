import { z } from 'zod';

import { isValidCnpj, isValidCpf } from '@/lib/validators';

const UF_PATTERN =
  /^(?:AC|AL|AP|AM|BA|CE|DF|ES|GO|MA|MT|MS|MG|PA|PB|PR|PE|PI|RJ|RN|RS|RO|RR|SC|SP|SE|TO)$/;

/**
 * Schema alinhado com backend CarWash.Application.DTOs.Clientes.CreateClienteRequest.
 * Endereço estruturado, celular obrigatório (RF003), data de nascimento com
 * idade 18..110, email opcional.
 */
export const clienteSchema = z.object({
  cpfCnpj: z
    .string()
    .min(1, 'Documento é obrigatório.')
    .refine(
      (val) => {
        const d = val.replace(/\D/g, '');
        return d.length === 11 || d.length === 14;
      },
      { message: 'Informe um CPF (11 dígitos) ou CNPJ (14 dígitos).' },
    )
    .superRefine((val, ctx) => {
      const d = val.replace(/\D/g, '');
      if (d.length === 11 && !isValidCpf(d)) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'CPF inválido. Verifique os dígitos informados.',
        });
      }
      if (d.length === 14 && !isValidCnpj(d)) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'CNPJ inválido. Verifique os dígitos informados.',
        });
      }
    }),

  dataNascimento: z
    .string()
    .min(1, 'Data de nascimento é obrigatória.')
    .refine((val) => val.replace(/\D/g, '').length === 8, {
      message: 'Informe a data completa (DD/MM/AAAA).',
    })
    .superRefine((val, ctx) => {
      const d = val.replace(/\D/g, '');
      if (d.length !== 8) return;
      const day = parseInt(d.slice(0, 2), 10);
      const month = parseInt(d.slice(2, 4), 10) - 1;
      const year = parseInt(d.slice(4, 8), 10);
      const birth = new Date(year, month, day);
      // Verifica se a data é "real" (bissexto + dias por mês)
      if (birth.getFullYear() !== year || birth.getMonth() !== month || birth.getDate() !== day) {
        ctx.addIssue({ code: z.ZodIssueCode.custom, message: 'Data inválida.' });
        return;
      }
      const now = new Date();
      if (birth > now) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Data de nascimento não pode ser futura.',
        });
        return;
      }
      let age = now.getFullYear() - birth.getFullYear();
      const mDiff = now.getMonth() - birth.getMonth();
      if (mDiff < 0 || (mDiff === 0 && now.getDate() < birth.getDate())) age--;
      if (age < 18) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Cliente deve ter pelo menos 18 anos.',
        });
      }
      if (age > 110) {
        ctx.addIssue({
          code: z.ZodIssueCode.custom,
          message: 'Cliente deve ter no máximo 110 anos.',
        });
      }
    }),

  nome: z
    .string()
    .min(1, 'Nome é obrigatório.')
    .refine((val) => val.trim().length >= 3, {
      message: 'Nome deve ter no mínimo 3 caracteres.',
    })
    .refine((val) => val.trim().length <= 100, {
      message: 'Nome deve ter no máximo 100 caracteres.',
    }),

  // Celular OBRIGATÓRIO (RF003 — alinhamento com PR #15)
  celular: z
    .string()
    .min(1, 'Celular é obrigatório.')
    .refine((val) => val.replace(/\D/g, '').length === 11, {
      message: 'Celular deve conter 11 dígitos.',
    }),

  // Telefone opcional
  telefone: z
    .string()
    .optional()
    .refine(
      (val) => {
        if (!val || val.replace(/\D/g, '').length === 0) return true;
        const len = val.replace(/\D/g, '').length;
        return len === 10 || len === 11;
      },
      { message: 'Telefone deve conter 10 ou 11 dígitos.' },
    ),

  // E-mail opcional (DRP não exige)
  email: z
    .string()
    .optional()
    .refine(
      (val) => {
        if (!val || val.trim().length === 0) return true;
        if (val.length < 5 || val.length > 150) return false;
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val);
      },
      { message: 'E-mail inválido (5–150 chars).' },
    ),

  cep: z
    .string()
    .min(1, 'CEP é obrigatório.')
    .refine((val) => val.replace(/\D/g, '').length === 8, {
      message: 'CEP deve conter 8 dígitos.',
    }),

  logradouro: z
    .string()
    .min(1, 'Logradouro é obrigatório.')
    .max(150, 'Logradouro deve ter no máximo 150 caracteres.'),

  numero: z
    .string()
    .min(1, 'Número é obrigatório.')
    .max(20, 'Número deve ter no máximo 20 caracteres.'),

  complemento: z.string().max(100, 'Complemento deve ter no máximo 100 caracteres.').optional(),

  bairro: z
    .string()
    .min(1, 'Bairro é obrigatório.')
    .max(100, 'Bairro deve ter no máximo 100 caracteres.'),

  cidade: z
    .string()
    .min(1, 'Cidade é obrigatória.')
    .max(100, 'Cidade deve ter no máximo 100 caracteres.'),

  uf: z
    .string()
    .min(1, 'UF é obrigatória.')
    .transform((v) => v.toUpperCase())
    .refine((v) => UF_PATTERN.test(v), { message: 'UF inválida (use sigla dos 27 estados).' }),
});

export type ClienteFormData = z.infer<typeof clienteSchema>;
