import { z } from 'zod';

const SERVICO_NOME_PATTERN = /^[a-zA-ZáàãâäéèêëíïóôõöúüçñÁÀÃÂÄÉÈÊËÍÏÓÔÕÖÚÜÇÑ0-9\s\-]+$/;

export const servicoSchema = z.object({
  nome: z
    .string()
    .min(1, 'Nome do serviço é obrigatório.')
    .max(120, 'Nome do serviço deve ter no máximo 120 caracteres.')
    .refine((val) => val.trim().length > 0, {
      message: 'Nome do serviço não pode conter apenas espaços.',
    })
    .refine((val) => SERVICO_NOME_PATTERN.test(val), {
      message: 'Nome do serviço contém caracteres especiais inválidos.',
    }),

  preco: z
    .string()
    .min(1, 'Preço do serviço é obrigatório.')
    .refine((val) => !isNaN(Number(val)) && Number(val) > 0, {
      message: 'Preço do serviço deve ser um número maior que zero.',
    }),

  duracaoMin: z
    .string()
    .min(1, 'Duração do serviço é obrigatória.')
    .refine((val) => Number.isInteger(Number(val)) && Number(val) > 0, {
      message: 'Duração do serviço deve ser um número inteiro maior que zero.',
    }),
});

export type ServicoFormData = z.infer<typeof servicoSchema>;
