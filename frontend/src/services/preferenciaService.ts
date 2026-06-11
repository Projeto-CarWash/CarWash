import api from './api';

import type { Theme } from '@/providers/ThemeContext';

/** Envelope do backend: `{ message, data: { theme }, traceId }` (RF016). */
interface PreferenciasResponse {
  data: { theme: string };
}

/**
 * Service de preferências do usuário (RF016 — alternância de tema).
 *
 * <p>Comunica com o backend para persistir o tema escolhido:
 * `GET /api/v1/usuarios/me/preferencias` e
 * `PATCH /api/v1/usuarios/me/preferencias/tema` (body `{ theme }`).
 * Se a API falhar, o tema é mantido apenas no `localStorage`.</p>
 */
export const preferenciaService = {
  /**
   * Busca a preferência de tema salva no backend.
   * Retorna `null` se a API falhar (o `ThemeProvider` usa `localStorage` como fallback).
   */
  async obterTema(): Promise<Theme | null> {
    try {
      const { data } = await api.get<PreferenciasResponse>('/api/v1/usuarios/me/preferencias', {
        _skipAuthRefresh: true,
      });
      const theme = data.data?.theme;
      if (theme === 'light' || theme === 'dark') {
        return theme;
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
    await api.patch('/api/v1/usuarios/me/preferencias/tema', { theme: tema });
  },
};
