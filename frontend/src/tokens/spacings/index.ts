const values = {
  sQuark: '4px',
  sQuarkL: '8px',
  sNano: '8px',
  sXXS: '12px',
  sXS: '16px',
  sSM: '20px',
  sS: '24px',
  sMD: '32px',
  sM: '40px',
  sLG: '48px',
  sL: '64px',
  sXL: '80px',
  sXXL: '96px',
} as const

const vars = {
  sQuark: 'var(--spacings--sQuark)',
  sQuarkL: 'var(--spacings--sQuarkL)',
  sNano: 'var(--spacings--sNano)',
  sXXS: 'var(--spacings--sXXS)',
  sXS: 'var(--spacings--sXS)',
  sSM: 'var(--spacings--sSM)',
  sS: 'var(--spacings--sS)',
  sMD: 'var(--spacings--sMD)',
  sM: 'var(--spacings--sM)',
  sLG: 'var(--spacings--sLG)',
  sL: 'var(--spacings--sL)',
  sXL: 'var(--spacings--sXL)',
  sXXL: 'var(--spacings--sXXL)',
} as const

const toCssVars = () => ({
  '--spacings--sQuark': values.sQuark,
  '--spacings--sQuarkL': values.sQuarkL,
  '--spacings--sNano': values.sNano,
  '--spacings--sXXS': values.sXXS,
  '--spacings--sXS': values.sXS,
  '--spacings--sSM': values.sSM,
  '--spacings--sS': values.sS,
  '--spacings--sMD': values.sMD,
  '--spacings--sM': values.sM,
  '--spacings--sLG': values.sLG,
  '--spacings--sL': values.sL,
  '--spacings--sXL': values.sXL,
  '--spacings--sXXL': values.sXXL,
})

export type Spacings = typeof vars
export { values, toCssVars }
export default vars
