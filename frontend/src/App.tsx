import { Navigate, Route, Routes } from 'react-router-dom';

import { NovoClientePage } from '@/components/clientes/NovoClientePage';
import { PageHeader } from '@/components/clientes/PageHeader';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import PrivateRoute from '@/components/PrivateRoute';
import { AuthProvider } from '@/contexts/AuthProvider';
import Dashboard from '@/pages/Dashboard/Dashboard';
import Login from '@/pages/Login/Login';

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

        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    </AuthProvider>
  );
}

export default App;
