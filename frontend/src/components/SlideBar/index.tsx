import {
  CalendarDays,
  CarFront,
  ChartNoAxesColumn,
  Cog,
  DollarSign,
  Gauge,
  Menu,
  Settings,
  Users,
  Wrench,
  type LucideIcon,
} from 'lucide-react';
import { Link, useLocation } from 'react-router-dom';

import {
  Sidebar,
  SidebarContent,
  SidebarFooter,
  SidebarGroup,
  SidebarGroupLabel,
  SidebarHeader,
  SidebarMenu,
  SidebarMenuBadge,
  SidebarMenuButton,
  SidebarMenuItem,
  SidebarProvider,
  SidebarTrigger,
} from '../ui/sidebar.tsx';

import styles from './slidebar.module.css';

interface MenuItem {
  label: string;
  href: string;
  icon: LucideIcon;
  badge?: number;
}

interface MenuSection {
  title: string;
  items: MenuItem[];
}

const sections: MenuSection[] = [
  {
    title: 'Operacao',
    items: [
      { label: 'Painel', href: '/painel', icon: Gauge, badge: 12 },
      { label: 'Servicos', href: '/servicos', icon: Wrench },
      { label: 'Clientes', href: '/clientes', icon: Users },
      { label: 'Veiculos', href: '/veiculos', icon: CarFront },
    ],
  },
  {
    title: 'Gestao',
    items: [
      { label: 'Agendamentos', href: '/agendamentos', icon: CalendarDays },
      { label: 'Financeiro', href: '/financeiro', icon: DollarSign },
      { label: 'Relatorios', href: '/relatorios', icon: ChartNoAxesColumn },
      { label: 'Equipe', href: '/equipe', icon: Users },
    ],
  },
  {
    title: 'Sistema',
    items: [{ label: 'Configuracoes', href: '/configuracoes', icon: Settings }],
  },
];

const SidebarBody = () => {
  const location = useLocation();

  const isPathActive = (href: string) => {
    if (href === '/painel') {
      return location.pathname === '/' || location.pathname === href;
    }

    return location.pathname === href || location.pathname.startsWith(`${href}/`);
  };

  return (
    <Sidebar className={styles.sidebar}>
      <SidebarHeader className={styles.header}>
        <div className={styles.headerTopRow}>
          <SidebarTrigger aria-label="Abrir menu" className={styles.mobileTrigger}>
            <Menu size={18} strokeWidth={1.8} />
          </SidebarTrigger>
        </div>

        <Link aria-label="Ir para Painel" className={styles.brandLink} to="/painel">
          <div className={styles.logoFrame}>
            <CarFront aria-hidden="true" className={styles.logoIcon} size={20} strokeWidth={1.9} />
          </div>
          <div className={styles.brandTextWrapper}>
            <strong className={styles.brandTitle}>
              CAR<span className={styles.brandTitleHighlight}>WASH</span>
            </strong>
            <small className={styles.version}>Admin · v2.4</small>
          </div>
        </Link>
      </SidebarHeader>

      <SidebarContent className={styles.content}>
        {sections.map((section) => (
          <SidebarGroup aria-label={section.title} className={styles.group} key={section.title}>
            <SidebarGroupLabel className={styles.groupTitle}>{section.title}</SidebarGroupLabel>
            <SidebarMenu className={styles.menuList}>
              {section.items.map((item) => {
                const Icon = item.icon;
                const isActive = isPathActive(item.href);

                return (
                  <SidebarMenuItem className={styles.menuItem} key={item.href}>
                    <SidebarMenuButton asChild className={styles.menuButton} isActive={isActive}>
                      <Link
                        aria-current={isActive ? 'page' : undefined}
                        className={styles.menuLink}
                        to={item.href}
                      >
                        <Icon
                          aria-hidden="true"
                          className={styles.menuIcon}
                          size={18}
                          strokeWidth={1.8}
                        />
                        <span className={styles.menuText}>{item.label}</span>
                        {item.badge ? (
                          <SidebarMenuBadge className={styles.badge}>{item.badge}</SidebarMenuBadge>
                        ) : null}
                      </Link>
                    </SidebarMenuButton>
                  </SidebarMenuItem>
                );
              })}
            </SidebarMenu>
          </SidebarGroup>
        ))}
      </SidebarContent>

      <SidebarFooter className={styles.footer}>
        <button className={styles.profileButton} type="button">
          <span aria-hidden="true" className={styles.avatar}>
            RM
          </span>
          <span className={styles.profileMeta}>
            <strong className={styles.profileName}>Ricardo Motta</strong>
            <span className={styles.profileRole}>Gerente</span>
          </span>
          <Cog aria-hidden="true" className={styles.profileIcon} size={16} strokeWidth={1.8} />
        </button>
      </SidebarFooter>
    </Sidebar>
  );
};

export const SlideBar = () => (
  <SidebarProvider>
    <SidebarBody />
  </SidebarProvider>
);

export default SlideBar;
