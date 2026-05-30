/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
        mono: ['JetBrains Mono', 'Fira Code', 'monospace'],
      },
      // Todas as cores vêm de frontend/src/tokens/colors/colors.css
      // (:root light + .dark overrides). Componentes shadcn consomem por nome
      // semântico; trocas de paleta acontecem apenas no arquivo CSS dos tokens.
      colors: {
        border: 'hsl(var(--colors--border))',
        input: 'hsl(var(--colors--input))',
        ring: 'hsl(var(--colors--ring))',
        background: 'hsl(var(--colors--background))',
        foreground: 'hsl(var(--colors--foreground))',
        primary: {
          DEFAULT: 'hsl(var(--colors--primary))',
          foreground: 'hsl(var(--colors--primaryForeground))',
        },
        secondary: {
          DEFAULT: 'hsl(var(--colors--secondary))',
          foreground: 'hsl(var(--colors--secondaryForeground))',
        },
        destructive: {
          DEFAULT: 'hsl(var(--colors--destructive))',
          foreground: 'hsl(var(--colors--destructiveForeground))',
        },
        muted: {
          DEFAULT: 'hsl(var(--colors--muted))',
          foreground: 'hsl(var(--colors--mutedForeground))',
        },
        accent: {
          DEFAULT: 'hsl(var(--colors--accent))',
          foreground: 'hsl(var(--colors--accentForeground))',
        },
        card: {
          DEFAULT: 'hsl(var(--colors--card))',
          foreground: 'hsl(var(--colors--cardForeground))',
        },
        popover: {
          DEFAULT: 'hsl(var(--colors--popover))',
          foreground: 'hsl(var(--colors--popoverForeground))',
        },
        success: 'hsl(var(--colors--success))',
      },
      borderRadius: {
        lg: 'var(--radius)',
        md: 'calc(var(--radius) - 2px)',
        sm: 'calc(var(--radius) - 4px)',
      },
    },
  },
  plugins: [],
};
