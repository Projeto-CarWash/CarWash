# ADR 0006 — Consolidação da árvore de migrations EF Core (DRAFT)

- **Status:** **Proposta (DRAFT)** — pendente de decisão do Arquiteto Técnico (Guilherme Brogio) e do CEO. Bloqueador ativo: 183/187 testes de integração falhando.
- **Data:** 2026-05-25
- **Autores:** Antonio Neto (PO/PM) — preparação da decisão a partir das pendências da sessão de recuperação do monorepo.
- **Escopo:** Backend `.NET 8` — assembly `CarWash.Infrastructure`, projeto `backend/src/CarWash.Infrastructure/CarWash.Infrastructure.csproj` e DbContext `CarWashDbContext`.
- **Relacionado:** RAT01 (Governança de esquema e migrações versionadas — DAT §11) · ADR 0001 (UUID em app) · ADR 0003 (CQRS/Vertical Slices).

> **Importante:** este ADR está em formato **draft** intencional. O Analista de
> Requisitos não decide arquitetura — apenas inventaria evidências, opções e
> consequências de cada caminho. A decisão final é do Arquiteto Técnico
> com aprovação do CEO. O analista assina apenas uma recomendação tática
> (§Recomendação tática) para acelerar a deliberação.

---

## Contexto

O assembly `CarWash.Infrastructure` carrega **duas árvores de migrations EF Core
distintas no mesmo DbContext (`CarWashDbContext`)**, ambas com uma migração
chamada `InitialSchema`. O EF Core não suporta dois `ModelSnapshot` por
DbContext — o resultado prático é que 183 de 187 testes de integração falham
(detectado durante a sessão de recuperação de pipeline conduzida pelo
arquiteto).

### Evidências concretas

#### Árvore A — `CarWash.Infrastructure/Migrations/` (namespace `CarWash.Infrastructure.Migrations`)

```
backend/src/CarWash.Infrastructure/Migrations/
  20260517223641_InitialSchema.cs                    (tabela `users` simplificada)
  20260517223641_InitialSchema.Designer.cs
  20260520120000_AddClientesVeiculos.cs              (cria `clientes` minimalista + `veiculos`)
  20260520120000_AddClientesVeiculos.Designer.cs
  20260520121000_UpdateVeiculoPlacaLength.cs
  20260520121000_UpdateVeiculoPlacaLength.Designer.cs
  20260521202913_AddVeiculosToClienteCadastro.cs
  20260521202913_AddVeiculosToClienteCadastro.Designer.cs
  CarWashDbContextModelSnapshot.cs
```

- `InitialSchema` cria `users` (`email`, `password_hash`, `active`, `created_at`,
  `updated_at`) — modelo **antigo**, anterior à entidade `Usuario` atual.
- `AddClientesVeiculos` cria `clientes` com apenas `id`, `nome`, `created_at`
  — modelo simplificado, anterior à refatoração `RefatoraClienteEndereco`.
- Conjunto **não** cria `btree_gist`, **não** cria tabelas de auditoria, sessão,
  preferência, agendamento, item, histórico, filial, serviço, outbox,
  notificação, idempotência etc.
- Conclusão: árvore **incompleta** e **desalinhada** com o modelo atual do
  domínio (`Domain/Entities/Usuario.cs`, `Cliente.cs`, `Veiculo.cs`,
  `Agendamento.cs`, `IdempotenciaRequisicao.cs`, etc.).

#### Árvore B — `CarWash.Infrastructure/Persistence/Migrations/` (namespace `CarWash.Infrastructure.Persistence.Migrations`)

```
backend/src/CarWash.Infrastructure/Persistence/Migrations/
  20260513114525_InitialSchema.cs                    (schema completo, `btree_gist`, RN011)
  20260513114525_InitialSchema.Designer.cs
  20260517022432_AddUsuarioLockoutFields.cs          (RF001/RNF003 — campos de lockout)
  20260517022432_AddUsuarioLockoutFields.Designer.cs
  20260517061810_RefatoraClienteEndereco.cs
  20260517061810_RefatoraClienteEndereco.Designer.cs
  20260517174741_AdicionaAuditoriaUsuarioCliente.cs
  20260517174741_AdicionaAuditoriaUsuarioCliente.Designer.cs
  20260521211104_AdicionaTotaisAgendamento.cs
  20260521211104_AdicionaTotaisAgendamento.Designer.cs
  20260522113123_AdicionaIdempotenciaRequisicoes.cs
  20260522113123_AdicionaIdempotenciaRequisicoes.Designer.cs
  CarWashDbContextModelSnapshot.cs
  .editorconfig
```

