import { z } from 'zod';

import { isValidCnpj, isValidCpf } from '@/lib/validators';

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
    .refine(
      (val: string) => {
        const d = val.replace(/\D/g, '');
        if (d.length !== 8) return true;
        const day = parseInt(d.slice(0, 2), 10);
        const month = parseInt(d.slice(2, 4), 10) - 1;
        const year = parseInt(d.slice(4, 8), 10);
        const birth = new Date(year, month, day);
        const now = new Date();
        let age = now.getFullYear() - birth.getFullYear();
        const mDiff = now.getMonth() - birth.getMonth();
        if (mDiff < 0 || (mDiff === 0 && now.getDate() < birth.getDate())) age--;
        return age >= 18;
      },
      { message: 'Cliente deve ser maior de 18 anos.' },
    ),

  nome: z
    .string()
    .min(1, 'Nome é obrigatório.')
    .min(3, 'Nome deve ter entre 3 e 100 caracteres.')
    .max(100, 'Nome deve ter entre 3 e 100 caracteres.')
    .refine((val) => val.trim().length >= 3, {
      message: 'Nome não pode conter apenas espaços.',
    }),

  telefone: z
    .string()
    .min(1, 'Telefone é obrigatório.')
    .refine(
      (val) => {
        const d = val.replace(/\D/g, '');
        return d.length === 10;
      },
      { message: 'Telefone deve conter 10 dígitos válidos.' },
    ),

  celular: z
    .string()
    .optional()
    .refine(
      (val) => {
        if (!val || val.replace(/\D/g, '').length === 0) return true;
        return val.replace(/\D/g, '').length === 11;
      },
      { message: 'Celular deve conter 11 dígitos válidos.' },
    ),

  email: z
    .string()
    .min(1, 'E-mail é obrigatório.')
    .min(5, 'E-mail deve ter entre 5 e 150 caracteres.')
    .max(150, 'E-mail deve ter entre 5 e 150 caracteres.')
    .email('E-mail em formato inválido.'),

  cep: z
    .string()
    .min(1, 'CEP é obrigatório.')
    .refine((val) => val.replace(/\D/g, '').length === 8, {
      message: 'CEP deve conter 8 dígitos.',
    }),

  cidade: z.string().min(1, 'Cidade é obrigatória.'),

  rua: z.string().min(1, 'Rua é obrigatória.'),

  numero: z.string().min(1, 'Número é obrigatório.'),

  observacoes: z.string().max(500, 'Observações não podem ultrapassar 500 caracteres.').optional(),
});

export type ClienteFormData = z.infer<typeof clienteSchema>;
