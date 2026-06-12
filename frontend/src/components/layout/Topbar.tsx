import { Bell, Plus, Search } from 'lucide-react';
import { Fragment } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';

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
  const location = useLocation();
  const navigate = useNavigate();
  const segments = location.pathname.split('/').filter(Boolean);

  function getBreadcrumbName(segment: string, index: number) {
    if (segment === 'dashboard') return 'Dashboard';
    if (segment === 'clientes') return 'Clientes';
    if (segment === 'usuarios') return 'Usuários';
    if (segment === 'agendamentos') return 'Agendamentos';
    if (segment === 'novo') return 'Novo';

    if (index === segments.length - 1 && segment !== 'novo') {
      return 'Detalhes';
    }

    return segment.charAt(0).toUpperCase() + segment.slice(1);
  }

  const breadcrumbs = segments.map((seg, i) => {
    const isLast = i === segments.length - 1;
    const name = getBreadcrumbName(seg, i);
    const href = '/' + segments.slice(0, i + 1).join('/');
    return { name, href, isLast };
  });

  return (
    <header className="flex items-center justify-between border-b border-border bg-background/80 px-6 py-3 backdrop-blur-sm">
      <Breadcrumb>
        <BreadcrumbList className="text-xs font-bold tracking-[0.18em] uppercase">
          <BreadcrumbItem>
            <BreadcrumbLink asChild className="text-muted-foreground hover:text-foreground">
              <Link to="/dashboard">Admin</Link>
            </BreadcrumbLink>
          </BreadcrumbItem>
          {breadcrumbs.map((crumb) => (
            <Fragment key={crumb.href}>
              <BreadcrumbSeparator className="text-muted-foreground">/</BreadcrumbSeparator>
              <BreadcrumbItem>
                {crumb.isLast ? (
                  <BreadcrumbPage className="text-foreground">{crumb.name}</BreadcrumbPage>
                ) : (
                  <BreadcrumbLink asChild className="text-muted-foreground hover:text-foreground">
                    <Link to={crumb.href}>{crumb.name}</Link>
                  </BreadcrumbLink>
                )}
              </BreadcrumbItem>
            </Fragment>
          ))}
        </BreadcrumbList>
      </Breadcrumb>

      <div className="flex items-center gap-3">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            type="search"
            placeholder="Buscar placa, cliente ou OS..."
            className="h-9 w-72 rounded-full border-border bg-card pl-9 pr-14 text-sm text-foreground placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-0"
            aria-label="Buscar"
          />
          <kbd className="absolute right-3 top-1/2 -translate-y-1/2 rounded border border-border bg-muted px-1.5 py-0.5 text-[10px] font-semibold text-muted-foreground">
            ⌘K
          </kbd>
        </div>

        <button
          type="button"
          className="relative flex h-9 w-9 items-center justify-center rounded-full border border-border bg-card text-muted-foreground transition-colors hover:border-ring hover:text-foreground"
          aria-label="Notificações"
        >
          <Bell className="h-4 w-4" />
          <span className="absolute right-2 top-2 h-2 w-2 rounded-full bg-red-500 shadow-[0_0_0_3px_rgba(239,68,68,0.2)]" />
        </button>

        <Button
          onClick={() => void navigate('/agendamentos/novo')}
          className="h-9 rounded-full bg-red-600 px-4 text-sm font-semibold text-white shadow-lg shadow-red-600/25 hover:bg-red-700"
        >
          <Plus className="mr-1 h-4 w-4" />
          Novo agendamento
        </Button>
      </div>
    </header>
  );
}
