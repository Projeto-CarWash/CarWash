import { Bell, Plus, Search } from 'lucide-react';

import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from '@/components/ui/breadcrumb';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

export function Topbar() {
  return (
    <header className="flex items-center justify-between border-b border-zinc-800/50 bg-zinc-950/80 px-6 py-3 backdrop-blur-sm">
      <Breadcrumb>
        <BreadcrumbList className="text-xs font-bold tracking-[0.18em] uppercase">
          <BreadcrumbItem>
            <BreadcrumbLink className="text-zinc-500 hover:text-zinc-300" href="#">
              Admin
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator className="text-zinc-600">/</BreadcrumbSeparator>
          <BreadcrumbItem>
            <BreadcrumbLink className="text-zinc-500 hover:text-zinc-300" href="#">
              Clientes
            </BreadcrumbLink>
          </BreadcrumbItem>
          <BreadcrumbSeparator className="text-zinc-600">/</BreadcrumbSeparator>
          <BreadcrumbItem>
            <BreadcrumbPage className="text-zinc-300">Novo</BreadcrumbPage>
          </BreadcrumbItem>
        </BreadcrumbList>
      </Breadcrumb>

      <div className="flex items-center gap-3">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-500" />
          <Input
            type="search"
            placeholder="Buscar placa, cliente ou OS..."
            className="h-9 w-72 rounded-full border-zinc-700/60 bg-zinc-900/50 pl-9 pr-14 text-sm text-zinc-300 placeholder:text-zinc-500 focus-visible:border-zinc-600 focus-visible:ring-0"
            aria-label="Buscar"
          />
          <kbd className="absolute right-3 top-1/2 -translate-y-1/2 rounded border border-zinc-700 bg-zinc-800 px-1.5 py-0.5 text-[10px] font-semibold text-zinc-500">
            ⌘K
          </kbd>
        </div>

        <button
          type="button"
          className="relative flex h-9 w-9 items-center justify-center rounded-full border border-zinc-700/60 bg-zinc-900/50 text-zinc-400 transition-colors hover:border-zinc-600 hover:text-zinc-200"
          aria-label="Notificações"
        >
          <Bell className="h-4 w-4" />
          <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-red-500 shadow-[0_0_0_3px_rgba(239,68,68,0.2)]" />
        </button>

        <Button className="h-9 rounded-full bg-red-600 px-4 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700">
          <Plus className="mr-1 h-4 w-4" />
          Novo agendamento
        </Button>
      </div>
    </header>
  );
}
