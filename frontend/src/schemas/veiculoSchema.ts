import { z } from 'zod';

export const veiculoSchema = z.object({
  clienteId: z
    .string()
    .min(1, 'Selecione um cliente para vincular o veículo.')
    .uuid('Selecione um cliente para vincular o veículo.'),

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
    .transform((val) => val.trim())
    .refine((val) => val.length >= 2 && val.length <= 80, {
      message: 'Modelo deve ter entre 2 e 80 caracteres.',
    }),

  fabricante: z
    .string()
    .min(1, 'Fabricante é obrigatório.')
    .transform((val) => val.trim())
    .refine((val) => val.length >= 2 && val.length <= 80, {
      message: 'Fabricante deve ter entre 2 e 80 caracteres.',
    }),

  cor: z
    .string()
    .min(1, 'Cor é obrigatória.')
    .transform((val) => val.trim())
    .refine((val) => val.length >= 2 && val.length <= 40, {
      message: 'Cor deve ter entre 2 e 40 caracteres.',
    }),

  observacoes: z.string().max(500, 'Observações deve ter no máximo 500 caracteres.').optional(),
});

export type VeiculoFormData = z.infer<typeof veiculoSchema>;
