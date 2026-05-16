import { UserPlus, ArrowLeft, CheckCircle, AlertCircle } from 'lucide-react';
import { useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';

import Button from '../../components/Button/Button';
import Input from '../../components/Input/Input';
import Select from '../../components/Select/Select';
import Toggle from '../../components/Toggle/Toggle';
import userService from '../../services/userService';
import { validateEmail, validatePassword } from '../../utils/validators';

import styles from './UserForm.module.css';

import type { CreateUserData, UserRole } from '../../types/user';
import type React from 'react';

const UserForm: React.FC = () => {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState(false);
  const [globalError, setGlobalError] = useState<string | null>(null);

  const [formData, setFormData] = useState<CreateUserData>({
    name: '',
    email: '',
    role: 'ADMIN',
    password: '',
    confirmPassword: '',
    status: 'ACTIVE',
  });

  const [errors, setErrors] = useState<Partial<Record<keyof CreateUserData, string>>>({});

  // Refs para auto-foco em erro
  const nameRef = useRef<HTMLInputElement>(null);
  const emailRef = useRef<HTMLInputElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);
  const confirmPasswordRef = useRef<HTMLInputElement>(null);

  const roleOptions = [
    { value: 'ADMIN', label: 'Administrador' },
    { value: 'MANAGER', label: 'Gerente' },
    { value: 'OPERATOR', label: 'Operador' },
  ];

  const validateForm = (): boolean => {
    const newErrors: Partial<Record<keyof CreateUserData, string>> = {};

    if (!formData.name.trim()) {
      newErrors.name = 'Nome é obrigatório';
    }

    const emailError = validateEmail(formData.email);
    if (emailError) {
      newErrors.email = emailError;
    } else {
      const emailLower = formData.email.toLowerCase();
      if (!emailLower.endsWith('@gmail.com') && !emailLower.endsWith('@outlook.com')) {
        newErrors.email = 'Apenas e-mails @gmail.com ou @outlook.com são permitidos';
      }
    }

    const passwordError = validatePassword(formData.password ?? '');
    if (passwordError) {
      newErrors.password = passwordError;
    }

    if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = 'As senhas não coincidem';
    }

    setErrors(newErrors);

    // Auto-foco no primeiro erro
    if (newErrors.name) nameRef.current?.focus();
    else if (newErrors.email) emailRef.current?.focus();
    else if (newErrors.password) passwordRef.current?.focus();
    else if (newErrors.confirmPassword) confirmPasswordRef.current?.focus();

    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setGlobalError(null);

    if (!validateForm()) return;

    setLoading(true);
    try {
      await userService.create(formData);
      setSuccess(true);
      setTimeout(() => navigate('/dashboard'), 2000);
    } catch (error) {
      const err = error as { response?: { status?: number } };
      if (err.response?.status === 409) {
        setErrors({ email: 'Já existe um usuário cadastrado com este e-mail' });
        emailRef.current?.focus();
      } else if (err.response?.status === 403) {
        setGlobalError('Você não possui permissão para cadastrar usuários.');
      } else {
        setGlobalError('Ocorreu um erro técnico. Tente novamente em instantes.');
      }
    } finally {
      setLoading(false);
    }
  };

  const handleChange = <T extends keyof CreateUserData>(field: T, value: CreateUserData[T]) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors((prev) => ({ ...prev, [field]: undefined }));
    }
  };

  if (success) {
    return (
      <div className={styles.successContainer}>
        <div className={styles.successCard}>
          <CheckCircle size={64} className={styles.successIcon} />
          <h2>Usuário cadastrado com sucesso!</h2>
          <p>Redirecionando para o painel principal...</p>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.pageContainer}>
      <header className={styles.header}>
        <button onClick={() => navigate(-1)} className={styles.backButton}>
          <ArrowLeft size={20} />
          Voltar
        </button>
        <div className={styles.headerTitle}>
          <UserPlus size={24} color="var(--color-primary)" />
          <h1>Novo Usuário Interno</h1>
        </div>
      </header>

      <main className={styles.main}>
        <form onSubmit={handleSubmit} className={styles.formCard}>
          {globalError && (
            <div className={styles.globalError}>
              <AlertCircle size={20} />
              {globalError}
            </div>
          )}

          <div className={styles.formGrid}>
            <div className={styles.span2}>
              <Input
                ref={nameRef}
                label="Nome Completo"
                placeholder="Ex: João Silva"
                value={formData.name}
                onChange={(e) => handleChange('name', e.target.value)}
                error={errors.name}
                required
              />
            </div>

            <Input
              ref={emailRef}
              label="E-mail"
              type="email"
              placeholder="joao@carwash.com"
              value={formData.email}
              onChange={(e) => handleChange('email', e.target.value)}
              error={errors.email}
              required
            />

            <Select
              label="Perfil de Acesso"
              options={roleOptions}
              value={formData.role}
              onChange={(e) => handleChange('role', e.target.value as UserRole)}
              required
            />

            <Input
              ref={passwordRef}
              label="Senha Inicial"
              type="password"
              placeholder="••••••••"
              value={formData.password}
              onChange={(e) => handleChange('password', e.target.value)}
              error={errors.password}
              required
            />

            <Input
              ref={confirmPasswordRef}
              label="Confirmar Senha"
              type="password"
              placeholder="••••••••"
              value={formData.confirmPassword}
              onChange={(e) => handleChange('confirmPassword', e.target.value)}
              error={errors.confirmPassword}
              required
            />

            <div className={styles.statusSection}>
              <Toggle
                label="Status de Acesso"
                checked={formData.status === 'ACTIVE'}
                onChange={(checked) => handleChange('status', checked ? 'ACTIVE' : 'INACTIVE')}
              />
              <span
                className={`${styles.statusBadge} ${formData.status === 'ACTIVE' ? styles.active : styles.inactive}`}
              >
                {formData.status === 'ACTIVE' ? 'Ativo' : 'Inativo'}
              </span>
            </div>
          </div>

          <div className={styles.formActions}>
            <Button
              type="button"
              variant="secondary"
              onClick={() => navigate(-1)}
              disabled={loading}
            >
              Cancelar
            </Button>
            <Button type="submit" variant="primary" isLoading={loading} loadingText="Salvando...">
              Salvar Usuário
            </Button>
          </div>
        </form>
      </main>
    </div>
  );
};

export default UserForm;
