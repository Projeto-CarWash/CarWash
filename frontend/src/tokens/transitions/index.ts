const values = {
  fast: '150ms ease',
  base: '200ms ease',
  slow: '300ms ease',
  slower: '500ms ease',
  spring: '300ms cubic-bezier(0.34, 1.56, 0.64, 1)',
} as const

const vars = {
  fast: 'var(--transitions--fast)',
  base: 'var(--transitions--base)',
  slow: 'var(--transitions--slow)',
  slower: 'var(--transitions--slower)',
  spring: 'var(--transitions--spring)',
} as const

const toCssVars = () => ({
  '--transitions--fast': values.fast,
  '--transitions--base': values.base,
  '--transitions--slow': values.slow,
  '--transitions--slower': values.slower,
  '--transitions--spring': values.spring,
})

export type Transitions = typeof vars
export { values, toCssVars }
export default vars
