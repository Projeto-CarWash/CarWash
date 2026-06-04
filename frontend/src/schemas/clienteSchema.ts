import { z } from 'zod';

import { isValidCnpj, isValidCpf } from '@/lib/validators';

const UF_PATTERN =
  /^(?:AC|AL|AP|AM|BA|CE|DF|ES|GO|MA|MT|MS|MG|PA|PB|PR|PE|PI|RJ|RN|RS|RO|RR|SC|SP|SE|TO)$/;

const CIDADE_PATTERN = /^[a-zA-ZáàãâäéèêëíïóôõöúüçñÁÀÃÂÄÉÈÊËÍÏÓÔÕÖÚÜÇÑ\s-]+$/;

/**
 * Schema alinhado com backend CarWash.Application.DTOs.Clientes.CreateClienteRequest.
 * Endereço estruturado, celular obrigatório (RF003), data de nascimento com
 * idade 18..110, email opcional.
 *
 * Veículos seguem o contrato oficial da API:
 *   { placa: string, fabricante: string, modelo: string, cor: string, ano: number }
 * - `cor` persiste o valor hexadecimal diretamente (ex: "#2563EB").
 * - `ano` é enviado como Number.
 * - Preferências de agendamento (lembretes, canais de contato)
 * - Filiados vinculados ao cliente
 */

export const veiculoItemSchema = z.object({
  placa: z
    .string()
    .min(1, 'Placa é obrigatória.')
    .transform((val) => val.trim().replace(/[\s-]/g, '').toUpperCase())
    .refine((val) => /^[A-Z]{3}[0-9][A-Z0-9][0-9]{2}$/.test(val), {
      message: 'Formato de placa inválido (ex: ABC-1234 ou ABC1D23).',
    }),
  fabricante: z
    .string()
    .min(1, 'Fabricante é obrigatório.')
    .transform((val) => val.trim())
    .refine((val) => val.length >= 2 && val.length <= 80, {
      message: 'Fabricante deve ter entre 2 e 80 caracteres.',
    }),
  modelo: z
    .string()
    .min(1, 'Modelo é obrigatório.')
    .transform((val) => val.trim())
    .refine((val) => val.length >= 2 && val.length <= 80, {
      message: 'Modelo deve ter entre 2 e 80 caracteres.',
    }),
  cor: z
    .string()
    .min(1, 'Cor é obrigatória.'),
  ano: z
    .union([z.string(), z.number()])
    .optional()
    .transform((val) => {
      if (val === '' || val === undefined || val === null) return undefined;
      return Number(val);
    })
    .refine((val) => val === undefined || (!isNaN(val) && Number.isInteger(val)), {
      message: 'Ano deve ser um número inteiro.',
    })
    .refine((val) => val === undefined || (val >= 1930 && val <= 2027), {
      message: 'O ano deve ser entre 1930 e 2027.',
    }),
});

export const filiadoSchema = z.object({
  cpf: z
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
  nome: z
    .string()
    .min(1, 'Nome é obrigatório.')
    .refine((val) => val.trim().length >= 3, {
      message: 'Nome deve ter no mínimo 3 caracteres.',
    })
    .refine((val) => /^[a-zA-ZáàãâéèêíïóôõöúçñÁÀÃÂÉÈÊÍÏÓÔÕÖÚÇÑ\s.'&-]+$/.test(val), {
      message: 'Nome deve conter apenas letras.',
    }),
  telefone: z.string().optional(),
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
});

