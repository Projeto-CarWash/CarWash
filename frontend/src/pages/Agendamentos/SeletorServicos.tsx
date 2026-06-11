import { Check, Loader2 } from 'lucide-react';

import { formatarDuracao, formatarReais } from '@/lib/format';
import { cn } from '@/lib/utils';

import type { ServicoResumo } from '@/types/servico';

interface SeletorServicosProps {
  servicos: ServicoResumo[];
  selecionados: string[];
  onToggle: (servicoId: string) => void;
  carregando: boolean;
  erro: boolean;
  /** Mensagem de validação do formulário (Zod ou backend). */
  mensagemErro?: string;
  disabled?: boolean;
}

/**
 * Multi-seleção de serviços (RF007). Cada serviço é um botão alternável,
 * acessível por teclado (`aria-pressed`). O resumo de totais é calculado
 * pela página a partir dos itens selecionados.
 */
export function SeletorServicos({
  servicos,
  selecionados,
  onToggle,
  carregando,
  erro,
  mensagemErro,
  disabled = false,
}: SeletorServicosProps) {
  const selecionadosSet = new Set(selecionados);

  if (carregando) {
    return (
      <div
        className="flex items-center gap-2 rounded-xl border border-border bg-card px-4 py-6 text-sm text-muted-foreground"
        aria-live="polite"
      >
        <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />
        Carregando serviços…
      </div>
    );
  }

  if (erro) {
    return (
      <div
        role="alert"
        className="rounded-xl border border-amber-500/30 bg-amber-950/20 px-4 py-3 text-sm text-amber-300"
      >
        Não foi possível carregar o catálogo de serviços. O endpoint{' '}
        <code className="font-mono text-amber-200">GET /api/v1/servicos</code> ainda é uma
        dependência pendente do backend.
      </div>
    );
  }

  if (servicos.length === 0) {
    return (
      <div className="rounded-xl border border-border bg-card px-4 py-6 text-center text-sm text-muted-foreground">
        Nenhum serviço ativo no catálogo.
      </div>
    );
  }

  return (
    <div className="space-y-2">
      <div
        role="group"
        aria-label="Serviços do agendamento"
        className="grid grid-cols-1 gap-2 sm:grid-cols-2"
      >
        {servicos.map((servico) => {
          const ativo = selecionadosSet.has(servico.id);
          return (
            <button
              key={servico.id}
              type="button"
              disabled={disabled}
              aria-pressed={ativo}
              onClick={() => onToggle(servico.id)}
              className={cn(
                'flex items-center justify-between gap-3 rounded-xl border px-4 py-3 text-left text-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500/50 disabled:cursor-not-allowed disabled:opacity-50',
                ativo
                  ? 'border-red-500/60 bg-red-950/30 text-foreground'
                  : 'border-border bg-card text-foreground hover:border-ring hover:bg-accent',
              )}
            >
              <span className="min-w-0">
                <span className="block truncate font-medium">{servico.nome}</span>
                <span className="mt-0.5 block text-xs text-muted-foreground">
                  {formatarReais(servico.precoBase)} · {formatarDuracao(servico.duracaoMin)}
                </span>
              </span>
              <span
                aria-hidden="true"
                className={cn(
                  'flex h-5 w-5 shrink-0 items-center justify-center rounded-full border',
                  ativo
                    ? 'border-red-500 bg-red-600 text-white'
                    : 'border-border bg-transparent text-transparent',
                )}
              >
                <Check className="h-3 w-3" />
              </span>
            </button>
          );
        })}
      </div>
      {mensagemErro && (
        <p role="alert" className="text-xs text-red-400">
          {mensagemErro}
        </p>
      )}
    </div>
  );
}
