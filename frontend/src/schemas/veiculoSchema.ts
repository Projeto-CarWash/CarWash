import { z } from 'zod';

export const veiculoSchema = z.object({
  placa: z
    .string()
    .min(1, 'Placa é obrigatória.')
    .transform((val) =>
      val
        .trim()
        .replace(/\s+/g, '') // remove all internal spaces
        .replace(/-/g, '') // remove hyphens
        .toUpperCase(),
    )
    .refine((val) => val.length === 7, {
      message: 'Placa deve conter 7 caracteres válidos.',
    })
    .refine((val) => /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(val), {
      message: 'Placa inválida. Formatos aceitos: AAA0000 ou AAA0A00.',
    }),

  modelo: z
    .string()
    .min(1, 'Modelo é obrigatório.')
    .refine((val) => val.trim().length <= 80, {
      message: 'Modelo deve ter no máximo 80 caracteres.',
    }),

  fabricante: z
    .string()
    .min(1, 'Fabricante é obrigatório.')
    .refine((val) => val.trim().length <= 80, {
      message: 'Fabricante deve ter no máximo 80 caracteres.',
    }),

  cor: z
    .string()
    .min(1, 'Cor é obrigatória.')
    .refine((val) => val.trim().length <= 40, {
      message: 'Cor deve ter no máximo 40 caracteres.',
    }),

  ano: z
    .string()
    .optional()
    .refine(
      (val) => {
        if (!val || val.trim() === '') return true;
        const num = Number(val);
        return !isNaN(num) && num >= 1900 && num <= 2100;
      },
      { message: 'Ano deve estar entre 1900 e 2100.' },
    ),
});

export type VeiculoFormData = z.infer<typeof veiculoSchema>;
