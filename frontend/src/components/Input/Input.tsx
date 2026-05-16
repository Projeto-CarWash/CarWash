import { forwardRef } from 'react';

import styles from './Input.module.css';

import type { InputHTMLAttributes, ReactNode } from 'react';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  /** Ícone renderizado à direita do input (ex: toggle de senha) */
  rightIcon?: ReactNode;
  /** Callback do clique no ícone */
  onIconClick?: () => void;
  /** aria-label do botão do ícone */
  iconAriaLabel?: string;
}

const Input = forwardRef<HTMLInputElement, InputProps>(
  (
    {
      label,
      error,
      rightIcon,
      onIconClick,
      iconAriaLabel,
      id,
      className,
      ...rest
    },
    ref,
  ) => {
    const inputId = id ?? `input-${label.toLowerCase().replace(/\s+/g, '-')}`;
    const errorId = `${inputId}-error`;
    const hasError = Boolean(error);

    return (
      <div className={styles.inputGroup}>
        <label className={styles.label} htmlFor={inputId}>
          {label}
        </label>

        <div className={styles.inputWrapper}>
          <input
            ref={ref}
            id={inputId}
            className={`${styles.input} ${rightIcon ? styles.inputWithIcon : ''} ${hasError ? styles.inputError : ''} ${className ?? ''}`}
            aria-invalid={hasError}
            aria-describedby={hasError ? errorId : undefined}
            {...rest}
          />

          {rightIcon && (
            <button
              type="button"
              className={styles.iconButton}
              onClick={onIconClick}
              aria-label={iconAriaLabel ?? 'Ação do ícone'}
              tabIndex={0}
            >
              {rightIcon}
            </button>
          )}
        </div>

        {hasError && (
          <span id={errorId} className={styles.errorMessage} role="alert">
            {error}
          </span>
        )}
      </div>
    );
  },
);

Input.displayName = 'Input';

export default Input;
