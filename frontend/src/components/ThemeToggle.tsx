import { Moon, Sun } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { useTheme } from '@/hooks/useTheme';

interface ThemeToggleProps {
  /** Aplica classes adicionais ao botão (ex.: ajuste de tamanho/posição). */
  className?: string;
}

/**
 * Alternador de tema (RF016 + RNF010). Coloca um botão circular com ícone
 * que troca de Lua/Sol conforme o tema atual.
 */
export function ThemeToggle({ className }: ThemeToggleProps) {
  const { theme, toggle } = useTheme();
  const escuro = theme === 'dark';

  return (
    <Button
      type="button"
      variant="ghost"
      size="icon"
      onClick={toggle}
      aria-label={escuro ? 'Mudar para tema claro' : 'Mudar para tema escuro'}
      className={className}
    >
      {escuro ? (
        <Sun className="h-4 w-4" aria-hidden="true" />
      ) : (
        <Moon className="h-4 w-4" aria-hidden="true" />
      )}
    </Button>
  );
}
