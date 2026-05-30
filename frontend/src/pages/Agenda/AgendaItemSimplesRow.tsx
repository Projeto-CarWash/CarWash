import { CalendarClock, Car, User } from 'lucide-react';

import { classesStatus, formatarFaixaHorario, rotuloStatus } from './agendaFormat';

import type { AgendaItemSimples } from '@/types/agenda';

interface AgendaItemSimplesRowProps {
  item: AgendaItemSimples;
}

/**
 * Linha compacta da agenda no formato `simples` (RF009).
 *
 * <p>Renderiza como `<li>` — o container é uma `<ul>`. Layout responsivo:
 * empilha no mobile, alinha em colunas no desktop.</p>
 */
export function AgendaItemSimplesRow({ item }: AgendaItemSimplesRowProps) {
  return (
    <li className="flex flex-col gap-2 rounded-xl border border-zinc-200/70 bg-white/60 p-4 transition-colors hover:border-red-500/40 sm:flex-row sm:items-center sm:gap-4 dark:border-zinc-800/60 dark:bg-zinc-900/30">
      <div className="flex items-center gap-2 text-sm font-medium text-zinc-700 sm:w-64 sm:shrink-0 dark:text-zinc-200">
        <CalendarClock className="h-4 w-4 shrink-0 text-red-500" aria-hidden="true" />
        <span className="tabular-nums">{formatarFaixaHorario(item.inicio, item.fim)}</span>
      </div>

      <div className="min-w-0 flex-1">
        <p className="truncate text-sm font-semibold text-zinc-900 dark:text-zinc-50">
          {item.titulo}
        </p>
        <p className="mt-0.5 truncate text-xs text-zinc-500 dark:text-zinc-400">
          {item.servicosResumo}
        </p>
      </div>

      <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-zinc-500 sm:w-56 sm:shrink-0 dark:text-zinc-400">
        <span className="flex items-center gap-1.5">
          <User className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
          <span className="truncate">{item.clienteNome}</span>
        </span>
        <span className="flex items-center gap-1.5">
          <Car className="h-3.5 w-3.5 shrink-0" aria-hidden="true" />
          <span className="font-mono tabular-nums">{item.veiculoPlaca}</span>
        </span>
      </div>

      <span
        className={`shrink-0 self-start rounded-full px-2.5 py-1 text-[10px] font-bold tracking-[0.12em] sm:self-center ${classesStatus(
          item.status,
        )}`}
      >
        {rotuloStatus(item.status).toUpperCase()}
      </span>
    </li>
  );
}
