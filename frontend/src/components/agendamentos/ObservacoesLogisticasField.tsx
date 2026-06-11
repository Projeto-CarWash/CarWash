import { Label } from '@/components/ui/label';

import type * as React from 'react';

/**
 * Campo controlado de "Observações Logísticas", reutilizado no formulário de
 * criação, na tela de edição e na tela de detalhe do agendamento.
 *
 * <p>Características: textarea opcional, máx. 1000 caracteres, contador
 * visível e acessível, suporte a `readonly` por status, trim no envio (feito
 * pelo chamador), quebras de linha preservadas na exibição. A validação
 * canônica permanece no backend (RAT03) — este componente fornece apenas
 * feedback de UX.</p>
 *
 * <p>Acessibilidade (RNF008): `<label>` explícita, `aria-describedby` para o
 * texto auxiliar/erro, contador como região `aria-live="polite"`,
 * `aria-readonly` e `aria-invalid` conforme estado.</p>
 */

const MAX_CHARS = 1000;

interface ObservacoesLogisticasFieldProps {
  id?: string;
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  inputRef?: React.Ref<HTMLTextAreaElement>;
  error?: string;
  /**
   * Quando `true`, o campo fica bloqueado para edição (status CONCLUIDO,
   * CANCELADO ou EM_ANDAMENTO quando o agendamento não é editável).
   */
  readonly?: boolean;
  disabled?: boolean;
}

export function ObservacoesLogisticasField({
  id = 'observacoes-logisticas',
  value,
  onChange,
  onBlur,
  inputRef,
  error,
  readonly = false,
  disabled = false,
}: ObservacoesLogisticasFieldProps) {
  const contadorId = `${id}-contador`;
  const errorId = `${id}-error`;
  const hintId = `${id}-hint`;

  const comprimento = value.length;
  const isAtLimit = comprimento >= MAX_CHARS;

  /** Descrição acessível: usa errorId quando há erro, hintId caso contrário. */
  const describeBy = error ? errorId : hintId;

  function handleChange(event: React.ChangeEvent<HTMLTextAreaElement>) {
    // Bloqueia digitação acima do limite (defesa inline — Zod valida no submit).
    if (event.target.value.length <= MAX_CHARS) {
      onChange(event.target.value);
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex items-baseline justify-between gap-2">
        <Label htmlFor={id} className="text-zinc-300">
          Observações logísticas{' '}
          <span className="text-xs font-normal text-zinc-500">(opcional)</span>
        </Label>

        {/* Contador de caracteres — aria-live anuncia mudanças para leitores de tela */}
        <span
          id={contadorId}
          aria-live="polite"
          aria-atomic="true"
          className={`shrink-0 text-xs tabular-nums ${
            isAtLimit
              ? 'font-semibold text-red-400'
              : comprimento >= MAX_CHARS * 0.9
                ? 'text-amber-400'
                : 'text-zinc-500'
          }`}
        >
          {comprimento}/{MAX_CHARS}
        </span>
      </div>

      <textarea
        id={id}
        ref={inputRef}
        value={value}
        rows={4}
        maxLength={MAX_CHARS}
        placeholder={
          readonly ? '' : 'Informe observações complementares sobre a logística deste agendamento…'
        }
        readOnly={readonly}
        disabled={disabled}
        onBlur={onBlur}
        onChange={handleChange}
        aria-invalid={!!error}
        aria-readonly={readonly}
        aria-describedby={describeBy}
        className={`w-full resize-y rounded-lg border px-3 py-2.5 text-sm leading-relaxed transition-colors placeholder:text-zinc-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-offset-0 dark:[color-scheme:dark] ${
          readonly || disabled
            ? 'cursor-default border-zinc-700/40 bg-zinc-900/20 text-zinc-400 focus-visible:ring-0'
            : error
              ? 'border-red-500/60 bg-red-950/20 text-zinc-100 focus-visible:border-red-500 focus-visible:ring-red-500/30'
              : 'border-zinc-700/60 bg-zinc-950/40 text-zinc-100 focus-visible:border-zinc-500 focus-visible:ring-zinc-500/20'
        }`}
      />

      {/* Região de feedback: erro (assertiva) ou hint (não interrompe leitura) */}
      {error ? (
        <p id={errorId} role="alert" aria-live="assertive" className="text-xs text-red-400">
          {error}
        </p>
      ) : (
        <p id={hintId} className="text-xs text-zinc-500">
          {readonly
            ? 'Campo somente leitura no status atual do agendamento.'
            : 'Máximo 1000 caracteres. Quebras de linha são preservadas.'}
        </p>
      )}
    </div>
  );
}
