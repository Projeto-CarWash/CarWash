import api from './api';

import type { Theme } from '@/providers/ThemeContext';

interface PreferenciasResponse {
  tema: Theme;
}

/**
 * Service de preferências do usuário (RF016 — alternância de tema).
 *
 * <p>Comunica com `GET/PATCH /api/v1/usuarios/me/preferencias` para persistir
 * o tema escolhido no backend. Se a API não estiver pronta, falha
 * silenciosamente e o tema é mantido apenas no `localStorage`.</p>
 */
export const preferenciaService = {
  /**
   * Busca a preferência de tema salva no backend.
   * Retorna `null` se a API falhar (o `ThemeProvider` usa `localStorage` como fallback).
   */
  async obterTema(): Promise<Theme | null> {
    try {
      const { data } = await api.get<PreferenciasResponse>('/api/v1/usuarios/me/preferencias');
      if (data.tema === 'light' || data.tema === 'dark') {
        return data.tema;
      }
      return null;
    } catch {
      return null;
    }
  },

  /**
   * Persiste a preferência de tema no backend.
   * Lança o erro original para que o caller possa exibir o toast amigável.
   */
  async salvarTema(tema: Theme): Promise<void> {
    await api.patch('/api/v1/usuarios/me/preferencias', { tema });
  },
};
