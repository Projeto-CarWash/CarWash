/**
 * Tipos de autenticação alinhados aos contratos do backend
 * (LoginCommand / LoginResponse — ver testes em
 * backend/tests/CarWash.IntegrationTests/Endpoints/Auth/LoginEndpointTests.cs).
 *
 * O backend serializa em camelCase (default do Minimal API JsonSerializerDefaults.Web)
 * e o enum PerfilUsuario chega como string ("Admin" | "Funcionario").
 */

export type PerfilUsuario = 'Admin' | 'Funcionario';

export interface UsuarioLogado {
  id: string;
  nome: string;
  email: string;
  perfil: PerfilUsuario;
}

export interface LoginCommand {
  email: string;
  senha: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  usuario: UsuarioLogado;
}

/**
 * Resposta do POST /api/v1/auth/refresh — mesma forma do LoginResponse
 * (refresh token vai pelo Set-Cookie httpOnly, não pelo body).
 */
export type RefreshResponse = LoginResponse;

/**
 * Formato de erro (RFC 7807 ProblemDetails) devolvido pelo backend
 * — ver CarWash.Api/Middleware/ExceptionHandlingMiddleware.cs.
 */
export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  correlationId?: string;
  errors?: Record<string, string[]>;
}
