import { Navigate, Route, Routes } from 'react-router-dom';

import { NovoAgendamentoPage } from '@/components/agendamentos/NovoAgendamentoPage';
import { NovoClientePage } from '@/components/clientes/NovoClientePage';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import PrivateRoute from '@/components/PrivateRoute';
import { AuthProvider } from '@/contexts/AuthProvider';
import { AgendamentosCalendarioPage } from '@/pages/Agendamentos/AgendamentosCalendarioPage';
import { AgendamentosDashboardPage } from '@/pages/Agendamentos/AgendamentosDashboardPage';
import { EditarAgendamentoPage } from '@/pages/Agendamentos/EditarAgendamentoPage';
import { ClienteDetalhePage } from '@/pages/Clientes/ClienteDetalhePage';
import { ClientesListaPage } from '@/pages/Clientes/ClientesListaPage';
import { EditarClientePage } from '@/pages/Clientes/EditarClientePage';
import { NovoVeiculoPage } from '@/pages/Clientes/NovoVeiculoPage';
import Dashboard from '@/pages/Dashboard/Dashboard';
import { FiliaisListaPage } from '@/pages/Filiais/FiliaisListaPage';
import { FilialEditarPage } from '@/pages/Filiais/FilialEditarPage';
import { FilialFormPage } from '@/pages/Filiais/FilialFormPage';
import { FinanceiroPage } from '@/pages/Financeiro/FinanceiroPage';
import Login from '@/pages/Login/Login';
import { RelatoriosPage } from '@/pages/Relatorios/RelatoriosPage';
import { ServicoFormPage } from '@/pages/Servicos/ServicoFormPage';
import { ServicosListaPage } from '@/pages/Servicos/ServicosListaPage';
import { NovoUsuarioPage } from '@/pages/Usuarios/NovoUsuarioPage';
import { UsuarioDetalhePage } from '@/pages/Usuarios/UsuarioDetalhePage';
import { UsuariosListaPage } from '@/pages/Usuarios/UsuariosListaPage';
import { VeiculosListaPage } from '@/pages/Veiculos/VeiculosListaPage';

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
          path="/clientes/:id/editar"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <EditarClientePage />
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
          path="/veiculos"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <VeiculosListaPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/veiculos/novo"
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
          path="/servicos"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <ServicosListaPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/servicos/novo"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <ServicoFormPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/servicos/:id/editar"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <ServicoFormPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/filiais"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <FiliaisListaPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/filiais/nova"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <FilialFormPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/filiais/:id/editar"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <FilialEditarPage />
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
          path="/agendamentos/:id/editar"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <EditarAgendamentoPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/relatorios"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <RelatoriosPage />
              </DashboardLayout>
            </PrivateRoute>
          }
        />

        <Route
          path="/financeiro"
          element={
            <PrivateRoute>
              <DashboardLayout>
                <FinanceiroPage />
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
