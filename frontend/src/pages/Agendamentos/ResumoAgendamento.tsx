import { Clock, Receipt } from 'lucide-react';

import { formatarDuracao, formatarReais } from '@/lib/format';

import type { ServicoResumo } from '@/types/servico';

interface ResumoAgendamentoProps {
  servicosSelecionados: ServicoResumo[];
}

/**
 * Resumo inline simples do agendamento (escopo do card 131).
 *
 * <p>Mostra duração e valor TOTAIS ESTIMADOS a partir dos serviços
 * selecionados. Não é a tela de confirmação dedicada (RF015) — esta é um
 * card separado. Os valores definitivos são derivados e congelados pelo
 * backend na criação (campos `valorTotal`/`duracaoTotalMin` da resposta).</p>
 */
export function ResumoAgendamento({ servicosSelecionados }: ResumoAgendamentoProps) {
  const duracaoTotal = servicosSelecionados.reduce((acc, s) => acc + s.duracaoMin, 0);
  const valorTotal = servicosSelecionados.reduce((acc, s) => acc + s.precoBase, 0);
  const vazio = servicosSelecionados.length === 0;

  return (
    <aside
      aria-label="Resumo do agendamento"
      className="rounded-2xl border border-border bg-card p-5"
    >
      <h2 className="text-sm font-semibold tracking-wide text-foreground">Resumo estimado</h2>
      <p className="mt-1 text-xs text-muted-foreground">
        Valores calculados a partir dos serviços. O total definitivo é confirmado pelo servidor.
      </p>

      {vazio ? (
        <p className="mt-4 text-sm text-muted-foreground">Selecione serviços para ver os totais.</p>
      ) : (
        <ul className="mt-4 space-y-2" aria-live="polite">
          {servicosSelecionados.map((servico) => (
            <li
              key={servico.id}
              className="flex items-center justify-between gap-3 text-sm text-foreground"
            >
              <span className="min-w-0 truncate">{servico.nome}</span>
              <span className="shrink-0 tabular-nums text-muted-foreground">
                {formatarReais(servico.precoBase)}
              </span>
            </li>
          ))}
        </ul>
      )}

      <div className="mt-4 space-y-2 border-t border-border pt-4">
        <div className="flex items-center justify-between text-sm">
          <span className="flex items-center gap-2 text-muted-foreground">
            <Clock className="h-4 w-4" aria-hidden="true" />
            Duração total
          </span>
          <span data-testid="resumo-duracao" className="font-medium tabular-nums text-foreground">
            {formatarDuracao(duracaoTotal)}
          </span>
        </div>
        <div className="flex items-center justify-between text-sm">
          <span className="flex items-center gap-2 text-muted-foreground">
            <Receipt className="h-4 w-4" aria-hidden="true" />
            Valor total
          </span>
          <span
            data-testid="resumo-valor"
            className="text-base font-bold tabular-nums text-red-400"
          >
            {formatarReais(valorTotal)}
          </span>
        </div>
      </div>
    </aside>
  );
}