const LEMBRETES_VALUES = ['24H', '12H', '6H', '1H', 'NENHUM'] as const;
const CANAIS_VALUES = ['WHATSAPP', 'EMAIL', 'SMS', 'LIGACAO'] as const;

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
    })
    .refine((val) => /^[a-zA-ZáàãâéèêíïóôõöúçñÁÀÃÂÉÈÊÍÏÓÔÕÖÚÇÑ\s.'&-]+$/.test(val), {
      message: 'Nome deve conter apenas letras.',
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
      { message: 'Telefone deve conter 10 or 11 dígitos.' },
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
      { message: 'E-mail Inválido' },
    ),

  cep: z
    .string()
    .min(1, 'CEP é obrigatório.')
    .refine((val) => val.replace(/\D/g, '').length === 8, {
      message: 'CEP deve conter 8 dígitos.',
    }),

  logradouro: z
    .string()
    .trim()
    .min(3, 'Logradouro deve ter no mínimo 3 caracteres.')
    .max(150, 'Logradouro deve ter no máximo 150 caracteres.'),

  // Número do endereço aceita valores alfanuméricos (ex.: 123, 123A, 12-F,
  // 100 Fundos, 25 Casa 2, A-15). Apenas espaços em branco são inválidos.
  numero: z
    .string()
    .trim()
    .min(1, 'Número é obrigatório.')
    .max(20, 'Número deve ter no máximo 20 caracteres.')
    .refine((val) => /^[\p{L}\p{N}][\p{L}\p{N}\s/.,-]*$/u.test(val), {
      message: 'Número inválido. Use letras, números e separadores (ex: 123A, 12-F, 100 Fundos).',
    }),

  complemento: z
    .string()
    .trim()
    .max(100, 'Complemento deve ter no máximo 100 caracteres.')
    .optional(),

  bairro: z
    .string()
    .trim()
    .min(2, 'Bairro deve ter no mínimo 2 caracteres.')
    .max(100, 'Bairro deve ter no máximo 100 caracteres.'),

  cidade: z
    .string()
    .trim()
    .min(3, 'Cidade deve ter no mínimo 3 caracteres.')
    .max(100, 'Cidade deve ter no máximo 100 caracteres.')
    .refine((val) => CIDADE_PATTERN.test(val), {
      message: 'Cidade não deve conter números ou caracteres especiais.',
    }),

  uf: z
    .string()
    .min(1, 'UF é obrigatória.')
    .transform((v) => v.toUpperCase())
    .refine((v) => UF_PATTERN.test(v), { message: 'UF inválida (use sigla dos 27 estados).' }),

  veiculos: z
    .array(veiculoItemSchema)
    .min(1, 'Adicione ao menos um veículo para concluir o cadastro.'),

  // ── Preferências & Fidelidade ──────────────────────────────────────────────
  lembretes: z.array(z.enum(LEMBRETES_VALUES)),
  canaisPreferenciais: z.array(z.enum(CANAIS_VALUES)),
  observacoesGerais: z.string().max(1000, 'Máximo 1000 caracteres.').optional(),
  filiados: z.array(filiadoSchema),
});

/**
 * Schema de EDIÇÃO de cliente (PUT /api/v1/clientes/{id}).
 *
 * Reaproveita as mesmas regras de identificação, contato e endereço do cadastro,
 * mas remove os campos que o endpoint de atualização não aceita:
 * - cpfCnpj: imutável por decisão de produto (backend ignora e apenas loga warning);
 * - veiculos: possuem fluxo próprio na tela de detalhe do cliente;
 * - preferências/filiados: não fazem parte do contrato do PUT.
 */
export const editarClienteSchema = clienteSchema.omit({
  cpfCnpj: true,
  veiculos: true,
  lembretes: true,
  canaisPreferenciais: true,
  observacoesGerais: true,
  filiados: true,
});

export type VeiculoLocalFormData = z.infer<typeof veiculoItemSchema>;
export type FiliadoFormData = z.infer<typeof filiadoSchema>;
export type ClienteFormData = z.infer<typeof clienteSchema>;
export type EditarClienteFormData = z.infer<typeof editarClienteSchema>;
export type LembreteValue = (typeof LEMBRETES_VALUES)[number];
export type CanalValue = (typeof CANAIS_VALUES)[number];
