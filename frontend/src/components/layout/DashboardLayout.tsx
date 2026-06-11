import { BottomBar } from './BottomBar';
import { Sidebar } from './Sidebar';
import { Topbar } from './Topbar';

import type { ReactNode } from 'react';

interface DashboardLayoutProps {
  children: ReactNode;
}

export function DashboardLayout({ children }: DashboardLayoutProps) {
  return (
    <div className="flex h-screen overflow-hidden bg-zinc-950">
      <Sidebar />

      <div className="ml-64 flex flex-1 flex-col">
        <Topbar />
        <main className="flex-1 overflow-y-auto bg-zinc-950 pb-10">{children}</main>
        <BottomBar />
      </div>
    </div>
  );
}
