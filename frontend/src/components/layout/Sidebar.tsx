import {
  BarChart3,
  Building2,
  CalendarDays,
  CarFront,
  DollarSign,
  LayoutDashboard,
  Settings,
  UserCog,
  Users,
  Wrench,
} from 'lucide-react';
import { NavLink, useLocation } from 'react-router-dom';

import { ThemeToggle } from '@/components/ThemeToggle';
import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Separator } from '@/components/ui/separator';
import { useAuth } from '@/hooks/useAuth';

interface NavLinkItem {
  icon: React.ComponentType<{ className?: string }>;
  label: string;
  to?: string;
}

const operacaoLinks: NavLinkItem[] = [
  { icon: LayoutDashboard, label: 'Painel', to: '/dashboard' },
  { icon: Building2, label: 'Filiais', to: '/filiais' },
  { icon: Wrench, label: 'Serviços', to: '/servicos' },
  { icon: Users, label: 'Clientes', to: '/clientes' },
  { icon: CarFront, label: 'Veículos', to: '/veiculos' },
];

const gestaoLinks: NavLinkItem[] = [
  { icon: CalendarDays, label: 'Agendamentos', to: '/agendamentos' },
  { icon: DollarSign, label: 'Financeiro' },
  { icon: BarChart3, label: 'Relatórios' },
  { icon: UserCog, label: 'Equipe', to: '/usuarios' },
];

const sistemaLinks: NavLinkItem[] = [{ icon: Settings, label: 'Configurações' }];

export function Sidebar() {
  const { user } = useAuth();
  const { pathname } = useLocation();

  const inicial = (user?.nome?.[0] ?? '?').toUpperCase();

  function isActive(to?: string): boolean {
    if (!to) return false;
    return pathname === to || pathname.startsWith(`${to}/`);
  }

  function renderItem(link: NavLinkItem) {
    const baseClasses =
      'group relative flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-all';
    const ativo = isActive(link.to);
    const ativoClasses = 'bg-gradient-to-r from-red-600/20 to-transparent text-foreground';
    const inativoClasses = 'text-muted-foreground hover:bg-accent hover:text-foreground';

    if (link.to) {
      return (
        <NavLink to={link.to} className={`${baseClasses} ${ativo ? ativoClasses : inativoClasses}`}>
          {ativo && (
            <div
              className="absolute -left-3 top-1/2 h-8 w-1.5 -translate-y-1/2 rounded-r-full bg-red-600"
              aria-hidden="true"
            />
          )}
          <link.icon className={`h-4 w-4 ${ativo ? 'text-foreground' : 'text-muted-foreground'}`} />
          <span>{link.label}</span>
        </NavLink>
      );
    }

    return (
      <button
        type="button"
        disabled
        className={`${baseClasses} ${inativoClasses} cursor-not-allowed opacity-60`}
      >
        <link.icon className="h-4 w-4 text-muted-foreground" />
        <span>{link.label}</span>
        <span className="ml-auto text-[9px] tracking-widest text-muted-foreground">EM BREVE</span>
      </button>
    );
  }

  return (
    <aside className="fixed bottom-0 left-0 top-0 z-40 flex w-64 flex-col border-r border-border bg-background">
      <div className="flex items-center gap-3 px-5 py-5">
        <div className="flex h-11 w-11 shrink-0 items-center justify-center overflow-hidden rounded-xl bg-black ring-1 ring-border">
          <img src="/logo.png" alt="CarWash" className="h-full w-full object-contain" />
        </div>
        <div>
          <h1 className="text-lg font-black tracking-wider">
            <span className="text-foreground">CAR</span>
            <span className="text-red-600">WASH</span>
          </h1>
          <p className="mt-0.5 text-[10.5px] font-bold tracking-[0.2em] text-muted-foreground">
            ADMIN <span className="px-0.5 text-muted-foreground">•</span> v2.4
          </p>
        </div>
      </div>

      <Separator className="bg-border" />

      <nav className="flex-1 overflow-y-auto px-3 py-4">
        <p className="mb-2 px-3 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">OPERAÇÃO</p>
        <ul className="mb-4 space-y-0.5">
          {operacaoLinks.map((link) => (
            <li key={link.label}>{renderItem(link)}</li>
          ))}
        </ul>

        <p className="mb-2 px-3 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">GESTÃO</p>
        <ul className="mb-4 space-y-0.5">
          {gestaoLinks.map((link) => (
            <li key={link.label}>{renderItem(link)}</li>
          ))}
        </ul>

        <p className="mb-2 px-3 text-[10px] font-bold tracking-[0.2em] text-muted-foreground">SISTEMA</p>
        <ul className="space-y-0.5">
          {sistemaLinks.map((link) => (
            <li key={link.label}>{renderItem(link)}</li>
          ))}
        </ul>
      </nav>

      <Separator className="bg-border" />

      <div className="flex items-center gap-3 px-5 py-4">
        <Avatar>
          <AvatarFallback className="bg-muted text-xs font-semibold text-foreground">
            {inicial}
          </AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-foreground">{user?.nome ?? '—'}</p>
          <p className="text-[10px] font-semibold tracking-widest text-muted-foreground">
            {user?.perfil?.toUpperCase() ?? ''}
          </p>
        </div>
        <ThemeToggle className="h-8 w-8" />
      </div>
    </aside>
  );
}
