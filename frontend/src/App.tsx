import { Navigate, Route, Routes } from 'react-router-dom';

import { NovoAgendamentoPage } from '@/components/agendamentos/NovoAgendamentoPage';
import { NovoClientePage } from '@/components/clientes/NovoClientePage';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import PrivateRoute from '@/components/PrivateRoute';
import { AuthProvider } from '@/contexts/AuthProvider';
import { ClienteDetalhePage } from '@/pages/Clientes/ClienteDetalhePage';
import { ClientesListaPage } from '@/pages/Clientes/ClientesListaPage';
import { NovoVeiculoPage } from '@/pages/Clientes/NovoVeiculoPage';
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
          path="/clientes/:id/veiculos/novo"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <NovoVeiculoPage />
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

        <Route
          path="/agendamentos"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <AgendamentosDashboardPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/agendamentos/calendario"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <AgendamentosCalendarioPage />
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
