import type { AgendaStatus } from '@/types/agenda';

/**
 * Utilitários de apresentação específicos da visualização de agenda (RF009).
 */

/** Rótulo legível (pt-BR) de cada status. */
const ROTULOS_STATUS: Record<AgendaStatus, string> = {
  AGENDADO: 'Agendado',
  EM_ANDAMENTO: 'Em andamento',
  CONCLUIDO: 'Concluído',
  CANCELADO: 'Cancelado',
};

/** Classes Tailwind do badge de cada status (tema vermelho/preto, claro/escuro). */
const CLASSES_STATUS: Record<AgendaStatus, string> = {
  AGENDADO: 'bg-blue-500/15 text-blue-600 dark:text-blue-300',
  EM_ANDAMENTO: 'bg-amber-500/15 text-amber-600 dark:text-amber-300',
  CONCLUIDO: 'bg-green-500/15 text-green-600 dark:text-green-300',
  CANCELADO: 'bg-zinc-500/15 text-zinc-500 dark:text-zinc-400',
};

/** Retorna o rótulo legível de um status. */
export function rotuloStatus(status: AgendaStatus): string {
  return ROTULOS_STATUS[status];
}

/** Retorna as classes do badge de um status. */
export function classesStatus(status: AgendaStatus): string {
  return CLASSES_STATUS[status];
}

const FMT_DATA_HORA = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
});

const FMT_HORA = new Intl.DateTimeFormat('pt-BR', {
  hour: '2-digit',
  minute: '2-digit',
});

const FMT_DATA = new Intl.DateTimeFormat('pt-BR', {
  weekday: 'short',
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
});

/** Formata um instante ISO-8601 como `dd/mm/aaaa HH:mm` (fuso local). */
export function formatarDataHora(iso: string): string {
  return FMT_DATA_HORA.format(new Date(iso));
}

/** Formata apenas a hora (`HH:mm`) de um instante ISO-8601 (fuso local). */
export function formatarHora(iso: string): string {
  return FMT_HORA.format(new Date(iso));
}

/** Formata a data por extenso curta (`seg., 27/04/2026`) de um ISO-8601. */
export function formatarData(iso: string): string {
  return FMT_DATA.format(new Date(iso));
}

/**
 * Formata uma faixa de horário do mesmo dia (`27/04/2026 13:00 — 14:30`) ou,
 * quando início e fim caem em dias diferentes, mostra a data nas duas pontas.
 */
export function formatarFaixaHorario(inicioIso: string, fimIso: string): string {
  const inicio = new Date(inicioIso);
  const fim = new Date(fimIso);
  const mesmoDia =
    inicio.getFullYear() === fim.getFullYear() &&
    inicio.getMonth() === fim.getMonth() &&
    inicio.getDate() === fim.getDate();

  if (mesmoDia) {
    return `${FMT_DATA_HORA.format(inicio)} — ${FMT_HORA.format(fim)}`;
  }
  return `${FMT_DATA_HORA.format(inicio)} — ${FMT_DATA_HORA.format(fim)}`;
}

/** Formata um documento (CPF 11 dígitos ou CNPJ 14 dígitos); senão devolve cru. */
export function formatarCpfCnpj(documento: string): string {
  const d = documento.replace(/\D/g, '');
  if (d.length === 11) {
    return `${d.slice(0, 3)}.${d.slice(3, 6)}.${d.slice(6, 9)}-${d.slice(9)}`;
  }
  if (d.length === 14) {
    return `${d.slice(0, 2)}.${d.slice(2, 5)}.${d.slice(5, 8)}/${d.slice(8, 12)}-${d.slice(12)}`;
  }
  return documento;
}

/** Formata um telefone/celular brasileiro; aceita `null` (devolve `—`). */
export function formatarTelefone(telefone: string | null): string {
  if (!telefone) return '—';
  const d = telefone.replace(/\D/g, '');
  if (d.length === 11) {
    return `(${d.slice(0, 2)}) ${d.slice(2, 7)}-${d.slice(7)}`;
  }
  if (d.length === 10) {
    return `(${d.slice(0, 2)}) ${d.slice(2, 6)}-${d.slice(6)}`;
  }
  return telefone;
}
