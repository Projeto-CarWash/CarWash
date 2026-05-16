import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  AlertCircle,
  Calendar,
  Car,
  Eye,
  EyeOff,
  BarChart3,
} from 'lucide-react';

import Button from '../../components/Button/Button';
import Input from '../../components/Input/Input';
import { useAuth } from '../../hooks/useAuth';
import { validateEmail, validatePassword } from '../../utils/validators';
import logo from '../../assets/logo.png';
import styles from './Login.module.css';

interface FormErrors {
  email: string;
  password: string;
}

export default function Login() {
  const navigate = useNavigate();
  const { login, isAuthenticated } = useAuth();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [rememberMe, setRememberMe] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({ email: '', password: '' });
  const [generalError, setGeneralError] = useState('');

  // Redireciona se já autenticado
  useEffect(() => {
    if (isAuthenticated) {
      void navigate('/dashboard', { replace: true });
    }
  }, [isAuthenticated, navigate]);

  // Restaura e-mail se "Lembrar-me" foi marcado anteriormente
  useEffect(() => {
    const savedEmail = localStorage.getItem('carwash_remember_email');
    if (savedEmail) {
      setEmail(savedEmail);
      setRememberMe(true);
    }
  }, []);

  const validateForm = useCallback((): boolean => {
    const emailError = validateEmail(email);
    const passwordError = validatePassword(password);

    setErrors({ email: emailError, password: passwordError });

    return emailError === '' && passwordError === '';
  }, [email, password]);

  const handleSubmit = useCallback(
    async (e: React.FormEvent) => {
      e.preventDefault();
      setGeneralError('');

      if (!validateForm()) return;

      setIsLoading(true);

      try {
        await login(email, password);

        // Salva e-mail se "Lembrar-me" estiver marcado
        if (rememberMe) {
          localStorage.setItem('carwash_remember_email', email);
        } else {
          localStorage.removeItem('carwash_remember_email');
        }

        void navigate('/dashboard', { replace: true });
      } catch (err) {
        const message =
          err instanceof Error
            ? err.message
            : 'Erro ao realizar login. Tente novamente.';
        setGeneralError(message);
      } finally {
        setIsLoading(false);
      }
    },
    [email, password, rememberMe, login, navigate, validateForm],
  );

  const handleEmailBlur = useCallback(() => {
    if (email.trim()) {
      setErrors((prev) => ({ ...prev, email: validateEmail(email) }));
    }
  }, [email]);

  const handlePasswordBlur = useCallback(() => {
    if (password) {
      setErrors((prev) => ({
        ...prev,
        password: validatePassword(password),
      }));
    }
  }, [password]);

  const isFormEmpty = email.trim() === '' || password === '';

  return (
    <div className={styles.loginContainer}>
      {/* ===== Painel Esquerdo — Branding ===== */}
      <div className={styles.brandPanel} aria-hidden="true">
        {/* Partículas decorativas */}
        <div className={`${styles.particle} ${styles.particle1}`} />
        <div className={`${styles.particle} ${styles.particle2}`} />
        <div className={`${styles.particle} ${styles.particle3}`} />
        {/* Grid lines */}
        <div className={`${styles.gridLine} ${styles.gridLine1}`} />
        <div className={`${styles.gridLine} ${styles.gridLine2}`} />
        <div className={`${styles.gridLine} ${styles.gridLine3}`} />

        <div className={styles.brandContent}>
          <img
            src={logo}
            alt="CarWash"
            style={{ width: '100%', maxWidth: '320px', height: 'auto', marginBottom: '24px' }}
          />
          <h1 className={styles.brandTitle}>
            Sistema de Gestão
          </h1>
          <p className={styles.brandSubtitle}>
            Agendamento, clientes, veículos e finanças
            para lavagem e estética automotiva.
          </p>

          <div className={styles.features}>
            <div className={styles.featureItem}>
              <div className={styles.featureIcon}>
                <Calendar size={20} />
              </div>
              <div className={styles.featureText}>
                <h3>Gestão de Agenda</h3>
                <p>Agendamentos simultâneos e controle por filial</p>
              </div>
            </div>

            <div className={styles.featureItem}>
              <div className={styles.featureIcon}>
                <Car size={20} />
              </div>
              <div className={styles.featureText}>
                <h3>Cadastro Completo</h3>
                <p>Clientes, veículos e histórico de atendimentos</p>
              </div>
            </div>

            <div className={styles.featureItem}>
              <div className={styles.featureIcon}>
                <BarChart3 size={20} />
              </div>
              <div className={styles.featureText}>
                <h3>Dashboard Operacional</h3>
                <p>Métricas e indicadores em tempo real</p>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* ===== Painel Direito — Formulário ===== */}
      <main className={styles.formPanel}>
        <div className={styles.formCard}>
          <img
            src={logo}
            alt="CarWash - Sistema de Gestão"
            className={styles.logoSmall}
          />

          <div className={styles.formHeader}>
            <h2 className={styles.formTitle}>Acesse sua conta</h2>
            <p className={styles.formDescription}>
              Insira suas credenciais para entrar no sistema
            </p>
          </div>

          <form className={styles.form} onSubmit={handleSubmit} noValidate>
            <Input
              label="E-mail"
              type="email"
              id="login-email"
              placeholder="seu@email.com"
              autoComplete="email"
              value={email}
              onChange={(e) => {
                setEmail(e.target.value);
                if (errors.email) {
                  setErrors((prev) => ({ ...prev, email: '' }));
                }
              }}
              onBlur={handleEmailBlur}
              error={errors.email}
              required
              aria-required="true"
            />

            <Input
              label="Senha"
              type={showPassword ? 'text' : 'password'}
              id="login-password"
              placeholder="••••••••"
              autoComplete="current-password"
              value={password}
              onChange={(e) => {
                setPassword(e.target.value);
                if (errors.password) {
                  setErrors((prev) => ({ ...prev, password: '' }));
                }
              }}
              onBlur={handlePasswordBlur}
              error={errors.password}
              required
              aria-required="true"
              rightIcon={
                showPassword ? <EyeOff size={20} /> : <Eye size={20} />
              }
              onIconClick={() => {
                setShowPassword((prev) => !prev);
              }}
              iconAriaLabel={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
            />

            <div className={styles.formActions}>
              <label className={styles.rememberMe} htmlFor="remember-me">
                <input
                  type="checkbox"
                  id="remember-me"
                  className={styles.checkbox}
                  checked={rememberMe}
                  onChange={(e) => {
                    setRememberMe(e.target.checked);
                  }}
                />
                Lembrar-me
              </label>

              <button
                type="button"
                className={styles.forgotPassword}
                onClick={() => {
                  setGeneralError(
                    'Funcionalidade em desenvolvimento. Contate o administrador para redefinir sua senha.',
                  );
                }}
              >
                Esqueci minha senha
              </button>
            </div>

            <div className={styles.submitSection}>
              {generalError && (
                <div className={styles.errorAlert} role="alert">
                  <AlertCircle
                    size={20}
                    className={styles.errorAlertIcon}
                    aria-hidden="true"
                  />
                  <span>{generalError}</span>
                </div>
              )}

              <Button
                type="submit"
                variant="primary"
                isLoading={isLoading}
                disabled={isFormEmpty}
                id="login-submit"
              >
                Acessar
              </Button>
            </div>
          </form>

          <p className={styles.footer}>
            Problemas para acessar? Contate o administrador do sistema.
          </p>
        </div>
      </main>
    </div>
  );
}
