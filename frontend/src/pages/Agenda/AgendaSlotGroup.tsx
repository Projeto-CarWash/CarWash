import { CalendarClock, Layers } from 'lucide-react';

import { formatarFaixaHorario } from './agendaFormat';

interface AgendaSlotGroupProps {
  /** ISO-8601 do início do slot. */
  inicio: string;
  /** ISO-8601 do fim do slot. */
  fim: string;
  /** Quantidade de agendamentos neste slot. */
  quantidade: number;
  /** Cards/rows renderizados dentro do slot. */
  children: React.ReactNode;
  /** Se `true`, usa grid multi-coluna (detalhado); se `false`, lista vertical (simples). */
  modoGrade: boolean;
}

/**
 * Agrupa N agendamentos do mesmo slot de horário (RF008.1).
 *
 * <p>Exibe um header com a faixa horária e um badge indicando quantos
 * agendamentos simultâneos existem. No modo grade (detalhado), distribui
 * os cards em colunas responsivas. No modo lista (simples), empilha
 * verticalmente.</p>
 */
export function AgendaSlotGroup({
  inicio,
  fim,
  quantidade,
  children,
  modoGrade,
}: AgendaSlotGroupProps) {
  const descricao = `${quantidade} agendamento${quantidade > 1 ? 's' : ''} — ${formatarFaixaHorario(inicio, fim)}`;

  return (
    <section
      role="group"
      aria-label={descricao}
      className="rounded-2xl border border-border bg-white/60 dark:border-zinc-800/60 dark:bg-zinc-900/30"
    >
      {/* Header do slot */}
      <div className="flex flex-wrap items-center gap-2 border-b border-border px-4 py-3 dark:border-zinc-800/40">
        <CalendarClock className="h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
        <span className="text-sm font-semibold tabular-nums text-foreground dark:text-zinc-100">
          {formatarFaixaHorario(inicio, fim)}
        </span>

        {quantidade > 1 && (
          <span className="inline-flex items-center gap-1 rounded-full bg-red-500/10 px-2.5 py-0.5 text-[10px] font-bold tracking-wide text-red-600 dark:text-red-400">
            <Layers className="h-3 w-3" aria-hidden="true" />
            {quantidade} simultâneos
          </span>
        )}
      </div>

      {/* Conteúdo — grid ou lista */}
      <div
        className={
          modoGrade
            ? 'grid grid-cols-1 gap-4 p-4 sm:grid-cols-2 lg:grid-cols-3'
            : 'flex flex-col gap-2 p-4'
        }
      >
        {children}
      </div>
    </section>
  );
}
