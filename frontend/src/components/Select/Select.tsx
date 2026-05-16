import styles from './Select.module.css';

import type React from 'react';


interface Option {
  value: string;
  label: string;
}

interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  options: Option[];
  error?: string;
}

const Select: React.FC<SelectProps> = ({ label, options, error, ...props }) => {
  return (
    <div className={styles.container}>
      {label && <label className={styles.label} htmlFor={props.id}>{label}</label>}
      <div className={styles.selectWrapper}>
        <select 
          className={`${styles.select} ${error ? styles.error : ''}`} 
          {...props}
        >
          {options.map((option) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        <span className={styles.arrow} />
      </div>
      {error && <span className={styles.errorMessage}>{error}</span>}
    </div>
  );
};

export default Select;
