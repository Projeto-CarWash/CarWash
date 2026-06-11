import { useCallback, useEffect, useMemo, useRef, useState } from 'react';

import '@/tokens';

import { preferenciaService } from '@/services/preferenciaService';

import { ThemeContext, type Theme } from './ThemeContext';

import type { ReactNode } from 'react';

interface ThemeProviderProps {
  children: ReactNode;
  /** Tema inicial usado quando não há preferência salva. Default: 'dark'. */
  defaultTheme?: Theme;
}

const STORAGE_KEY = 'carwash_theme';

/**
 * Provider de tema (RF016 — alternância claro/escuro, Must).
 * Aplica `class="dark"` em <html> conforme a preferência atual; persiste em
 * <c>localStorage[carwash_theme]</c>. No primeiro mount, lê a preferência
 * salva — se ausente, cai no <c>defaultTheme</c> e, secundariamente, em
 * <c>prefers-color-scheme</c>.
 *
 * Sincroniza com o backend via `preferenciaService`:
 * - No mount: tenta buscar o tema salvo no backend (GET).
 * - No toggle: tenta salvar a nova preferência no backend (PATCH).
 * - Se qualquer chamada falhar, mantém o tema local e exibe toast (se disponível).
 */
export function ThemeProvider({ children, defaultTheme = 'dark' }: ThemeProviderProps) {
  const [theme, setThemeState] = useState<Theme>(() => carregarTema(defaultTheme));
  const [isSyncing, setIsSyncing] = useState(false);
  const [toastMsg, setToastMsg] = useState<string | null>(null);
  const mountedRef = useRef(true);

  // Aplica a classe no DOM e persiste no localStorage
  useEffect(() => {
    aplicarClasse(theme);
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(STORAGE_KEY, theme);
    }
  }, [theme]);

  // Tenta buscar o tema do backend no mount (sem bloquear a renderização)
  useEffect(() => {
    mountedRef.current = true;
    void preferenciaService.obterTema().then((temaBackend) => {
      if (mountedRef.current && temaBackend && temaBackend !== theme) {
        setThemeState(temaBackend);
      }
    });
    return () => {
      mountedRef.current = false;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Toast auto-dismiss
  useEffect(() => {
    if (!toastMsg) return;
    const timer = setTimeout(() => setToastMsg(null), 4000);
    return () => clearTimeout(timer);
  }, [toastMsg]);

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next);
  }, []);

  const toggle = useCallback(() => {
    setThemeState((prev) => {
      const next = prev === 'dark' ? 'light' : 'dark';

      // Sincroniza com o backend de forma assíncrona (fire-and-forget com tratamento de erro)
      setIsSyncing(true);
      void preferenciaService
        .salvarTema(next)
        .catch(() => {
          // RF016: NÃO desfaz o tema visualmente. Mantém o tema local para a sessão.
          setToastMsg('Não foi possível salvar sua preferência de tema no momento.');
        })
        .finally(() => {
          setIsSyncing(false);
        });

      return next;
    });
  }, []);

  const value = useMemo(
    () => ({ theme, toggle, setTheme, isSyncing }),
    [theme, toggle, setTheme, isSyncing],
  );

  return (
    <ThemeContext.Provider value={value}>
      {children}
      {toastMsg && <ThemeToast message={toastMsg} onDismiss={() => setToastMsg(null)} />}
    </ThemeContext.Provider>
  );
}

function carregarTema(fallback: Theme): Theme {
  if (typeof window === 'undefined') {
    return fallback;
  }
  const salvo = window.localStorage.getItem(STORAGE_KEY);
  if (salvo === 'light' || salvo === 'dark') {
    return salvo;
  }
  // Fallback: respeita preferência do SO se houver.
  if (window.matchMedia('(prefers-color-scheme: light)').matches) {
    return 'light';
  }
  return fallback;
}

function aplicarClasse(theme: Theme): void {
  if (typeof document === 'undefined') {
    return;
  }
  const root = document.documentElement;
  if (theme === 'dark') {
    root.classList.add('dark');
  } else {
    root.classList.remove('dark');
  }
}

/**
 * Toast minimalista para feedback de falha ao salvar tema.
 * Renderizado diretamente pelo ThemeProvider para evitar dependências circulares.
 */
function ThemeToast({ message, onDismiss }: { message: string; onDismiss: () => void }) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed bottom-6 right-6 z-[9999] flex max-w-sm items-center gap-3 rounded-xl border border-amber-500/30 bg-amber-950/90 px-4 py-3 text-sm text-amber-200 shadow-lg backdrop-blur-sm dark:border-amber-500/30 dark:bg-amber-950/90 dark:text-amber-200"
    >
      <span className="flex-1">{message}</span>
      <button
        type="button"
        onClick={onDismiss}
        className="shrink-0 text-amber-400/60 transition-colors hover:text-amber-300"
        aria-label="Fechar notificação"
      >
        ✕
      </button>
    </div>
  );
}
