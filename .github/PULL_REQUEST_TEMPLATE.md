<!--
Título do PR = Conventional Commit (será a mensagem de squash em main)
Ex: feat(back): adiciona constraint UNIQUE global em Agendamento (RF020)
-->

## Por quê
<!-- Motivação de negócio. Cite problema (P1–P7) e/ou objetivo do RF/CA. -->

## O que muda
<!-- Resumo técnico do que foi alterado. Sem diff — quem revisa lê o diff. -->

## Como testei
<!-- Manual + automatizado. Cole comando(s) e/ou print(s) quando útil. -->
- [ ] `dotnet test` passou (back)
- [ ] `npm run check` passou (front)
- [ ] `make smoke ENV=dev` passou (infra)
- [ ] Teste de negócio para CA0XX adicionado/atualizado

## Rastreabilidade
- **Problema (DVP-E §2.2):** P_
- **Requisitos (DRP §3):** RF0__
- **Regras (DRP §4):** RN0__
- **Critérios de aceite (DRP §10):** CA0__
- **Módulo (DAT §4.1):** _
- **Risco mitigado/criado (DVS §4 / DAT §10):** RV0__ / RAT0__

## Checklist de DoD
- [ ] Conventional Commits respeitado em todos os commits e no título do PR
- [ ] Branch atualizada com `main` via rebase (sem merge commits)
- [ ] CI verde (lint, typecheck, build, test, docker validate)
- [ ] Cobertura de testes para regra crítica (CA011) quando aplicável
- [ ] Migration EF Core revisada (se houver) — script SQL gerado e lido
- [ ] Logs estruturados em pontos críticos (RNF009)
- [ ] Sem segredo, `.env`, certificado ou dump de banco no diff
- [ ] Documentação afetada atualizada (`docs/`, `CONTRIBUTING.md`, `README.md`, comentários)
- [ ] Sem `--no-verify` no caminho

## Riscos / impactos
<!-- O que pode quebrar? Plano de rollback? Breaking change? -->

## Notas para o reviewer
<!-- Atalhos, contexto extra, áreas que merecem atenção. Opcional. -->
