import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

export function PageHeader() {
  return (
    <div className="flex items-center justify-between px-8 py-6">
      <div>
        <div className="flex items-center gap-3">
          <h1 className="text-3xl font-bold tracking-tight text-zinc-50">Novo cliente</h1>
          <Badge className="rounded-full border border-zinc-700/60 bg-zinc-800/60 px-3 py-1 text-[10px] font-semibold tracking-[0.2em] text-zinc-400">
            PASSO 1 DE 3
          </Badge>
        </div>
      </div>
      <div className="flex items-center gap-3">
        <Button
          variant="outline"
          className="h-9 rounded-full border-zinc-700/60 bg-transparent text-sm text-zinc-400 hover:bg-zinc-800/50 hover:text-zinc-200"
        >
          Cancelar
        </Button>
        <Button className="h-9 rounded-full bg-red-600 px-5 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700">
          Salvar Rascunho
        </Button>
      </div>
    </div>
  );
}
