export interface User {
  id: string;
  name: string;
  email: string;
  role: 'admin' | 'employee';
}

export interface LoginCredentials {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}
