import { BrowserRouter, Navigate, Route, Routes, useLocation } from 'react-router-dom';
import SlideBar from '@/components/SlideBar';
import styles from './app.module.css';

const routeTitles: Record<string, string> = {
  '/painel': 'Painel',
  '/servicos': 'Servicos',
  '/clientes': 'Clientes',
  '/veiculos': 'Veiculos',
  '/agendamentos': 'Agendamentos',
  '/financeiro': 'Financeiro',
  '/relatorios': 'Relatorios',
  '/equipe': 'Equipe',
  '/configuracoes': 'Configuracoes',
};

const ScreenContent = () => {
  const location = useLocation();
  const title = routeTitles[location.pathname] ?? 'Painel';

  return (
    <main className={styles.content}>
      <section className={styles.contentCard}>
        <h1 className={styles.contentTitle}>{title}</h1>
        <p className={styles.contentSubtitle}>
          Estrutura pronta para integrar os modulos funcionais do CarWash com navegacao lateral
          premium, responsiva e acessivel.
        </p>
      </section>
    </main>
  );
};

const AppRoutes = () => (
  <div className={styles.appShell}>
    <SlideBar />
    <Routes>
      <Route element={<ScreenContent />} path="/painel" />
      <Route element={<ScreenContent />} path="/servicos" />
      <Route element={<ScreenContent />} path="/clientes" />
      <Route element={<ScreenContent />} path="/veiculos" />
      <Route element={<ScreenContent />} path="/agendamentos" />
      <Route element={<ScreenContent />} path="/financeiro" />
      <Route element={<ScreenContent />} path="/relatorios" />
      <Route element={<ScreenContent />} path="/equipe" />
      <Route element={<ScreenContent />} path="/configuracoes" />
      <Route element={<Navigate replace to="/painel" />} path="*" />
    </Routes>
  </div>
);

const App = () => (
  <BrowserRouter>
    <AppRoutes />
  </BrowserRouter>
);

export default App;
