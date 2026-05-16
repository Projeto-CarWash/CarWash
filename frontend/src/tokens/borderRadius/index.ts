const values = {
  none: '0px',
  small: '4px',
  medium: '6px',
  large: '8px',
  xLarge: '12px',
  xxLarge: '16px',
  xxxLarge: '24px',
  full: '9999px',
} as const;

const vars = {
  none: 'var(--borderRadius--none)',
  small: 'var(--borderRadius--small)',
  medium: 'var(--borderRadius--medium)',
  large: 'var(--borderRadius--large)',
  xLarge: 'var(--borderRadius--xLarge)',
  xxLarge: 'var(--borderRadius--xxLarge)',
  xxxLarge: 'var(--borderRadius--xxxLarge)',
  full: 'var(--borderRadius--full)',
} as const;

const toCssVars = () => ({
  '--borderRadius--none': values.none,
  '--borderRadius--small': values.small,
  '--borderRadius--medium': values.medium,
  '--borderRadius--large': values.large,
  '--borderRadius--xLarge': values.xLarge,
  '--borderRadius--xxLarge': values.xxLarge,
  '--borderRadius--xxxLarge': values.xxxLarge,
  '--borderRadius--full': values.full,
});

export type BorderRadius = typeof vars;
export { values, toCssVars };
export default vars;
