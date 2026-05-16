/**
 * Valida formato de e-mail
 * @returns mensagem de erro ou string vazia se válido
 */
export function validateEmail(email: string): string {
  if (!email.trim()) {
    return 'O e-mail é obrigatório';
  }

  const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
  if (!emailRegex.test(email)) {
    return 'Insira um e-mail válido';
  }

  return '';
}

/**
 * Valida senha
 * @returns mensagem de erro ou string vazia se válido
 */
export function validatePassword(password: string): string {
  if (!password) {
    return 'A senha é obrigatória';
  }

  if (password.length < 6) {
    return 'A senha deve ter pelo menos 6 caracteres';
  }

  return '';
}
