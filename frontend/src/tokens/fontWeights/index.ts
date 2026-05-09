const values = {
  light: '300',
  normal: '400',
  medium: '500',
  semibold: '600',
  bold: '700',
  extrabold: '800',
} as const

const vars = {
  light: 'var(--fontWeights--light)',
  normal: 'var(--fontWeights--normal)',
  medium: 'var(--fontWeights--medium)',
  semibold: 'var(--fontWeights--semibold)',
  bold: 'var(--fontWeights--bold)',
  extrabold: 'var(--fontWeights--extrabold)',
} as const

const toCssVars = () => ({
  '--fontWeights--light': values.light,
  '--fontWeights--normal': values.normal,
  '--fontWeights--medium': values.medium,
  '--fontWeights--semibold': values.semibold,
  '--fontWeights--bold': values.bold,
  '--fontWeights--extrabold': values.extrabold,
})

export type FontWeights = typeof vars
export { values, toCssVars }
export default vars
