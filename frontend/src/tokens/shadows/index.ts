const values = {
  none: 'none',
  sm: '0 1px 2px rgba(0,0,0,0.4)',
  base: '0 2px 4px rgba(0,0,0,0.5)',
  md: '0 4px 12px rgba(0,0,0,0.6)',
  lg: '0 8px 24px rgba(0,0,0,0.7)',
  glow: '0 0 16px rgba(255, 31, 46, 0.4)',
  glowStrong: '0 0 32px rgba(255, 31, 46, 0.7)',
} as const

const vars = {
  none: 'var(--shadows--none)',
  sm: 'var(--shadows--sm)',
  base: 'var(--shadows--base)',
  md: 'var(--shadows--md)',
  lg: 'var(--shadows--lg)',
  glow: 'var(--shadows--glow)',
  glowStrong: 'var(--shadows--glowStrong)',
} as const

const toCssVars = () => ({
  '--shadows--none': values.none,
  '--shadows--sm': values.sm,
  '--shadows--base': values.base,
  '--shadows--md': values.md,
  '--shadows--lg': values.lg,
  '--shadows--glow': values.glow,
  '--shadows--glowStrong': values.glowStrong,
})

export type Shadows = typeof vars
export { values, toCssVars }
export default vars
