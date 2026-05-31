## Por quê
O formulário de adição de novos veículos a um cliente existente (`NovoVeiculoPage.tsx`) possuía problemas na lógica de desabilitar o botão de envio (ficava travado incorretamente antes de qualquer tentativa) e faltava exibir um toast de sucesso claro no canto superior direito ao salvar, além de melhor tratamento para erros 409 (placa em uso), 401, 403 e 422.

## O que muda
- `NovoVeiculoPage.tsx`:
  - Corrigida lógica de `isSubmitDisabled`: agora só bloqueia o botão durante o carregamento (`isSubmitting`) ou se o formulário já foi submetido uma vez e possui erros ativos (`isSubmitted && !isValid`).
  - Implementado toast de sucesso fixado no topo superior direito com timeout automático após o salvamento bem-sucedido.
  - Implementado tratamento detalhado para erro 409 (placa já cadastrada): destaca visualmente o campo placa e exibe mensagem de erro customizada abaixo dele, mantendo os dados preenchidos para correção.
  - Adicionado tratamento para erro 401 (redirecionamento com mensagem), 403/422 (aviso de cliente inativo ou sem permissão) e 500 (mensagem genérica mantendo dados).
  - Configurado o redirecionamento pós-sucesso passando o estado `{ veiculoCriado: true }` para forçar o recarregamento da lista na tela de detalhes do cliente.

## Como testei
- [ ] `dotnet test` passou (back)
- [x] `npm run check` passou (front)
- [ ] `make smoke ENV=dev` passou (infra)
- [x] Teste de negócio para CA004 adicionado/atualizado

## Rastreabilidade
- **Problema (DVP-E §2.2):** P_RF004
- **Requisitos (DRP §3):** RF004 (Cadastrar Veículo para Cliente Existente)
- **Regras (DRP §4):** RN012, RN013
- **Critérios de aceite (DRP §10):** CA004
- **Módulo (DAT §4.1):** Frontend
- **Risco mitigado/criado (DVS §4 / DAT §10):** RV004 / RAT004

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
As modificações estão restritas ao fluxo de adição de veículos a clientes existentes. O risco de impactos colaterais é extremamente reduzido.

## Notas para o reviewer
A integração do redirecionamento se comunica perfeitamente com a tela de detalhes do cliente (`ClienteDetalhePage.tsx`), que escuta a propriedade `veiculoCriado` do estado de rota para disparar o refetch.
