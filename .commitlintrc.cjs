// commitlint — valida mensagens de commit conforme CONTRIBUTING.md §2
// Roda no CI via job "commit-conventions" (.github/workflows/ci.yml).
module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    // Tipos permitidos no projeto — espelha CONTRIBUTING.md §2.
    'type-enum': [
      2,
      'always',
      ['feat', 'fix', 'docs', 'style', 'refactor', 'test', 'chore', 'build', 'ci', 'perf', 'revert'],
    ],
    // Escopos livres — o time já usa muitos (back, front, infra, cliente, auth, usuarios,
    // routing, tokens, api, ui, db, ci, agents, deps, RF014, etc.). Lista fechada quebraria
    // o histórico atual sem ganho real.
    'scope-empty': [0],
    'scope-enum': [0],
    // Permite caixa mista no assunto (ex.: "RF003 validações", "RF001 — tela de login").
    'subject-case': [0],
    // CONTRIBUTING diz ≤ 72 chars, mas mensagens de squash (com "(#NN)" anexado pelo GitHub)
    // extrapolam fácil. 100 é tolerante o suficiente sem perder a intenção de assuntos curtos.
    'header-max-length': [2, 'always', 100],
    'subject-full-stop': [2, 'never', '.'],
    'body-leading-blank': [2, 'always'],
    'footer-leading-blank': [2, 'always'],
  },
};
