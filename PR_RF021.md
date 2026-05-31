## Por quê
O fluxo de cadastro de novo cliente possuía lacunas no tratamento de erros da API, permitindo o envio do formulário sem veículos associados (violando regras do negócio), além de não validar a placa do veículo localmente nem permitir a remoção correta de veículos adicionados temporariamente no formulário.

## O que muda
- `NovoClientePage.tsx`:
  - Adicionado tratamento granular para erros HTTP (409 para conflito de placa vs documento, 400 para erros de campo, 401 para redirecionamento e 500 para toasts de rede/sistema).
  - Bloqueio de submissão do formulário caso a lista de veículos esteja vazia (`clienteSchema.veiculos.min(1)`).
  - Preservação dos dados preenchidos no formulário caso ocorra falha no envio.
- `VeiculosClienteForm.tsx`:
  - Adicionada validação local via Zod para o formato da placa (padrões antigo AAA0000 e Mercosul AAA0A00) e campos obrigatórios.
  - Bloqueio da adição de veículos com placas duplicadas na listagem local.
  - Correção na exclusão de veículos locais utilizando a função `remove` do `useFieldArray`.
  - Limpeza automática de mensagens de erro ao adicionar com sucesso.

## Como testei
- [ ] `dotnet test` passou (back)
- [x] `npm run check` passou (front)
- [ ] `make smoke ENV=dev` passou (infra)
- [x] Teste de negócio para CA021 adicionado/atualizado

## Rastreabilidade
- **Problema (DVP-E §2.2):** P_RF021
- **Requisitos (DRP §3):** RF021 (Cadastrar Cliente com Veículo)
- **Regras (DRP §4):** RN015, RN016
- **Critérios de aceite (DRP §10):** CA021
- **Módulo (DAT §4.1):** Frontend
- **Risco mitigado/criado (DVS §4 / DAT §10):** RV021 / RAT021

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
As mudanças afetam diretamente a tela de criação de novos clientes e a validação de formato de placa. Risco baixo, pois as validações são síncronas e locais.

## Notas para o reviewer
A validação de placa suporta letras maiúsculas/minúsculas e faz a conversão automática para maiúsculas na interface.
