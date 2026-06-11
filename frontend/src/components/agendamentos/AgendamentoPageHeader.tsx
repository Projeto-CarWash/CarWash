import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

interface AgendamentoPageHeaderProps {
  step?: number;
  onCancel?: () => void;
}

export function AgendamentoPageHeader({ step = 1, onCancel }: AgendamentoPageHeaderProps) {
  return (
    <div className="flex items-center justify-between px-8 py-6">
      <div>
        <div className="flex items-center gap-3">
          <h1 className="text-3xl font-bold tracking-tight text-foreground">Novo agendamento</h1>
          <Badge className="rounded-full border border-border bg-muted px-3 py-1 text-[10px] font-semibold tracking-[0.2em] text-muted-foreground">
            PASSO {step} DE 3
          </Badge>
        </div>
      </div>
      <div className="flex items-center gap-3">
        <Button
          type="button"
          variant="outline"
          onClick={onCancel}
          className="h-9 rounded-full border-border bg-transparent text-sm text-muted-foreground hover:bg-accent hover:text-foreground"
        >
          Cancelar
        </Button>
      </div>
    </div>
  );
}