- `InitialSchema` cria schema completo: clientes, veículos, filiais, serviços,
  agendamentos, itens, histórico, outbox, notificações, usuário/sessão/preferência,
  ativando `btree_gist` (ADR 0001 + RN011).
- Sequência completa cobre: campos de lockout (`AddUsuarioLockoutFields` —
  necessária para o `LoginHandler` atual), refator de endereço estruturado,
  auditoria, totais denormalizados de agendamento (`AdicionaTotaisAgendamento`
  — referenciado pelo `ConsultarAgendaHandler`), idempotência de requisições
  (ADR 0004).
- Conclusão: árvore **completa**, **alinhada** ao domínio atual e referenciada
  por features já entregues (RF009 / card 132 / ADR 0005, RF015 / card 133 /
  ADR 0004).

### Por que o EF Core quebra

- **Dois `CarWashDbContextModelSnapshot` no mesmo assembly** com a mesma
  classe parcial e mesmo `DbContext` alvo → conflito de compilação ou
  comportamento indefinido em runtime (último snapshot vence, dependendo da
  ordem do scan).
- **Dois `InitialSchema` em namespaces diferentes** mas mesmo nome lógico de
  migration → `dotnet ef database update` pode aplicar a errada ou recusar a
  operação ao detectar inconsistência entre `__EFMigrationsHistory` e snapshot.
- Em runtime de teste (Testcontainers), `migrator.MigrateAsync()` resolve uma
  das duas árvores; a outra fica órfã produzindo schema parcialmente correto
  ou totalmente incorreto.

### Linha do tempo (forensic curta)

- `20260513114525_InitialSchema` (Árvore B) — primeira migration completa.
- `20260517022432_AddUsuarioLockoutFields` (B) — adiciona lockout.
- `20260517061810_RefatoraClienteEndereco` (B) — refator profundo do cliente.
- `20260517174741_AdicionaAuditoriaUsuarioCliente` (B) — auditoria.
- `20260517223641_InitialSchema` (Árvore A) — **criada depois**, com schema
  simplificado e fora do padrão de pasta. Provavelmente resultado de
  `dotnet ef migrations add InitialSchema` rodado **sem** o flag de output
  dir e/ou contra um DbContext temporário durante uma branch de feature.
- `20260520120000_AddClientesVeiculos` (A) — segue na pasta errada.
- `20260520121000_UpdateVeiculoPlacaLength` (A) — segue na pasta errada.
- `20260521202913_AddVeiculosToClienteCadastro` (A) — segue na pasta errada
  (mesmo dia em que B recebeu `AdicionaTotaisAgendamento`).
- `20260522113123_AdicionaIdempotenciaRequisicoes` (B) — última, alinhada com
  ADR 0004.

A leitura prudente: **a Árvore B é a oficial; a Árvore A é um galho acidental
criado por divergência de configuração de `MigrationsAssembly` ou de output
dir, que ninguém percebeu até quebrar os testes.**

---

## Decisão

> **Aguardando bater o martelo (Arquiteto + CEO).**

Três opções inventariadas a seguir. O analista recomenda a Opção 2 (com
plano detalhado abaixo).

---

## Alternativas consideradas

### Opção 1 — Manter **`Persistence/Migrations/`** (Árvore B) e **deletar `Migrations/`** (Árvore A)

**Resumo:** apagar a árvore A inteira, ajustar `MigrationsAssembly`/`Migrations
HistoryTableName`/output dir para sempre escrever em `Persistence/Migrations/`,
revisar `Program.cs` e `CarWashDbContext` para garantir que o EF Core ignore o
namespace antigo.

**Prós:**
- **Preserva a sequência canônica** que já está alinhada com o domínio atual
  e com features entregues (RF001 lockout, RF005, RF009, RF015 via idempotência,
  totais de agendamento).
- **Zero perda de migrations úteis** — tudo que veio de A duplica (incompleto)
  o que B já entrega.
- **Zero risco para dados existentes** em ambientes pessoais que já rodaram
  B (a maioria dos devs, presumido pela completude da sequência).
- Reverte para o estado "uma árvore por DbContext" — padrão do EF Core.
- Não requer recriar dados — `__EFMigrationsHistory` continua válido para devs
  que rodaram B.

