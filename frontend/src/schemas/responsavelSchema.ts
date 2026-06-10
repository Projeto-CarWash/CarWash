import { z } from 'zod';

import { isValidCnpj, isValidCpf } from '@/lib/validators';

export const responsavelSchema = z.object({
  nome: z
    .string()
    .min(1, 'Nome é obrigatório.')
    .refine((val) => val.trim().length >= 3, {
      message: 'Nome deve ter no mínimo 3 caracteres.',
    })
    .refine((val) => /^[a-zA-ZáàãâéèêíïóôõöúçñÁÀÃÂÉÈÊÍÏÓÔÕÖÚÇÑ\s.'&-]+$/.test(val), {
      message: 'Nome deve conter apenas letras.',
    }),

  documento: z
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

  email: z
    .string()
    .optional()
    .refine(
      (val) => {
        if (!val || val.trim().length === 0) return true;
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(val);
      },
      { message: 'E-mail inválido.' },
    ),

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

  grauVinculo: z.enum(
    ['PAI', 'MAE', 'CONJUGE', 'FILHO', 'SOCIO', 'FUNCIONARIO', 'OUTRO'] as const,
    {
      message: 'Grau de vínculo inválido.',
    },
  ),
});

export type ResponsavelFormData = z.infer<typeof responsavelSchema>;
