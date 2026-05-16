import type { User, CreateUserData, UserStatus } from '../types/user';

const userService = {
  /**
   * Cria um novo usuário interno (POST /api/v1/usuarios)
   */
  create: async (userData: CreateUserData): Promise<User> => {
    // Simulando delay da API
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Mock de sucesso
    const newUser: User = {
      id: Math.random().toString(36).substr(2, 9),
      name: userData.name,
      email: userData.email.toLowerCase(),
      role: userData.role,
      status: userData.status,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    return newUser;
  },

  /**
   * Altera o status de um usuário (PATCH /api/v1/usuarios/{id}/status)
   */
  updateStatus: async (_id: string, _status: UserStatus): Promise<void> => {
    await new Promise(resolve => setTimeout(resolve, 1000));
  },

  /**
   * Busca um usuário pelo ID (GET /api/v1/usuarios/{id})
   */
  getById: async (id: string): Promise<User> => {
    // Mock
    await Promise.resolve();
    return {
      id,
      name: 'Usuário Exemplo',
      email: 'exemplo@carwash.com',
      role: 'ADMIN',
      status: 'ACTIVE',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };
  }
};

export default userService;
