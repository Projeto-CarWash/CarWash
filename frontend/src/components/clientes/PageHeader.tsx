import { ArrowLeft } from 'lucide-react';
import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

interface PageHeaderProps {
  onClearForm?: () => void;
  step?: number;
}

export function PageHeader({ onClearForm, step = 1 }: PageHeaderProps) {
  const navigate = useNavigate();

  const handleGoBack = useCallback(() => {
    void navigate('/clientes');
  }, [navigate]);

  return (
    <div className="flex items-center justify-between px-8 py-6">
      <div className="flex items-center gap-4">
        <Button
          type="button"
          variant="ghost"
          onClick={handleGoBack}
          className="h-9 w-9 rounded-full p-0 text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
          aria-label="Voltar para lista de clientes"
        >
          <ArrowLeft className="h-5 w-5" />
        </Button>
        <div>
          <div className="flex items-center gap-3">
            <h1 className="text-3xl font-bold tracking-tight text-zinc-50">Novo cliente</h1>
            <Badge className="rounded-full border border-zinc-700/60 bg-zinc-800/60 px-3 py-1 text-[10px] font-semibold tracking-[0.2em] text-zinc-400">
              PASSO {step} DE 4
            </Badge>
          </div>
        </div>
      </div>
      <div className="flex items-center gap-3">
        <Button
          type="button"
          variant="outline"
          onClick={onClearForm}
          className="h-9 rounded-full border-zinc-700/60 bg-transparent text-sm text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
        >
          Limpar Formulário
        </Button>
      </div>
    </div>
  );
}