**Contras:**
- O 1 dev que eventualmente tenha rodado a Árvore A em desenvolvimento
  pessoal terá schema parcial — precisa dropar o DB e refazer. Risco baixo
  (ambiente local, sem produção).
- Exige cuidado com `.editorconfig`, `Designer.cs` e `ModelSnapshot.cs` que
  podem ter sido editados manualmente em A; melhor revisar antes do delete
  para garantir que não há regra/seed esquecida.

**Esforço:** PP–P (1 dev backend, ~2h: deletar A, smoke test, rodar suíte
completa).

**Risco residual:** baixíssimo. Eventual feature legítima em A que ninguém
tenha portado para B precisa ser detectada antes do delete. A análise feita
indica que A não cobre nada que B já não cubra.

---

### Opção 2 — Manter **`Persistence/Migrations/`** (Árvore B), deletar **`Migrations/`** (Árvore A) **e** trancar configuração para impedir reincidência

**Resumo:** igual à Opção 1 + investir em prevenção. Adiciona:

- Setar `MigrationsAssembly` explícito em `CarWashDbContext.OnConfiguring`
  (ou no `AddDbContext`) → `optionsBuilder.UseNpgsql(connStr, x =>
  x.MigrationsAssembly("CarWash.Infrastructure").MigrationsHistoryTable(
  "__EFMigrationsHistory", "public"))`.
- Configurar **output dir padrão** em `Directory.Build.props` ou em um
  `.editorconfig` raiz do projeto Infrastructure, garantindo que
  `dotnet ef migrations add <Nome>` sempre saia em
  `Persistence/Migrations/`.
- Adicionar um teste de sanidade (`MigrationConventionsTests`) que falha o
  build se aparecer qualquer migration fora de
  `CarWash.Infrastructure/Persistence/Migrations/`.
