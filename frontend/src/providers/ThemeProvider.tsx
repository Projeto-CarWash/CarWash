import { useCallback, useEffect, useMemo, useState } from 'react';

import '@/tokens';

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
 */
export function ThemeProvider({ children, defaultTheme = 'dark' }: ThemeProviderProps) {
  const [theme, setThemeState] = useState<Theme>(() => carregarTema(defaultTheme));

  useEffect(() => {
    aplicarClasse(theme);
    if (typeof window !== 'undefined') {
      window.localStorage.setItem(STORAGE_KEY, theme);
    }
  }, [theme]);

  const setTheme = useCallback((next: Theme) => {
    setThemeState(next);
  }, []);

  const toggle = useCallback(() => {
    setThemeState((prev) => (prev === 'dark' ? 'light' : 'dark'));
  }, []);

  const value = useMemo(() => ({ theme, toggle, setTheme }), [theme, toggle, setTheme]);

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
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
