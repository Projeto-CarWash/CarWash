import { CalendarClock, Car, Pencil, Trash2, User } from 'lucide-react';

import { classesStatus, formatarFaixaHorario, rotuloStatus } from './agendaFormat';

import type { AgendaItemSimples } from '@/types/agenda';

interface AgendaItemSimplesRowProps {
  item: AgendaItemSimples;
  /** Callback disparado ao clicar/ativar a linha (RF008.1 — detalhe individual). */
  onClick?: (item: AgendaItemSimples) => void;
  onEditar?: (item: AgendaItemSimples) => void;
  onCancelar?: (item: AgendaItemSimples) => void;
}

/**
 * Linha compacta da agenda no formato `simples` (RF009).
 *
 * <p>Renderiza como `<li>` — o container é uma `<ul>`. Layout responsivo:
 * empilha no mobile, alinha em colunas no desktop.</p>
 */
export function AgendaItemSimplesRow({
  item,
  onClick,
  onEditar,
  onCancelar,
}: AgendaItemSimplesRowProps) {
  const descricao = `Agendamento: ${item.clienteNome}, placa ${item.veiculoPlaca}, ${rotuloStatus(item.status)}`;

  function handleKeyDown(e: React.KeyboardEvent) {
    if (onClick && (e.key === 'Enter' || e.key === ' ')) {
      e.preventDefault();
      onClick(item);
    }
  }

  const content = (
    <>
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

      {/* eslint-disable-next-line jsx-a11y/no-static-element-interactions */}
      <div
        className="flex items-center gap-2 shrink-0 self-start sm:self-center"
        onClick={(e) => e.stopPropagation()}
        onKeyDown={(e) => e.stopPropagation()}
      >
        {onEditar && item.status === 'AGENDADO' && (
          <button
            type="button"
            className="p-1 rounded text-zinc-400 hover:text-zinc-600 dark:hover:text-zinc-200 focus-visible:ring-2 focus-visible:ring-red-500/50"
            aria-label="Editar agendamento"
            onClick={() => onEditar(item)}
          >
            <Pencil className="h-4 w-4" />
          </button>
        )}
        {onCancelar && (item.status === 'AGENDADO' || item.status === 'EM_ANDAMENTO') && (
          <button
            type="button"
            className="p-1 rounded text-zinc-400 hover:text-red-500 focus-visible:ring-2 focus-visible:ring-red-500/50"
            aria-label="Cancelar agendamento"
            onClick={() => onCancelar(item)}
          >
            <Trash2 className="h-4 w-4" />
          </button>
        )}
        <span
          className={`shrink-0 rounded-full px-2.5 py-1 text-[10px] font-bold tracking-[0.12em] ${classesStatus(
            item.status,
          )}`}
        >
          {rotuloStatus(item.status).toUpperCase()}
        </span>
      </div>
    </>
  );

  if (onClick) {
    return (
      <li>
        <div
          role="button"
          tabIndex={0}
          className="w-full text-left flex flex-col gap-2 rounded-xl border border-zinc-200/70 bg-white/60 p-4 transition-colors hover:border-red-500/40 sm:flex-row sm:items-center sm:gap-4 dark:border-zinc-800/60 dark:bg-zinc-900/30 cursor-pointer focus-visible:ring-2 focus-visible:ring-red-500/50 focus-visible:outline-none"
          aria-label={descricao}
          onClick={() => onClick(item)}
          onKeyDown={handleKeyDown}
        >
          {content}
        </div>
      </li>
    );
  }

  return (
    <li
      className="flex flex-col gap-2 rounded-xl border border-zinc-200/70 bg-white/60 p-4 sm:flex-row sm:items-center sm:gap-4 dark:border-zinc-800/60 dark:bg-zinc-900/30"
      aria-label={descricao}
    >
      {content}
    </li>
  );
}
