import { LogOut, LayoutDashboard, UserPlus } from 'lucide-react';
import { useNavigate } from 'react-router-dom';

import Button from '../../components/Button/Button';
import { useAuth } from '../../hooks/useAuth';

export default function Dashboard() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    void navigate('/login', { replace: true });
  };

  return (
    <div
      style={{
        minHeight: '100vh',
        backgroundColor: 'var(--color-background)',
        display: 'flex',
        flexDirection: 'column',
      }}
    >
      {/* Header */}
      <header
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '16px 32px',
          backgroundColor: 'var(--color-surface)',
          borderBottom: '1px solid var(--color-border)',
        }}
      >
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '12px',
          }}
        >
          <LayoutDashboard size={24} color="var(--color-primary)" />
          <h1
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: 'var(--font-size-xl)',
              fontWeight: 700,
              color: 'var(--color-text-primary)',
            }}
          >
            Dashboard
          </h1>
        </div>

        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '16px',
          }}
        >
          <span
            style={{
              fontSize: 'var(--font-size-sm)',
              color: 'var(--color-text-secondary)',
            }}
          >
            Olá, <strong style={{ color: 'var(--color-text-primary)' }}>{user?.name ?? 'Usuário'}</strong>
          </span>

          <div style={{ width: '120px' }}>
            <Button
              variant="secondary"
              onClick={handleLogout}
              id="logout-button"
            >
              <LogOut size={18} />
              Sair
            </Button>
          </div>
        </div>
      </header>

      {/* Conteúdo */}
      <main
        style={{
          flex: 1,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '32px',
        }}
      >
        <div
          style={{
            textAlign: 'center',
            padding: '48px',
            backgroundColor: 'var(--color-surface)',
            borderRadius: 'var(--radius-lg)',
            border: '1px solid var(--color-border)',
            maxWidth: '500px',
            width: '100%',
          }}
        >
          <div
            style={{
              width: '64px',
              height: '64px',
              margin: '0 auto 24px',
              backgroundColor: 'var(--color-primary-glow-strong)',
              borderRadius: 'var(--radius-lg)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <LayoutDashboard size={32} color="var(--color-primary)" />
          </div>

          <h2
            style={{
              fontFamily: 'var(--font-heading)',
              fontSize: 'var(--font-size-2xl)',
              fontWeight: 700,
              color: 'var(--color-text-primary)',
              marginBottom: '8px',
            }}
          >
            Bem-vindo ao CarWash
          </h2>
          <p
            style={{
              fontSize: 'var(--font-size-base)',
              color: 'var(--color-text-secondary)',
              lineHeight: 1.6,
            }}
          >
            Login realizado com sucesso! Este é o painel principal do sistema.
            Os módulos de gestão serão implementados em breve.
          </p>

          <div style={{ marginTop: '32px' }}>
            <Button 
              variant="primary" 
              // cspell:disable-next-line
              onClick={() => navigate('/usuarios/novo')}
            >
              <UserPlus size={20} />
              Cadastrar Novo Usuário
            </Button>
          </div>
        </div>
      </main>
    </div>
  );
}
