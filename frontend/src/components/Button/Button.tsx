import styles from './Button.module.css';

import type { ButtonHTMLAttributes, ReactNode } from 'react';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary';
  isLoading?: boolean;
  children: ReactNode;
}

export default function Button({
  variant = 'primary',
  isLoading = false,
  children,
  disabled,
  className,
  ...rest
}: ButtonProps) {
  const isDisabled = disabled ?? isLoading;

  return (
    <button
      className={`${styles.button} ${styles[variant]} ${isLoading ? styles.loading : ''} ${className ?? ''}`}
      disabled={isDisabled}
      {...rest}
    >
      {isLoading ? (
        <>
          <span
            className={`${styles.spinner} ${variant === 'secondary' ? styles.secondarySpinner : ''}`}
            aria-hidden="true"
          />
          <span>Acessando...</span>
        </>
      ) : (
        children
      )}
    </button>
  );
}
