import { NovoClientePage } from '@/components/clientes/NovoClientePage';
import { PageHeader } from '@/components/clientes/PageHeader';
import { DashboardLayout } from '@/components/layout/DashboardLayout';

function App() {
  return (
    <DashboardLayout>
      <PageHeader />
      <NovoClientePage />
    </DashboardLayout>
  );
}

export default App;
