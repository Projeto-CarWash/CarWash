const values = {
  none: '1',
  tight: '1.25',
  snug: '1.375',
  normal: '1.5',
  relaxed: '1.625',
  loose: '2',
} as const

const vars = {
  none: 'var(--lineHeights--none)',
  tight: 'var(--lineHeights--tight)',
  snug: 'var(--lineHeights--snug)',
  normal: 'var(--lineHeights--normal)',
  relaxed: 'var(--lineHeights--relaxed)',
  loose: 'var(--lineHeights--loose)',
} as const

const toCssVars = () => ({
  '--lineHeights--none': values.none,
  '--lineHeights--tight': values.tight,
  '--lineHeights--snug': values.snug,
  '--lineHeights--normal': values.normal,
  '--lineHeights--relaxed': values.relaxed,
  '--lineHeights--loose': values.loose,
})

export type LineHeights = typeof vars
export { values, toCssVars }
export default vars
