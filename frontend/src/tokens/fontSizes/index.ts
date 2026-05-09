const values = {
  xs: '0.75rem',
  sm: '0.875rem',
  base: '1rem',
  lg: '1.125rem',
  xl: '1.25rem',
  '2xl': '1.5rem',
  '3xl': '1.875rem',
  '4xl': '2.25rem',
  '5xl': '3rem',
} as const

const vars = {
  xs: 'var(--fontSizes--xs)',
  sm: 'var(--fontSizes--sm)',
  base: 'var(--fontSizes--base)',
  lg: 'var(--fontSizes--lg)',
  xl: 'var(--fontSizes--xl)',
  '2xl': 'var(--fontSizes--2xl)',
  '3xl': 'var(--fontSizes--3xl)',
  '4xl': 'var(--fontSizes--4xl)',
  '5xl': 'var(--fontSizes--5xl)',
} as const

const toCssVars = () => ({
  '--fontSizes--xs': values.xs,
  '--fontSizes--sm': values.sm,
  '--fontSizes--base': values.base,
  '--fontSizes--lg': values.lg,
  '--fontSizes--xl': values.xl,
  '--fontSizes--2xl': values['2xl'],
  '--fontSizes--3xl': values['3xl'],
  '--fontSizes--4xl': values['4xl'],
  '--fontSizes--5xl': values['5xl'],
})

export type FontSizes = typeof vars
export { values, toCssVars }
export default vars
