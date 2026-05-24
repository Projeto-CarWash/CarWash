import { z } from 'zod';

/**
 * Schema do formulário de cadastro de usuário interno (RF014).
 * Espelha as regras do `CriarUsuarioCommandValidator` no backend
 * (mensagens e limites idênticos, NIST SP 800-63B para senha).
 *
 * O backend é a fonte de verdade — este schema entrega feedback rápido
 * mas todas as validações são reconfirmadas server-side.
 */
const EMAIL_REGEX = /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/;
const SENHA_FRACA = 'Senha não atende aos requisitos mínimos.';

export const usuarioSchema = z
  .object({
    nome: z
      .string()
      .min(1, 'Nome é obrigatório.')
      .max(120, 'Nome excede 120 caracteres.')
      .refine((val) => val.trim().length > 0, {
        message: 'Nome não pode conter apenas espaços.',
      }),

    email: z
      .string()
      .min(1, 'E-mail é obrigatório.')
      .max(150, 'E-mail excede 150 caracteres.')
      .regex(EMAIL_REGEX, 'E-mail inválido.'),

    senha: z
      .string()
      .min(8, SENHA_FRACA)
      .max(128, SENHA_FRACA)
      .regex(/[A-Za-z]/, SENHA_FRACA)
      .regex(/\d/, SENHA_FRACA),

    confirmarSenha: z.string().min(1, 'Confirmação de senha é obrigatória.'),

    perfil: z.enum(['Admin', 'Funcionario'], {
      message: 'Perfil inválido.',
    }),
  })
  .superRefine((data, ctx) => {
    if (data.senha && data.confirmarSenha && data.senha !== data.confirmarSenha) {
      ctx.addIssue({
        code: 'custom',
        path: ['confirmarSenha'],
        message: 'As senhas não coincidem.',
      });
    }
  });

export type UsuarioFormData = z.infer<typeof usuarioSchema>;
