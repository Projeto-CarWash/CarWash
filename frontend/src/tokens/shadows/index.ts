export interface ShadowsType {
  none: string;
  sm: string;
  base: string;
  md: string;
  lg: string;
  glow: string;
  glowStrong: string;
}

export const Shadows: ShadowsType = {
  none: 'none',
  sm: '0 1px 2px rgba(0,0,0,0.4)',
  base: '0 2px 4px rgba(0,0,0,0.5)',
  md: '0 4px 12px rgba(0,0,0,0.6)',
  lg: '0 8px 24px rgba(0,0,0,0.7)',
  glow: '0 0 16px rgba(255, 31, 46, 0.4)',
  glowStrong: '0 0 32px rgba(255, 31, 46, 0.7)',
};
