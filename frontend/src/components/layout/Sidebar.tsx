import {
  BarChart3,
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
  { icon: Wrench, label: 'Serviços' },
  { icon: Users, label: 'Clientes', to: '/clientes' },
  { icon: CarFront, label: 'Veículos' },
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
    const ativoClasses = 'bg-gradient-to-r from-red-600/20 to-transparent text-white';
    const inativoClasses = 'text-zinc-400 hover:bg-zinc-800/40 hover:text-zinc-200';

    if (link.to) {
      return (
        <NavLink to={link.to} className={`${baseClasses} ${ativo ? ativoClasses : inativoClasses}`}>
          {ativo && (
            <div
              className="absolute -left-3 top-1/2 h-8 w-1.5 -translate-y-1/2 rounded-r-full bg-red-600"
              aria-hidden="true"
            />
          )}
          <link.icon className={`h-4 w-4 ${ativo ? 'text-white' : 'text-zinc-500'}`} />
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
        <link.icon className="h-4 w-4 text-zinc-500" />
        <span>{link.label}</span>
        <span className="ml-auto text-[9px] tracking-widest text-zinc-600">EM BREVE</span>
      </button>
    );
  }

  return (
    <aside className="fixed bottom-0 left-0 top-0 z-40 flex w-64 flex-col border-r border-zinc-800/60 bg-zinc-950">
      <div className="flex items-center gap-3 px-5 py-5">
        <div className="flex h-12 w-12 items-center justify-center">
          <img src="/logo.png" alt="Logo CarWash" className="h-full w-full object-contain" />
        </div>
        <div>
          <h1 className="text-lg font-black tracking-wider">
            <span className="text-zinc-50">CAR</span>
            <span className="text-red-600">WASH</span>
          </h1>
          <p className="mt-0.5 text-[10.5px] font-bold tracking-[0.2em] text-zinc-500">
            ADMIN <span className="px-0.5 text-zinc-600">•</span> v2.4
          </p>
        </div>
      </div>

      <Separator className="bg-zinc-800/60" />

      <nav className="flex-1 overflow-y-auto px-3 py-4">
        <p className="mb-2 px-3 text-[10px] font-bold tracking-[0.2em] text-zinc-600">OPERAÇÃO</p>
        <ul className="mb-4 space-y-0.5">
          {operacaoLinks.map((link) => (
            <li key={link.label}>{renderItem(link)}</li>
          ))}
        </ul>

        <p className="mb-2 px-3 text-[10px] font-bold tracking-[0.2em] text-zinc-600">GESTÃO</p>
        <ul className="mb-4 space-y-0.5">
          {gestaoLinks.map((link) => (
            <li key={link.label}>{renderItem(link)}</li>
          ))}
        </ul>

        <p className="mb-2 px-3 text-[10px] font-bold tracking-[0.2em] text-zinc-600">SISTEMA</p>
        <ul className="space-y-0.5">
          {sistemaLinks.map((link) => (
            <li key={link.label}>{renderItem(link)}</li>
          ))}
        </ul>
      </nav>

      <Separator className="bg-zinc-800/60" />

      <div className="flex items-center gap-3 px-5 py-4">
        <Avatar>
          <AvatarFallback className="bg-zinc-800 text-xs font-semibold text-zinc-300">
            {inicial}
          </AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-zinc-200">{user?.nome ?? '—'}</p>
          <p className="text-[10px] font-semibold tracking-widest text-zinc-500">
            {user?.perfil?.toUpperCase() ?? ''}
          </p>
        </div>
        <ThemeToggle className="h-8 w-8" />
      </div>
    </aside>
  );
}
