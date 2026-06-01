## Por quê
A listagem de serviços do frontend estava usando URLs que não continham o prefixo `/api/v1`, gerando incompatibilidade com as rotas reais da API e quebrando a exibição da listagem de serviços. Os mocks MSW também precisavam de atualização para refletir os endpoints corretos de alteração de preço e status de serviços.

## O que muda
- `servicoService.ts`: Adicionado o prefixo `/api/v1` aos endpoints de listagem, cadastro e atualização de preço/status dos serviços.
- `src/mocks/handlers.ts`: Atualizados os endpoints fictícios do browser para incluir `/api/v1`.
- `src/test/handlers.ts`: Adicionados os mocks para requisições do tipo `PATCH` (preço e status) e corrigido o campo `precoBase` para `preco` alinhado à API.
- `ServicosListaPage.test.tsx`: Criados 6 testes de integração testando a exibição da listagem, cenários de erro 500, lista vazia, ativação/desativação de serviços e modal de confirmação.

## Como testei
- [ ] `dotnet test` passou (back)
- [x] `npm run check` passou (front)
- [ ] `make smoke ENV=dev` passou (infra)
- [x] Teste de negócio para CA011 e CA012 adicionado/atualizado

## Rastreabilidade
- **Problema (DVP-E §2.2):** P_RF006
- **Requisitos (DRP §3):** RF006 (Manter Serviços)
- **Regras (DRP §4):** RN002, RN003
- **Critérios de aceite (DRP §10):** CA011, CA012
- **Módulo (DAT §4.1):** Frontend
- **Risco mitigado/criado (DVS §4 / DAT §10):** RV006 / RAT006

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
Alterações restritas à listagem de serviços do frontend e suas respectivas chamadas de API. Risco baixo de regressão em outros módulos.

## Notas para o reviewer
Todos os testes foram validados localmente através do Vitest e estão passando com 100% de cobertura no arquivo de teste criado.
