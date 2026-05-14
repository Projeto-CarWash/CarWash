export const App = () => (
  <main className="app-shell">
    <section className="hero-card" aria-labelledby="app-title">
      <p className="eyebrow">CarWash</p>
      <h1 id="app-title">A aplicação foi carregada com sucesso.</h1>
      <p className="lead">
        O ponto de entrada agora renderiza um componente real em vez de um placeholder vazio.
      </p>

      <div className="status-grid" aria-label="Resumo da interface inicial">
        <article>
          <span className="status-label">Frontend</span>
          <strong>React + Vite</strong>
        </article>
        <article>
          <span className="status-label">Tema</span>
          <strong>Carregado via ThemeProvider</strong>
        </article>
        <article>
          <span className="status-label">Estado</span>
          <strong>Tela inicial visível</strong>
        </article>
      </div>
    </section>
  </main>
);
