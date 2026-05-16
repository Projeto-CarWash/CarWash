const values = {
  none: '0px',
  sXS: '1px',
  sS: '1.5px',
  sM: '2px',
  sL: '3px',
} as const;

const vars = {
  none: 'var(--borderWidths--none)',
  sXS: 'var(--borderWidths--sXS)',
  sS: 'var(--borderWidths--sS)',
  sM: 'var(--borderWidths--sM)',
  sL: 'var(--borderWidths--sL)',
} as const;

const toCssVars = () => ({
  '--borderWidths--none': values.none,
  '--borderWidths--sXS': values.sXS,
  '--borderWidths--sS': values.sS,
  '--borderWidths--sM': values.sM,
  '--borderWidths--sL': values.sL,
});

export type BorderWidths = typeof vars;
export { values, toCssVars };
export default vars;
