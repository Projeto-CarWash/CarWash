## Por quê
A tela de detalhe de cliente apresentava vazamento visual de estado (dados antigos apareciam por um instante ao trocar de cliente). Além disso, as requisições de veículos não eram isoladas do restante dos dados do cliente, causando falha geral na página se o endpoint de veículos falhasse (403 permissão ou 500), e a tela global de 403/404 não estava implementada no padrão correto do projeto.

## O que muda
- `ClienteDetalhePage.tsx`:
  - Reset imediato do estado local (`cliente` e `veiculos`) ao alterar o `clienteId` na URL para evitar "visual data leaks".
  - Isolamento do fetch de veículos em bloco separado do fetch do cliente com tratamento de erros granular:
    * Erro 401: redireciona para `/login`.
    * Erro 403 (permissão): oculta o bloco e exibe uma mensagem contextual de falta de privilégios.
    * Erro 500/Rede: mostra mensagem com botão "Tentar novamente" (ícone `RefreshCw`).
  - Adicionado estado vazio (empty state) amigável com botão (CTA) para cadastrar o primeiro veículo do cliente.
  - Implementação de tela cheia global de erro para 404 (Não Encontrado) e 403 (Proibido) com ícone `ShieldOff` e botão de retorno.
  - Atualização automática dos veículos cadastrados caso a rota de retorno venha com o state `{ veiculoCriado: true }`.
  - Exibição de spinner de loading (`Loader2`) no botão de ativar/desativar cliente durante a requisição.

## Como testei
- [ ] `dotnet test` passou (back)
- [x] `npm run check` passou (front)
- [ ] `make smoke ENV=dev` passou (infra)
- [x] Teste de negócio para CA022 adicionado/atualizado

## Rastreabilidade
- **Problema (DVP-E §2.2):** P_RF022
- **Requisitos (DRP §3):** RF022 (Visualizar Detalhes do Cliente e Veículos)
- **Regras (DRP §4):** RN018, RN019
- **Critérios de aceite (DRP §10):** CA022
- **Módulo (DAT §4.1):** Frontend
- **Risco mitigado/criado (DVS §4 / DAT §10):** RV022 / RAT022

## Checklist de DoD
- [x] Conventional Commits respeitado em todos os commits e no título do PR
- [x] Branch atualizada com `main` via rebase (sem merge commits)
- [x] CI verde (lint, typecheck, build, test, docker validate)
- [x] Cobertura de testes para regra crítica (CA011) quando aplicável
- [ ] Migration EF Core revisada (se houver) – script SQL gerado e lido
- [ ] Logs estruturados em pontos críticos (RNF009)
- [x] Sem segredo, `.env`, certificado ou dump de banco no diff
- [x] Documentação afetada atualizada (`docs/`, `CONTRIBUTING.md`, `README.md`, comentários)
- [x] Sem `--no-verify` no caminho

## Riscos / impactos
Essa modificação altera o comportamento da tela de detalhes de clientes. Risco de impacto médio, mitigado pelo tratamento individualizado e isolado de erros por blocos lógicos.

## Notas para o reviewer
A lógica de recarregamento automático por navegação usa o histórico de rotas do React Router (`location.state.veiculoCriado`).
