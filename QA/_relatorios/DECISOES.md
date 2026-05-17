# Decisões pendentes — resolvidas com base na documentação

Data: 2026-05-17
Documentos consultados: `docs/drp`, `docs/dvp-e`, `docs/dvs`, `docs/dat`.

---

## 1. BUG-TENANT-CLI — NÃO É BUG ✅

**Sintoma observado**: `Funcionario` de uma filial lê cliente criado por `Admin` de outra. Sem `tenant_id` em `clientes`.

**Decisão**: **PASS — comportamento esperado pela documentação**.

**Justificativa**:
- DRP linha 87: *"Na versão atual, administrador e funcionário possuem o mesmo nível de permissões. A diferenciação de acesso fica para versão futura."*
- DRP RF-FUT003: "Diferenciação de permissões por perfil de usuário" (versão futura).
- DRP RN001: sistema é uso interno do estabelecimento.
- Cliente é entidade global; filial é apenas contexto operacional de **agendamento** (RN010, RN011), não de cliente.
- RN011 inclusive obriga conflito de veículo *entre* filiais — confirma que dados de cliente/veículo são compartilhados.

**Ação**: marcar T8 byid (Clientes Read) como **PASS** no próximo SUMARIO. Bug encerrado como "comportamento documentado".

---

## 2. BUG-LGPD-CLI — NÃO É BUG NO MVP ✅

**Sintoma observado**: CPF/CNPJ/email/celular/dataNascimento/endereço em claro nas respostas.

**Decisão**: **PASS — comportamento esperado para sistema interno**.

**Justificativa**:
- DRP linha 74: "sistema interno para estabelecimentos de estética automotiva".
- DRP RN001: "sistema é de uso interno do estabelecimento e não oferece portal público".
- Nenhum requisito de mascaramento de PII na DRP/DVS/DVP-E/DAT (LGPD aparece apenas como termo no glossário do DVS).
- Funcionários precisam ver dados completos para atender o cliente (operação por telefone, identificação por documento, etc.).
- `Cache-Control: no-store` já implementado pelo dev (proteção contra cache intermediário).

**Ação**: marcar T17 listar e T10 byid (Clientes Read) como **PASS**. Bug encerrado como "uso interno autorizado". Auditoria de acesso a PII fica como melhoria de RNF futura (não bloqueia MVP).

---

## 3. BUG-POST-DATANASC-NULL — É BUG · FIX 1 ✅

**Sintoma observado**: POST cliente sem `dataNascimento` → 500 com stack visível.

**Decisão**: **fix técnico — validator `NotNull`**.

**Justificativa**:
- DRP §6 (linhas 207-215) lista campos obrigatórios do Cliente: Nome, CPF/CNPJ, Celular/Telefone, Endereço, Responsáveis. **`dataNascimento` não está listado como obrigatório na doc.**
- Entretanto, o schema atual após migration `RefatoraClienteEndereco` tem `data_nascimento date NOT NULL`. Já existe acordo implícito do time de adicionar como obrigatório (caso contrário a migration teria sido `NULL`).
- Mantendo compatibilidade com o schema, basta o validator retornar **400 estruturado** em vez de 500.

**Ação**: agente `dev-dotnet-carwash` adiciona `NotNull` no validator (Create + Update).

---

## 4. BUG-PUT-ATIVO-IGNORADO — É bug menor · FIX 2 ✅

**Sintoma observado**: PUT cliente com `{"ativo":false}` → 200 mas DB mantém `ativo=t`.

**Decisão**: **fix técnico — warning Serilog (consistência com `cpf`/`cnpj`)**.

**Justificativa**:
- Design correto: status de cliente muda via PATCH `/status` (endpoint dedicado, alinhado com PATCH `/usuarios/{id}/status`).
- PUT é update de cadastro, não de estado de ciclo de vida.
- `cpf`/`cnpj` no PUT também são ignorados, e o dev já implementou warning Serilog para esses (Opção B do BUG-CW-CLI-PUT-CPF). Aplicar o mesmo padrão para `ativo`.

**Ação**: agente `dev-dotnet-carwash` estende o warning para também detectar `ativo`.

---

## 5. BUG-CONTRATO-404-ROUTE — Inconsistência menor · FIX 3 ✅

**Sintoma observado**: `/clientes/abc` (id não-Guid) retorna 404 sem body; `/clientes/<guid inexistente>` retorna 404 com ProblemDetails.

**Decisão**: **fix técnico — padronizar ProblemDetails sempre**.

**Justificativa**:
- Doc não cobre, é decisão técnica.
- Padronização facilita o consumo pelo frontend (parse JSON sempre tem sucesso).
- Não-breaking; nenhum cliente legítimo depende de body vazio em 404.

**Ação**: agente `dev-dotnet-carwash` adiciona middleware `UseStatusCodePages` com fallback para 404 que ainda não tem ContentType.

---

## 6. Email único de cliente — MANTER ✅

**Decisão do dev**: implementou email único com índice parcial.

**Justificativa**: doc não exige, mas é boa prática contra erro de cadastro duplicado. Não há cenário de negócio que justifique aceitar emails duplicados (cliente compartilhando email com conjuge/empresa é caso raro e pode ser tratado por exceção manual).

**Ação**: manter como está.

---

## 7. Auto-desativação admin (BUG-U009) — MANTER ✅

**Decisão do dev**: implementou 409 bloqueando:
1. Admin desativando a si mesmo.
2. Desativação do último admin ativo.

**Justificativa**: doc não cobre, mas é proteção operacional crítica (sem essa trava, sistema pode ficar sem admin).

**Ação**: manter como está.

---

## 8. GAP-CW-CLI-STA-AGD — ADIAR ⏸

**Sintoma observado**: desativar cliente com agendamentos abertos é permitido.

**Decisão**: **aguardar feature de agendamento (UC003/UC004)**.

**Justificativa**: hoje não há tabela `agendamentos` populada nem feature implementada. Reabrir quando UC003/UC004 estiverem prontos.

**Ação**: registrar em backlog de UC003.

---

## Resumo final

| # | Item | Decisão | Quem implementa |
|---|---|---|---|
| 1 | BUG-TENANT-CLI | Não-bug (RF-FUT003) | — |
| 2 | BUG-LGPD-CLI | Não-bug no MVP (RN001 + uso interno) | — |
| 3 | BUG-POST-DATANASC-NULL | Fix técnico | dev-dotnet-carwash |
| 4 | BUG-PUT-ATIVO-IGNORADO | Fix técnico (warning) | dev-dotnet-carwash |
| 5 | BUG-CONTRATO-404-ROUTE | Fix técnico (ProblemDetails) | dev-dotnet-carwash |
| 6 | Email único | Manter (boa prática) | — |
| 7 | Auto-desativação admin | Manter (proteção operacional) | — |
| 8 | GAP-CW-CLI-STA-AGD | Adiar até UC003/UC004 | — |

Após os 3 fixes técnicos, **release do MVP fica liberado para Auth + Usuários + Clientes**. Próximo bloco: UC003 (veículos), UC004 (serviços), UC005-UC011 (agendamentos).
