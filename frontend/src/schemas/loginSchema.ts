import { z } from 'zod';

/**
 * Schema do formulário de login (RF001).
 * Validação intencionalmente frouxa: o backend é a fonte de verdade
 * (LoginValidator + LoginHandler unificam erro 401 para evitar enumeração
 * de usuários — ver backend/src/CarWash.Application/Auth/Login/*).
 */
export const loginSchema = z.object({
  email: z
    .string()
    .min(1, 'E-mail é obrigatório.')
    .max(150, 'E-mail deve ter no máximo 150 caracteres.')
    .email('Informe um e-mail válido.'),
  senha: z
    .string()
    .min(1, 'Senha é obrigatória.')
    .max(256, 'Senha deve ter no máximo 256 caracteres.'),
});

export type LoginFormData = z.infer<typeof loginSchema>;
