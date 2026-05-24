import { z } from 'zod';

export const servicoSchema = z.object({
  nome: z
    .string()
    .trim()
    .min(3, 'Nome do serviço deve ter entre 3 e 120 caracteres.')
    .max(120, 'Nome do serviço deve ter entre 3 e 120 caracteres.')
    .refine((val) => val.trim().length > 0, 'Nome do serviço é obrigatório.'),
  preco: z.preprocess(
    (val) => {
      if (typeof val === 'string') {
        const parsed = val.replace(',', '.');
        return parseFloat(parsed);
      }
      return val;
    },
    z.number()
      .positive('Preço do serviço deve ser maior que zero.')
      .refine(val => !isNaN(val), { message: 'Preço do serviço é obrigatório e deve ser numérico.' })
  ),
  duracaoMin: z.preprocess(
    (val) => (typeof val === 'string' && val !== '' ? Number(val) : val),
    z.number()
      .int('Duração do serviço é obrigatória e deve ser um número inteiro.')
      .positive('Duração do serviço deve ser maior que zero.')
      .max(1440, 'Duração do serviço não pode ultrapassar 1440 minutos.')
      .refine(val => !isNaN(val), { message: 'Duração do serviço é obrigatória e deve ser um número inteiro.' })
  ),
});

export type ServicoFormData = z.infer<typeof servicoSchema>;
