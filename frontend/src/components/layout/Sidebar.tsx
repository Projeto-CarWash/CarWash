import {
  BarChart3,
  CalendarDays,
  Car,
  CarFront,
  DollarSign,
  LayoutDashboard,
  Settings,
  UserCog,
  Users,
  Wrench,
} from 'lucide-react';

import { Avatar, AvatarFallback } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Separator } from '@/components/ui/separator';

const operacaoLinks = [
  { icon: LayoutDashboard, label: 'Painel', badge: '12' },
  { icon: Wrench, label: 'Serviços' },
  { icon: Users, label: 'Clientes', active: true },
  { icon: CarFront, label: 'Veículos' },
];

const gestaoLinks = [
  { icon: CalendarDays, label: 'Agendamentos' },
  { icon: DollarSign, label: 'Financeiro' },
  { icon: BarChart3, label: 'Relatórios' },
  { icon: UserCog, label: 'Equipe' },
];

const sistemaLinks = [{ icon: Settings, label: 'Configurações' }];

export function Sidebar() {
  return (
    <aside className="fixed left-0 top-0 bottom-0 z-40 flex w-64 flex-col border-r border-zinc-800/60 bg-zinc-950">
      <div className="flex items-center gap-3 px-5 py-5">
        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-red-600 shadow-lg shadow-red-600/25">
          <Car className="h-5 w-5 text-white" />
        </div>
        <div>
          <h1 className="text-sm font-bold tracking-wide text-zinc-50">CARWASH</h1>
          <p className="text-[10px] font-medium tracking-widest text-zinc-500">ADMIN / v2.4</p>
        </div>
      </div>

      <Separator className="bg-zinc-800/60" />

      <nav className="flex-1 overflow-y-auto px-3 py-4">
        <p className="mb-2 px-3 text-[10px] font-bold tracking-[0.2em] text-zinc-600">OPERAÇÃO</p>
        <ul className="mb-4 space-y-0.5">
          {operacaoLinks.map((link) => (
            <li key={link.label}>
              <button
                type="button"
                className={`group flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-all ${
                  link.active
                    ? 'border-l-2 border-red-600 bg-zinc-800/60 text-red-500'
                    : 'text-zinc-400 hover:bg-zinc-800/40 hover:text-zinc-200'
                }`}
              >
                <link.icon
                  className={`h-4 w-4 ${link.active ? 'text-red-500' : 'text-zinc-500'}`}
                />
                <span>{link.label}</span>
                {link.badge && (
                  <Badge className="ml-auto h-5 min-w-5 justify-center rounded-full bg-red-600/20 px-1.5 text-[10px] text-red-400">
                    {link.badge}
                  </Badge>
                )}
              </button>
            </li>
          ))}
        </ul>

        <p className="mb-2 px-3 text-[10px] font-bold tracking-[0.2em] text-zinc-600">GESTÃO</p>
        <ul className="mb-4 space-y-0.5">
          {gestaoLinks.map((link) => (
            <li key={link.label}>
              <button
                type="button"
                className="group flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-zinc-400 transition-all hover:bg-zinc-800/40 hover:text-zinc-200"
              >
                <link.icon className="h-4 w-4 text-zinc-500" />
                <span>{link.label}</span>
              </button>
            </li>
          ))}
        </ul>

        <p className="mb-2 px-3 text-[10px] font-bold tracking-[0.2em] text-zinc-600">SISTEMA</p>
        <ul className="space-y-0.5">
          {sistemaLinks.map((link) => (
            <li key={link.label}>
              <button
                type="button"
                className="group flex w-full items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-zinc-400 transition-all hover:bg-zinc-800/40 hover:text-zinc-200"
              >
                <link.icon className="h-4 w-4 text-zinc-500" />
                <span>{link.label}</span>
              </button>
            </li>
          ))}
        </ul>
      </nav>

      <Separator className="bg-zinc-800/60" />

      <div className="flex items-center gap-3 px-5 py-4">
        <Avatar>
          <AvatarFallback className="bg-zinc-800 text-xs font-semibold text-zinc-300">
            LA
          </AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <p className="truncate text-sm font-medium text-zinc-200">Lucas Arruda</p>
          <p className="text-[10px] font-semibold tracking-widest text-zinc-500">GERENTE</p>
        </div>
      </div>
    </aside>
  );
}
