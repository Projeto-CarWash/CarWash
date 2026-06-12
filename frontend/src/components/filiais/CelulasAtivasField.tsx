import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { onlyDigits } from '@/lib/masks';

import type * as React from 'react';

/**
 * Campo controlado de "Células ativas" (RF018), reutilizado no cadastro e na
 * edição de filiais.
 *
 * <p>Garante que apenas inteiros possam ser digitados/colados. Em vez de
 * `type="number"` — que em vários navegadores aceita `e`, `+`, `-`, `.` e `,`
 * e reporta valor vazio em estados intermediários — usa `type="text"` com
 * `inputMode="numeric"` e três defesas: bloqueio de tecla (`onKeyDown`),
 * higienização de colagem (`onPaste`) e remoção de não-dígitos no `onChange`.
 * A validação canônica (1–100, obrigatório) continua no schema Zod e no
 * backend (RAT03).</p>
 *
 * <p>Componente apresentacional: recebe os campos do `Controller`
 * (`value`/`onChange`/`onBlur`/`ref`) para não acoplar a tipos de formulário
 * específicos. Acessibilidade (RNF008): rótulo explícito, texto auxiliar,
 * `aria-describedby`, `aria-invalid` e mensagem de erro em região assertiva.</p>
 */

const TEXTO_AUXILIAR = 'Informe um número inteiro entre 1 e 100.';

/** Teclas de controle/navegação sempre permitidas (não produzem caractere). */
const TECLAS_PERMITIDAS = new Set([
  'Backspace',
  'Delete',
  'Tab',
  'Enter',
  'Escape',
  'ArrowLeft',
  'ArrowRight',
  'ArrowUp',
  'ArrowDown',
  'Home',
  'End',
]);

interface CelulasAtivasFieldProps {
  id?: string;
  value: string | number | undefined;
  onChange: (value: string) => void;
  onBlur: () => void;
  inputRef?: React.Ref<HTMLInputElement>;
  error?: string;
  disabled?: boolean;
}

export function CelulasAtivasField({
  id = 'filial-celulas',
  value,
  onChange,
  onBlur,
  inputRef,
  error,
  disabled = false,
}: CelulasAtivasFieldProps) {
  const errorId = `${id}-error`;
  const hintId = `${id}-hint`;
  const valor = value === undefined || value === null ? '' : String(value);

  function handleKeyDown(event: React.KeyboardEvent<HTMLInputElement>) {
    // Preserva atalhos (copiar/colar/selecionar) e teclas de navegação.
    if (event.ctrlKey || event.metaKey || event.altKey) return;
    if (TECLAS_PERMITIDAS.has(event.key)) return;
    // Bloqueia qualquer caractere único que não seja 0-9 — cobre explicitamente
    // e, E, +, -, . e , aceitos por inputs `number` em alguns navegadores.
    if (event.key.length === 1 && !/[0-9]/.test(event.key)) {
      event.preventDefault();
    }
  }

  function handlePaste(event: React.ClipboardEvent<HTMLInputElement>) {
    const colado = event.clipboardData.getData('text');
    if (!/^[0-9]*$/.test(colado)) {
      event.preventDefault();
      onChange(onlyDigits(`${valor}${colado}`).slice(0, 3));
    }
  }

  return (
    <div className="flex flex-col gap-2">
      <Label htmlFor={id} className="text-foreground">
        Células ativas
      </Label>
      <Input
        id={id}
        type="text"
        inputMode="numeric"
        pattern="[0-9]*"
        autoComplete="off"
        maxLength={3}
        placeholder="Ex: 4"
        value={valor}
        disabled={disabled}
        onKeyDown={handleKeyDown}
        onPaste={handlePaste}
        onChange={(event) => onChange(onlyDigits(event.target.value))}
        onBlur={onBlur}
        ref={inputRef}
        aria-invalid={!!error}
        aria-describedby={error ? errorId : hintId}
        className={`h-10 rounded-lg border px-3 text-sm text-foreground placeholder:text-muted-foreground focus-visible:ring-0 ${
          error
            ? 'border-red-500/60 bg-red-950/20 focus-visible:border-red-500'
            : 'border-border bg-background focus-visible:border-ring'
        }`}
      />
      {error ? (
        <p id={errorId} role="alert" aria-live="assertive" className="text-xs text-red-400">
          {error}
        </p>
      ) : (
        <p id={hintId} className="text-xs text-muted-foreground">
          {TEXTO_AUXILIAR}
        </p>
      )}
    </div>
  );
}
