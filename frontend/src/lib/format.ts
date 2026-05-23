/**
 * Formatadores de apresentação (pt-BR) compartilhados.
 */

const BRL = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
});

/** Formata um valor numérico em reais (ex.: `R$ 89,90`). */
export function formatarReais(valor: number): string {
  return BRL.format(valor);
}

/**
 * Formata uma duração em minutos de forma legível
 * (ex.: `45 min`, `1 h 30 min`, `2 h`).
 */
export function formatarDuracao(minutos: number): string {
  if (minutos <= 0) return '0 min';
  const horas = Math.floor(minutos / 60);
  const resto = minutos % 60;
  if (horas === 0) return `${resto} min`;
  if (resto === 0) return `${horas} h`;
  return `${horas} h ${resto} min`;
}

const DATA_HORA = new Intl.DateTimeFormat('pt-BR', {
  dateStyle: 'short',
  timeStyle: 'short',
});

/**
 * Formata uma data/hora ISO-8601 em pt-BR (ex.: `22/05/2026 14:00`).
 * Devolve string vazia se a entrada não for uma data válida.
 */
export function formatarDataHora(iso: string): string {
  const data = new Date(iso);
  if (Number.isNaN(data.getTime())) return '';
  return DATA_HORA.format(data);
}
