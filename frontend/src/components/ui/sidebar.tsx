import {
  type ButtonHTMLAttributes,
  type HTMLAttributes,
  createContext,
  forwardRef,
  useCallback,
  useContext,
  useMemo,
  useState,
} from 'react';
import { Slot } from '@radix-ui/react-slot';
import styles from './sidebar.module.css';

type SidebarContextValue = {
  isCollapsed: boolean;
  setIsCollapsed: (value: boolean) => void;
  isMobileOpen: boolean;
  setIsMobileOpen: (value: boolean) => void;
  toggleCollapsed: () => void;
};

const SidebarContext = createContext<SidebarContextValue | null>(null);

type SidebarProviderProps = {
  children: React.ReactNode;
  defaultCollapsed?: boolean;
};

export const SidebarProvider = ({ children, defaultCollapsed = false }: SidebarProviderProps) => {
  const [isCollapsed, setIsCollapsed] = useState(defaultCollapsed);
  const [isMobileOpen, setIsMobileOpen] = useState(false);

  const toggleCollapsed = useCallback(() => {
    setIsCollapsed((previousValue) => !previousValue);
  }, []);

  const value = useMemo(
    () => ({
      isCollapsed,
      setIsCollapsed,
      isMobileOpen,
      setIsMobileOpen,
      toggleCollapsed,
    }),
    [isCollapsed, isMobileOpen, toggleCollapsed],
  );

  return <SidebarContext.Provider value={value}>{children}</SidebarContext.Provider>;
};

export const useSidebar = () => {
  const context = useContext(SidebarContext);

  if (!context) {
    throw new Error('useSidebar must be used within a SidebarProvider');
  }

  return context;
};

type SidebarProps = HTMLAttributes<HTMLElement>;

export const Sidebar = ({ children, className = '', ...props }: SidebarProps) => {
  const { isCollapsed, isMobileOpen, setIsMobileOpen } = useSidebar();
  const sidebarClassName = [styles.sidebar, className].filter(Boolean).join(' ');

  return (
    <>
      <button
        aria-label="Fechar menu"
        className={styles.backdrop}
        data-open={isMobileOpen}
        onClick={() => setIsMobileOpen(false)}
        type="button"
      />
      <aside
        className={sidebarClassName}
        data-collapsed={isCollapsed}
        data-mobile-open={isMobileOpen}
        {...props}
      >
        {children}
      </aside>
    </>
  );
};

type SidebarTriggerProps = ButtonHTMLAttributes<HTMLButtonElement>;

export const SidebarTrigger = forwardRef<HTMLButtonElement, SidebarTriggerProps>(
  ({ className = '', onClick, ...props }, ref) => {
    const { isMobileOpen, setIsMobileOpen } = useSidebar();

    const handleClick: React.MouseEventHandler<HTMLButtonElement> = (event) => {
      onClick?.(event);
      if (!event.defaultPrevented) {
        setIsMobileOpen(!isMobileOpen);
      }
    };

    return (
      <button
        className={[styles.trigger, className].join(' ').trim()}
        onClick={handleClick}
        ref={ref}
        type="button"
        {...props}
      />
    );
  },
);

SidebarTrigger.displayName = 'SidebarTrigger';

export const SidebarHeader = ({
  children,
  className = '',
  ...props
}: HTMLAttributes<HTMLDivElement>) => (
  <header className={[styles.header, className].join(' ').trim()} {...props}>
    {children}
  </header>
);

export const SidebarContent = ({
  children,
  className = '',
  ...props
}: HTMLAttributes<HTMLDivElement>) => (
  <div className={[styles.content, className].join(' ').trim()} {...props}>
    {children}
  </div>
);

export const SidebarFooter = ({
  children,
  className = '',
  ...props
}: HTMLAttributes<HTMLDivElement>) => (
  <footer className={[styles.footer, className].join(' ').trim()} {...props}>
    {children}
  </footer>
);

export const SidebarGroup = ({
  children,
  className = '',
  ...props
}: HTMLAttributes<HTMLElement>) => (
  <section className={[styles.group, className].join(' ').trim()} {...props}>
    {children}
  </section>
);

export const SidebarGroupLabel = ({
  children,
  className = '',
  ...props
}: HTMLAttributes<HTMLHeadingElement>) => (
  <h2 className={[styles.groupLabel, className].join(' ').trim()} {...props}>
    {children}
  </h2>
);

export const SidebarMenu = ({
  children,
  className = '',
  ...props
}: HTMLAttributes<HTMLUListElement>) => (
  <ul className={[styles.menu, className].join(' ').trim()} {...props}>
    {children}
  </ul>
);

export const SidebarMenuItem = ({
  children,
  className = '',
  ...props
}: HTMLAttributes<HTMLLIElement>) => (
  <li className={[styles.menuItem, className].join(' ').trim()} {...props}>
    {children}
  </li>
);

type SidebarMenuButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  asChild?: boolean;
  isActive?: boolean;
};

export const SidebarMenuButton = forwardRef<HTMLButtonElement, SidebarMenuButtonProps>(
  ({ asChild = false, className = '', isActive = false, ...props }, ref) => {
    const Comp = asChild ? Slot : 'button';

    return (
      <Comp
        className={[styles.menuButton, className].join(' ').trim()}
        data-active={isActive}
        ref={ref}
        {...props}
      />
    );
  },
);

SidebarMenuButton.displayName = 'SidebarMenuButton';

export const SidebarMenuBadge = ({
  children,
  className = '',
  ...props
}: HTMLAttributes<HTMLSpanElement>) => (
  <span className={[styles.menuBadge, className].join(' ').trim()} {...props}>
    {children}
  </span>
);
