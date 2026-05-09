import type { CSSProperties, ReactNode } from 'react'
import { allCssVars } from '@/tokens'

type ThemeProviderProps = {
  children: ReactNode
}

export const ThemeProvider = ({ children }: ThemeProviderProps) => (
  <div style={allCssVars() as CSSProperties}>
    {children}
  </div>
)
