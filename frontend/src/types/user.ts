/**
 * Tipos de usuário interno alinhados ao contrato do backend
 * (CriarUsuarioCommand / UsuarioResponse — ver
 * backend/src/CarWash.Application/Usuarios/CriarUsuario/CriarUsuarioCommand.cs
 * e backend/src/CarWash.Application/Usuarios/Common/UsuarioResponse.cs).
 *
 * O enum PerfilUsuario é reaproveitado de `types/auth.ts` (mesma serialização
 * camelCase string `"Admin" | "Funcionario"`).
 */
import type { PerfilUsuario } from './auth';

export interface CriarUsuarioCommand {
  nome: string;
  email: string;
  senha: string;
  perfil: PerfilUsuario;
}

export interface UsuarioResponse {
  id: string;
  nome: string;
  email: string;
  perfil: PerfilUsuario;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm: string;
}
