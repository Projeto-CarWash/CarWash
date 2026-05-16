export type UserRole = 'ADMIN' | 'OPERATOR' | 'MANAGER';
export type UserStatus = 'ACTIVE' | 'INACTIVE';

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  status: UserStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateUserData {
  name: string;
  email: string;
  role: UserRole;
  password?: string;
  confirmPassword?: string;
  status: UserStatus;
}

export interface UserResponse {
  user: User;
  message: string;
}
