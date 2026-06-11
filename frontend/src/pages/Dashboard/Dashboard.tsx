import { LayoutDashboard, LogOut } from 'lucide-react';
import { useCallback } from 'react';
import { useNavigate } from 'react-router-dom';

import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/hooks/useAuth';

export default function Dashboard() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = useCallback(async () => {
    await logout();
    void navigate('/login', { replace: true });
  }, [logout, navigate]);

  return (
    <DashboardLayout>
      <div className="px-8 py-8">
        <Card className="border border-border bg-card">
          <CardHeader>
            <div className="flex items-start justify-between gap-4">
              <div className="flex items-center gap-3">
                <span
                  className="flex h-10 w-10 items-center justify-center rounded-lg bg-red-600/10 text-red-500"
                  aria-hidden="true"
                >
                  <LayoutDashboard className="h-5 w-5" />
                </span>
                <div>
                  <CardTitle className="text-lg text-foreground">
                    Bem-vindo{user?.nome ? `, ${user.nome}` : ''}
                  </CardTitle>
                  <CardDescription className="text-muted-foreground">
                    Perfil: {user?.perfil ?? '—'}
                  </CardDescription>
                </div>
              </div>

              <Button
                type="button"
                variant="outline"
                onClick={handleLogout}
                className="h-9 rounded-full border-border bg-transparent px-4 text-sm text-foreground hover:bg-accent hover:text-foreground"
              >
                <LogOut className="mr-1 h-4 w-4" aria-hidden="true" />
                Sair
              </Button>
            </div>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Login realizado com sucesso. Os módulos de gestão (clientes, veículos, agenda) serão
              integrados nesta área conforme avançamos no roadmap.
            </p>
          </CardContent>
        </Card>
      </div>
    </DashboardLayout>
  );
}
