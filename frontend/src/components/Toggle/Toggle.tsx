import styles from './Toggle.module.css';

import type React from 'react';


interface ToggleProps {
  label?: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  disabled?: boolean;
}

const Toggle: React.FC<ToggleProps> = ({ label, checked, onChange, disabled }) => {
  return (
    <div className={styles.container}>
      {label && <span className={styles.label}>{label}</span>}
      {/* eslint-disable-next-line jsx-a11y/label-has-associated-control */}
      <label className={`${styles.switch} ${disabled ? styles.disabled : ''}`}>
        <input
          type="checkbox"
          checked={checked}
          onChange={(e) => onChange(e.target.checked)}
          disabled={disabled}
        />
        <span className={styles.slider} />
      </label>
    </div>
  );
};

export default Toggle;
