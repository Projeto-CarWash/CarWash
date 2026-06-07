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

/**
 * Mensagens de validação do campo "células ativas" (RF018).
 *
 * <p>Centralizadas para reúso entre schema, formulários e testes, garantindo o
 * texto exato exigido pela especificação do campo. Cada caso é distinto:
 * obrigatório, tipo inválido (texto/decimal) e faixa inválida (1–100).</p>
 */
export const MENSAGENS_CELULAS_ATIVAS = {
  obrigatorio: 'Células ativas é obrigatório.',
  tipo: 'Células ativas deve ser um número inteiro.',
  faixa: 'Células ativas deve estar entre 1 e 100.',
  generico: 'Informe um valor válido de células ativas.',
} as const;

/**
 * Campo "células ativas": inteiro obrigatório entre 1 e 100 (RF018).
 *
 * <p>O `preprocess` normaliza a entrada do input (string) para número, tratando
 * vazio/ausente como obrigatório. Mantém apenas dígitos não é responsabilidade
 * do schema (o input cuida disso); aqui as mensagens distinguem cada caso para
 * o usuário. Saída sempre é `number` inteiro — o payload nunca envia string.</p>
 */
export const celulasAtivasSchema = z.preprocess(
  (value) => {
    if (typeof value === 'number') return value;
    if (typeof value === 'string') {
      const limpo = value.trim();
      if (limpo === '') return undefined;
      const numero = Number(limpo);
      return Number.isNaN(numero) ? limpo : numero;
    }
    // `null` é tratado como ausência → dispara a mensagem de obrigatório.
    return value ?? undefined;
  },
  z
    .number({
      error: (issue) =>
        issue.input === undefined
          ? MENSAGENS_CELULAS_ATIVAS.obrigatorio
          : MENSAGENS_CELULAS_ATIVAS.tipo,
    })
    .int(MENSAGENS_CELULAS_ATIVAS.tipo)
    .min(1, MENSAGENS_CELULAS_ATIVAS.faixa)
    .max(100, MENSAGENS_CELULAS_ATIVAS.faixa),
);

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

  celulasAtivas: celulasAtivasSchema,

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
