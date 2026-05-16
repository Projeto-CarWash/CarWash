/**
 * CarWash Design Tokens
 * Paleta extraída do Figma — AquaGloss Design System
 * Tema dark com accent vermelho (#E61F2D) — RNF010
 */
export const theme = {
  colors: {
    /* Accent / Primary — vermelho CarWash */
    primary: '#E61F2D',
    primaryHover: '#CC1B27',
    primaryGlow: 'rgba(230, 31, 45, 0.15)',
    primaryGlowStrong: 'rgba(230, 31, 45, 0.30)',

    /* Backgrounds — escala de pretos */
    background: '#0A0A0A',
    surface: '#111111',
    surfaceAlt: '#1A1A1A',
    surfaceRaised: '#222222',
    surfaceBorder: '#2A2A2A',

    /* Texto — escala de brancos */
    textPrimary: '#FFFFFF',
    textSecondary: '#A0A0A0',
    textTertiary: '#666666',
    textMuted: '#4A4A4A',

    /* Bordas */
    border: '#2A2A2A',
    borderHover: '#3A3A3A',
    borderFocus: '#E61F2D',

    /* Input — fundo afundado */
    inputBg: '#0D0D0D',
    inputBorder: '#2A2A2A',
    inputBorderHover: '#3A3A3A',
    inputBorderFocus: '#E61F2D',
    inputPlaceholder: '#555555',

    /* Feedback */
    error: '#EF4444',
    errorBg: 'rgba(239, 68, 68, 0.10)',
    errorBorder: 'rgba(239, 68, 68, 0.30)',
    success: '#22C55E',
    successBg: 'rgba(34, 197, 94, 0.10)',
    warning: '#F59E0B',
    warningBg: 'rgba(245, 158, 11, 0.10)',

    /* Overlay */
    overlay: 'rgba(0, 0, 0, 0.6)',
  },
  fonts: {
    heading: "'Space Grotesk', 'Inter', sans-serif",
    body: "'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif",
    mono: "'Geist Mono', 'JetBrains Mono', 'Fira Code', monospace",
  },
  fontSizes: {
    xs: '0.75rem',     /* 12px */
    sm: '0.875rem',    /* 14px */
    base: '1rem',      /* 16px */
    lg: '1.125rem',    /* 18px */
    xl: '1.25rem',     /* 20px */
    '2xl': '1.5rem',   /* 24px */
    '3xl': '2rem',     /* 32px */
    '4xl': '2.5rem',   /* 40px */
  },
  fontWeights: {
    regular: 400,
    medium: 500,
    semibold: 600,
    bold: 700,
  },
  radius: {
    none: '0px',
    sm: '6px',
    md: '8px',
    lg: '12px',
    full: '9999px',
  },
  spacing: {
    xs: '4px',
    sm: '8px',
    md: '16px',
    lg: '24px',
    xl: '32px',
    '2xl': '48px',
    '3xl': '64px',
  },
} as const;
