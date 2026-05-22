import { Navigate, Route, Routes } from 'react-router-dom';

import { NovoClientePage } from '@/components/clientes/NovoClientePage';
import { PageHeader } from '@/components/clientes/PageHeader';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import PrivateRoute from '@/components/PrivateRoute';
import { AuthProvider } from '@/contexts/AuthProvider';
import { AgendaPage } from '@/pages/Agenda/AgendaPage';
import { NovoAgendamentoPage } from '@/pages/Agendamentos/NovoAgendamentoPage';
import { ClienteDetalhePage } from '@/pages/Clientes/ClienteDetalhePage';
import { ClientesListaPage } from '@/pages/Clientes/ClientesListaPage';
import Dashboard from '@/pages/Dashboard/Dashboard';
import Login from '@/pages/Login/Login';
import { NovoUsuarioPage } from '@/pages/Usuarios/NovoUsuarioPage';
import { UsuarioDetalhePage } from '@/pages/Usuarios/UsuarioDetalhePage';
import { UsuariosListaPage } from '@/pages/Usuarios/UsuariosListaPage';

function App() {
  return (
    <AuthProvider>
      <Routes>
        <Route path="/login" element={<Login />} />

        <Route
          path="/dashboard"
          element={
            <PrivateRoute>
              <Dashboard />
            </PrivateRoute>
          }
        />

        <Route
          path="/clientes"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <ClientesListaPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/clientes/novo"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <PageHeader />
                <NovoClientePage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/clientes/:id"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <ClienteDetalhePage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/agenda"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <AgendaPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/agendamentos/novo"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <NovoAgendamentoPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/usuarios"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <UsuariosListaPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/usuarios/novo"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <NovoUsuarioPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/usuarios/:id"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <UsuarioDetalhePage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </AuthProvider>
  );
}

export default App;