- Documentar no `docs/arquitetura-backend.md`: seção curta "Como adicionar
  migration" com o comando completo (`dotnet ef migrations add <Nome>
  --project backend/src/CarWash.Infrastructure --startup-project
  backend/src/CarWash.Api --output-dir Persistence/Migrations`).

**Prós:**
- Todos os ganhos da Opção 1.
- **Não-regressão garantida**: o erro que originou a Árvore A não pode mais
  passar despercebido — o teste de convenção falha o build se o output for
  errado.
- Documenta o padrão para devs novos.
- Casa com o espírito do RAT01 do DAT (governança de esquema).

**Contras:**
- Esforço incremental sobre a Opção 1 (~+2–3h de prevenção).
- O teste de convenção é uma camada extra de "infra de teste" — precisa ser
  mantida.

**Esforço:** P (1 dev backend, ~meio dia: limpeza + prevenção + doc).

**Risco residual:** muito baixo. A prevenção fecha o ciclo de feedback.

---

### Opção 3 — Refazer do zero (squash de todas as migrations em uma `InitialSchema` única)

**Resumo:** dropar as duas árvores, snapshotar o modelo atual e gerar uma
única migration `InitialSchema_v2` (ou apenas `InitialSchema` se o histórico
puder ser perdido). Renomear o histórico de migrations em qualquer ambiente
de demonstração para casar com o novo baseline.

**Prós:**
- **Schema mais enxuto** — sem o ruído de `RefatoraClienteEndereco`,
  `AdicionaTotaisAgendamento` etc., a história fica linear.
- **Zero ambiguidade futura** — uma única migration baseline limpa.

**Contras:**
- **Quebra qualquer ambiente que já tenha rodado a Árvore B** (a maioria dos
  devs, possivelmente staging). `__EFMigrationsHistory` velho fica
  inconsistente — exige rebuild completo do DB.
- Perde **auditoria histórica** das mudanças (quando e por que `clientes`
  ganhou endereço estruturado, quando lockout foi adicionado etc.) — material
  útil em pen-test/auditoria.
- Custo de teste: cada feature já entregue (auth, idempotência, lockout)
  precisa ser revalidada contra a nova migration.
- Maior risco operacional se houver qualquer ambiente externo (proprietário,
  staging de homologação) que já rode B — coordenação cara para um ganho
  estético.
- Não resolve a causa raiz (configuração do EF Core que permitiu duas árvores)
  — sem a prevenção da Opção 2, o problema reincide.

**Esforço:** M (1 dev backend, ~1–2 dias: limpeza + nova baseline + revalidação
+ comunicação a todos os devs para resetar DBs locais).

**Risco residual:** médio. Bom só se houver outra razão estratégica para
limpar o histórico (ex: rebrand de schema, troca de SGBD, mudança grande de
domínio que torne migrations antigas obsoletas — nenhuma se aplica hoje).

---

## Consequências

### Se a Opção 1 ou 2 for aceita

#### Positivas
- Restaura 183 testes de integração → pipeline volta a verde.
- Mantém auditoria histórica do schema (importante para o RAT01).
- Não quebra nenhum ambiente de dev/staging que rodou B.

#### Negativas
- Devs que rodaram A em ambiente pessoal precisam dropar DB local e refazer
  com B. Comunicação clara via canal do time.

### Se a Opção 3 for aceita

#### Positivas
- Schema mais enxuto e história linear.

#### Negativas
- Perda de auditoria.
- Quebra ambientes pré-existentes.
- Não resolve a causa raiz se não vier acompanhada da prevenção da Opção 2.

---

## Recomendação tática (visão do analista — não vinculante)

**Opção 2** — manter `Persistence/Migrations/`, deletar `Migrations/` e
investir nas 4 guardas de prevenção (MigrationsAssembly explícito, output dir
padrão, teste de convenção, documentação).

**Justificativa em uma linha:** o ganho de "schema enxuto" da Opção 3 não
compensa o custo de revalidar todas as features já entregues e o risco em
ambientes externos, enquanto a Opção 2 fecha o ciclo de feedback que originou
o problema.

**Ordem de execução sugerida (caso aceita):**
1. Inspecionar manualmente cada arquivo de A para confirmar zero overlap útil
   com B (rotina já feita em rascunho — confirmar antes de deletar).
2. Backup curto do diretório `Migrations/` (zip local) caso o arquiteto queira
   inspeção forense pós-delete.
3. Deletar `backend/src/CarWash.Infrastructure/Migrations/` inteiro.
4. Adicionar `MigrationsAssembly` + `MigrationsHistoryTable` explícitos no
   `AddDbContext` (em `CarWash.Infrastructure/DependencyInjection.cs`).
5. Adicionar `Directory.Build.targets` ou similar para fixar output dir em
   `Persistence/Migrations/`.
6. Escrever `MigrationConventionsTests.cs` em `CarWash.UnitTests` (ou
   `CarWash.IntegrationTests` em smoke test) que falha se houver migration
   fora do diretório canônico.
7. Atualizar `docs/arquitetura-backend.md` com seção "Como criar migration".
8. Rodar `dotnet ef database drop && dotnet ef database update` em ambiente
   local de cada dev (comunicado no canal do time).
9. Validar suíte completa de integração: 187/187 verdes.

**Pessoas a alinhar antes do merge:** Guilherme Brogio (arquiteto — owner do
DAT), CEO (sinal verde para anunciar a regra ao time), todos os devs backend
(precisam dropar DB local).

---

## Re-avaliação

Esta ADR deve ser revisitada quando:

- O time precisar de uma segunda base de dados / DbContext (ex: read-replica
  com schema próprio) → reabrir discussão sobre nomenclatura e isolamento de
  migrations por DbContext.
- Surgir necessidade de squash legítimo (mudança grande de domínio) — então
  considerar a Opção 3 como decisão consciente.
- O teste de convenção (Opção 2) reportar tentativa de violação em PR — sinal
  de que o padrão precisa ser melhor comunicado.

---

## Referências

- ADR 0001 — [`./0001-geracao-de-uuid-pela-aplicacao.md`](./0001-geracao-de-uuid-pela-aplicacao.md) (motivou `btree_gist` em B).
- ADR 0003 — [`./0003-minimal-api-cqrs-vertical-slices.md`](./0003-minimal-api-cqrs-vertical-slices.md) (padrão de slices que depende do schema).
- ADR 0004 — [`./0004-confirmacao-em-duas-etapas-do-agendamento.md`](./0004-confirmacao-em-duas-etapas-do-agendamento.md) (idempotência adicionada em B).
- ADR 0005 — [`./0005-consulta-de-agenda-rf009.md`](./0005-consulta-de-agenda-rf009.md) (depende do índice `idx_ag_filial_inicio` criado em B).
- DAT §11 — RAT01 (Governança de esquema e migrações versionadas).
- EF Core — [Multiple DbContexts and migrations](https://learn.microsoft.com/ef/core/managing-schemas/migrations/projects).
