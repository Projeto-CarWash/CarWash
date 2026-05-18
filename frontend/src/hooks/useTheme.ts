import { useContext } from 'react';

import { ThemeContext } from '@/providers/ThemeContext';

/**
 * Acessa o contexto do tema (RF016). Use somente dentro de &lt;ThemeProvider&gt;.
 */
export function useTheme() {
  const context = useContext(ThemeContext);
  if (context === undefined) {
    throw new Error('useTheme deve ser usado dentro de um ThemeProvider.');
  }
  return context;
}
