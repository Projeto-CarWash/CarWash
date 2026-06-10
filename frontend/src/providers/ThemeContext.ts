import { createContext } from 'react';

export type Theme = 'light' | 'dark';

export interface ThemeContextData {
  theme: Theme;
  toggle: () => void;
  setTheme: (theme: Theme) => void;
  /** Indica que o tema está sendo sincronizado com o backend. */
  isSyncing: boolean;
}

/**
 * Context puro do tema — exportado separado do provider para preservar
 * fast-refresh (regra react-refresh/only-export-components).
 */
export const ThemeContext = createContext<ThemeContextData | undefined>(undefined);
