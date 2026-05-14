import type { ReactNode } from 'react';
import '../tokens/index.ts';

interface ThemeProviderProps {
  children: ReactNode;
}

export const ThemeProvider = ({ children }: ThemeProviderProps) => <div>{children}</div>;
