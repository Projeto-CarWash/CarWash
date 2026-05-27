# ADR 0007 — Cadastro de filiais (RF017) — extensão do agregado `Filial` e endpoint público

- **Status:** Aceita
- **Data:** 2026-05-27
- **Autores:** Arquiteto Técnico do CarWash (Stage 2 do pipeline analista→arquiteto→devs→QA).
- **Escopo:** Backend `.NET 8` — `CarWash.Domain` (agregado `Filial`), `CarWash.Application` (slices `Filiais/Criar` e `Filiais/Listar`), `CarWash.Infrastructure` (configuração EF + nova migration + repositório), `CarWash.Api` (endpoints `POST /api/v1/filiais` e `GET /api/v1/filiais`).
- **Relacionado:** RF017 (Must) · RF018 (Must) · RF019/RF020 (habilitadores) · RN009 · RN010 · RAT01 · RAT03 · RAT04 · ADR 0001 · ADR 0003 · ADR 0006 · Card de backlog `docs/backlog/card-204-rf017-cadastro-de-filiais.md`.

---

## 1. Contexto e escopo

O backend já possui a entidade `Filial` consumida por agendamento, agenda, auditoria e regra RN011, mas nunca expôs cadastro: o frontend (`frontend/src/services/filialService.ts`) cai em 404 ao listar e a matriz CA001 do DRP fica represada. Este ADR materializa o **Stage 2 (DoR para devs)** do card 204 — RF017 + RF018 — aplicando as decisões já ratificadas pelo CEO sobre as lacunas L1–L8: `nome` único case-insensitive via índice funcional `LOWER(nome)` (L1=b); `cnpj` opcional + UNIQUE parcial `NULLS DISTINCT` (L2=b); `uf` reaproveitando a lista das 27 UFs do VO `Endereco` (L3 ratificada); `ativo` ignorado no POST e sempre `true` (L4 ratificada); autenticação só por `RequireAuthorization()` (L5=a); endereço estruturado completo via VO `Endereco` (L6=b); GET de listagem incluso neste card (L7=a); `PATCH /status` fora deste card (L8 ratificada — card derivado 206). Migration é estritamente **aditiva e nullable** (RAT01) e o cadastro herda a auditoria automática do `AuditLogInterceptor` já listado em `EntidadesAuditaveis` (DAT §9.1).

---

## 2. Modelo de domínio

### 2.1 Entidade `Filial` estendida

```
Filial : IAuditable, IAuditableSetter
  Id                   Guid              private set     // app-side (ADR 0001)
  Nome                 string            private set     // 3..120, sanitizado
  Codigo               string            private set     // ^[A-Z0-9]{2,20}$ — novo
  Cnpj?                Cnpj              private set     // VO 14 dígitos — novo, opcional (L2)
  Endereco?            Endereco          private set     // VO completo — novo, opcional p/ rollout aditivo
  Ativa                bool              private set     // default true
  CelulasAtivas        int               private set     // 1..100 (RN009)
  Timezone             string            private set     // default "America/Sao_Paulo"
  CriadoEm             DateTime          private set
  AtualizadoEm         DateTime          private set
  CriadoPorUsuarioId?  Guid              private set     // novo — análogo a Cliente (RAT04)
```

Permanecem inalterados: `AjustarCelulas`, `Inativar`, `Ativar`, `IAuditableSetter` (timestamps preenchidos pelo interceptor `AuditableEntitiesInterceptor`).

### 2.2 Fábrica final

```csharp
public static Filial Criar(
    Guid id,
    string nome,
    string codigo,
    int celulasAtivas,
    Endereco? endereco = null,
    Cnpj? cnpj = null,
    string? timezone = null);
```

**Decisões:**

- `codigo` é **obrigatório no domínio** (regra de negócio crítica) — mesmo a coluna sendo nullable no banco durante o rollout aditivo, a fábrica recusa criação sem `codigo`. Devs não devem pular essa porta; seeds e testes velhos que dependem da fábrica antiga viram problema do passo de migração (ver §2.4).
- `endereco` e `cnpj` são opcionais — a invariante "se preenchido, é válido" mora dentro dos VOs (já implementada em `Endereco.cs` e `Cnpj.cs`).
- `timezone` mantém o default histórico `America/Sao_Paulo` (sem mudança).
- `RegistrarCriadoPor(Guid? usuarioId)` é adicionado, idêntico a `Cliente.RegistrarCriadoPor` (linha 186 de `Cliente.cs`). Não é argumento da fábrica para preservar o padrão "comando seta auditoria depois de instanciar" do `CriarClienteHandler`.

### 2.3 Invariantes — domínio vs validator

| Regra | Onde vive | Justificativa |
|-------|-----------|---------------|
| `id != Guid.Empty` | Domínio (já existe) | Invariante fundamental do agregado. |
| `nome` 3..120 + não-vazio | Domínio (sanity check) + Validator (mensagem detalhada) | Domínio existe para impedir corrupção; Validator entrega 400 estruturado com chave `nome`. |
| `codigo` regex `^[A-Z0-9]{2,20}$` | Domínio (regra de negócio crítica) + Validator (mensagem) + CHECK no banco | Tripla defesa (RAT03). Domínio é o ponto de verdade. |
| `celulasAtivas` entre 1..100 (RN009 / CA008) | Domínio (já existe) + Validator + CHECK no banco | RN009 é Must; tripla defesa obrigatória. |
| Formato sintático do CNPJ (14 dígitos + DV) | VO `Cnpj` (já existe) + Validator (mensagem) | VO falha com `DomainException`; Validator antecipa para 400. |
| Estrutura do `Endereco` (CEP, UF entre 27, etc.) | VO `Endereco` (já existe) + Validator (mensagens por campo) | Mesmo padrão de `Cliente`. |
| Unicidade de `nome` (case-insensitive), `codigo`, `cnpj` | Repositório (pré-check) + UNIQUE no banco | **Não** mora no domínio — depende de consulta externa. Tradução de violação concorrente vira `ConflictException` específica (§4). |
| Default `Ativa = true` | Domínio (já existe) | L4. POST ignora qualquer valor de `ativo` enviado. |

### 2.4 Compatibilidade da fábrica antiga

A assinatura antiga `Filial.Criar(Guid id, string nome, int celulasAtivas, string? timezone = null)` **será removida** — não marcada `[Obsolete]`. Justificativas:

1. A única fonte conhecida que chamava a fábrica antiga é o teste de domínio e eventuais seeds. Marcar `[Obsolete]` introduziria warning ruidoso no build e deixaria a porta aberta para criação de filiais sem `codigo`, ferindo a invariante recém-adicionada.
2. Quem precisar criar uma filial em teste/seed passa a fornecer `codigo` (ex.: `"MTZ"`, `"TST01"`). Refatoração mecânica.
3. Se algum seed dependia de uma filial sem código (improvável no MVP), o backfill da migration cobre a única instância eventualmente existente (ver §3.4).

Se durante a implementação o dev encontrar mais de 5 chamadas afetadas, escalar ao arquiteto antes de remover — pode justificar `[Obsolete]` transitório por uma sprint.

### 2.5 `RegistrarCriadoPor` em `Filial`

**Necessário.** O `AuditLogInterceptor` já intercepta `Filial`, mas o `CriadoPorUsuarioId` é o ator do cadastro armazenado na própria linha (espelha `Cliente`). O handler chama `filial.RegistrarCriadoPor(command.UsuarioId)` antes de `AdicionarAsync`, exatamente como `CriarClienteHandler:80`. Sem isso a coluna fica nula e a auditoria via `audit_logs` segue funcionando, mas perdemos o atalho de "quem criou esta filial" sem joinar logs — o cliente já faz isso e a paridade é barata.

---

## 3. Persistência (PostgreSQL via EF Core)

### 3.1 Tabela `filiais` — colunas finais

| Coluna | Tipo | Nullable | Notas |
|--------|------|----------|-------|
| `id` | `uuid` | NOT NULL | PK (app-side, ADR 0001) |
| `nome` | `varchar(120)` | NOT NULL | Inalterado |
| `codigo` | `varchar(20)` | **NULL** (rollout) | Aditivo. Card derivado 207 vira NOT NULL após backfill. |
| `cnpj` | `varchar(14)` | NULL | Opcional (L2) |
| `endereco_cep` | `varchar(8)` | NULL | Owned via shadow properties planas (espelha `Cliente`) |
| `endereco_logradouro` | `varchar(150)` | NULL | |
| `endereco_numero` | `varchar(20)` | NULL | |
| `endereco_complemento` | `varchar(100)` | NULL | |
| `endereco_bairro` | `varchar(100)` | NULL | |
| `endereco_cidade` | `varchar(100)` | NULL | |
| `endereco_uf` | `char(2)` | NULL | Fixed length, mesmo de `Cliente` |
| `ativa` | `bool` | NOT NULL | default true (inalterado) |
| `celulas_ativas` | `int` | NOT NULL | (inalterado) |
| `timezone` | `varchar(64)` | NOT NULL | default `America/Sao_Paulo` |
| `criado_em` | `timestamptz` | NOT NULL | default `now()` |
| `atualizado_em` | `timestamptz` | NOT NULL | default `now()` |
| `criado_por_usuario_id` | `uuid` | NULL | NOVO. FK lógica (sem REFERENCES — espelha `Cliente.criado_por_usuario_id`). |

> **Estratégia de mapping no EF:** seguir o padrão de `ClienteConfiguration` — **NÃO** usar `OwnsOne` para `Endereco`. As colunas `endereco_*` ficam como **propriedades planas** no agregado (`EnderecoCep`, `EnderecoLogradouro`, ..., `EnderecoUf`), com um getter computado `public Endereco? Endereco => string.IsNullOrEmpty(EnderecoCep) ? null : new Endereco(...)` e `builder.Ignore(x => x.Endereco)`. Consistência com `Cliente` vence elegância de owned type.

### 3.2 Constraints e índices

| Nome | Tipo | DDL conceitual | CA / RN | Justificativa |
|------|------|----------------|---------|---------------|
| `pk_filiais` | PK | `PRIMARY KEY (id)` | — | Identidade |
| `uk_filiais_codigo` | UNIQUE parcial | `CREATE UNIQUE INDEX uk_filiais_codigo ON filiais (codigo) WHERE codigo IS NOT NULL` | CA-204.3 | Rede de segurança contra duplicidade concorrente. Parcial enquanto `codigo` é nullable; vira full UNIQUE no card 207 quando o backfill completar. |
| `uk_filiais_cnpj` | UNIQUE parcial | `CREATE UNIQUE INDEX uk_filiais_cnpj ON filiais (cnpj) WHERE cnpj IS NOT NULL` (Postgres trata múltiplos NULL como distintos por default em UNIQUE INDEX, dispensando `NULLS DISTINCT` no PG16+) | CA-204.3 + L2 | CNPJ opcional pode repetir como NULL, mas não como valor concreto. |
| `uk_filiais_nome_lower` | UNIQUE funcional | `CREATE UNIQUE INDEX uk_filiais_nome_lower ON filiais (LOWER(nome))` | CA-204.3 + L1 | Bloqueia "Filial Centro" vs "FILIAL CENTRO". **Drop obrigatório do `uk_filiais_nome` cru existente** na mesma migration. |
| `ck_filiais_celulas_faixa` | CHECK | `celulas_ativas BETWEEN 1 AND 100` (já existe) | CA-204.2 + RN009 + CA008 | Defesa final RN009. |
| `ck_filiais_codigo_formato` | CHECK | `codigo IS NULL OR codigo ~ '^[A-Z0-9]{2,20}$'` | CA-204.2 + CA-204.3 | Garante formato mesmo em escrita fora do fluxo da app. Permite NULL durante rollout. |
| `idx_filiais_ativa` | índice parcial | `WHERE ativa = true` (já existe) | CA-204.4 + RF019 | Suporta a consulta padrão do agendamento (`ativo=true`). |
| `idx_filiais_cidade_uf` | índice composto parcial | `(endereco_cidade, endereco_uf) WHERE endereco_cidade IS NOT NULL` | GET listagem | Útil quando o GET filtra por cidade/UF. **Adicionar** — barato e antecipa filtros do front. O `LOWER(endereco_cidade)` entrará como follow-up se aparecer query case-insensitive concreta nessa coluna. |

### 3.3 Justificativa por CA

- **CA-204.1 (sucesso):** PK + colunas nullable permitem inserir sem nenhum bloqueio acidental. `ck_filiais_codigo_formato` aceita o valor enviado.
- **CA-204.2 (formato):** dois CHECKs (faixa + formato do código) cobrem o que o validator pode bypassar; UF inválida é coberta dentro do VO `Endereco`.
- **CA-204.3 (duplicidade):** três UKs cobrem as três chaves naturais; tradução para slug ocorre no repositório por nome de constraint.
- **CA-204.4 (filial disponível para agendamento):** `idx_filiais_ativa` mantém `ObterFilialResumoAsync` rápido.
- **CA-204.5/CA-204.6:** já implementados no `CalculadoraResumoAgendamento`; só validar na suíte.

### 3.4 Estratégia da migration

**Nome:** `AdicionaCadastroFilial` (próxima sequência em `Persistence/Migrations/` — ADR 0006 manda **criar nova, NUNCA editar `20260513114525_InitialSchema`**).

**Operações em ordem (transação única):**

1. `ALTER TABLE filiais ADD COLUMN codigo varchar(20) NULL;`
2. `ALTER TABLE filiais ADD COLUMN cnpj varchar(14) NULL;`
3. `ALTER TABLE filiais ADD COLUMN endereco_cep varchar(8) NULL;` ... (até `endereco_uf char(2) NULL`).
4. `ALTER TABLE filiais ADD COLUMN criado_por_usuario_id uuid NULL;`
5. `ALTER TABLE filiais ADD CONSTRAINT ck_filiais_codigo_formato CHECK (codigo IS NULL OR codigo ~ '^[A-Z0-9]{2,20}$');`
6. `DROP INDEX uk_filiais_nome;`
7. `CREATE UNIQUE INDEX uk_filiais_nome_lower ON filiais (LOWER(nome));`
8. `CREATE UNIQUE INDEX uk_filiais_codigo ON filiais (codigo) WHERE codigo IS NOT NULL;`
9. `CREATE UNIQUE INDEX uk_filiais_cnpj ON filiais (cnpj) WHERE cnpj IS NOT NULL;`
10. `CREATE INDEX idx_filiais_cidade_uf ON filiais (endereco_cidade, endereco_uf) WHERE endereco_cidade IS NOT NULL;` (índice composto parcial; ver §3.2 sobre `LOWER(endereco_cidade)` como follow-up)

**Backfill:** **não há backfill automático na migration.** Se houver filial seed pré-existente sem `codigo`, ela permanece com `codigo = NULL` e é corrigida manualmente em homologação (uma única linha). O card 207 fechará o ciclo trocando `codigo` para `NOT NULL` depois que o backfill manual confirmar 0 linhas com `codigo IS NULL`.

**Pré-check obrigatório antes do deploy do `uk_filiais_nome_lower`:** o índice antigo `uk_filiais_nome` era case-sensitive, então pares como `"Filial Centro"` vs `"FILIAL CENTRO"` eram permitidos. O novo índice funcional em `LOWER(nome)` é mais restritivo e a migration falha se houver colisão pré-existente. **Rodar em homologação e produção antes do `database update`:**

```sql
SELECT LOWER(nome) AS chave, COUNT(*) AS qtd, array_agg(nome) AS nomes
FROM filiais
GROUP BY LOWER(nome)
HAVING COUNT(*) > 1;
```

Se houver linhas, decidir manualmente (renomear, desativar ou consolidar) **antes** de aplicar a migration. Hoje a base do MVP tem ≤ 3 filiais seed sem colisão — risco operacional baixo, mas o pré-check é parte do runbook de deploy.

**Checklist obrigatório para o dev (ADR 0006 + RAT01):**

- [ ] Migration criada com `--output-dir Persistence/Migrations` (output dir canônico).
- [ ] `*.Designer.cs` gerado e versionado.
- [ ] `CarWashDbContextModelSnapshot.cs` atualizado e conferido por diff.
- [ ] `dotnet ef database update` executado limpo em DB local zerado.
- [ ] Suite de integração roda 100% verde após `database update`.
- [ ] Nenhuma alteração em `20260513114525_InitialSchema.cs`.

---

## 4. Contrato HTTP

### 4.1 `POST /api/v1/filiais`

**Request body** (camelCase, padrão `System.Text.Json` do projeto):

```json
{
  "nome": "Filial Matriz",
  "codigo": "MTZ",
  "cnpj": "12345678000190",
  "celulasAtivas": 50,
  "timezone": "America/Sao_Paulo",
  "endereco": {
    "cep": "01310100",
    "logradouro": "Av. Paulista",
    "numero": "1000",
    "complemento": "Sala 12",
    "bairro": "Bela Vista",
    "cidade": "São Paulo",
    "uf": "SP"
  }
}
```

- `cnpj`, `timezone` e `endereco` são opcionais.
- Qualquer campo `ativo` enviado é **ignorado** (L4).

**Response 201 Created:**

```json
{
  "id": "8f3d...",
  "mensagem": "Filial cadastrada com sucesso.",
  "traceId": "0HMV..."
}
```

- Tipo .NET: `CriarFilialResponse { Guid Id; string Mensagem; string TraceId; }` — espelha `CriarClienteResponse` (envelope `{id, mensagem, traceId}` é o padrão canônico do projeto para POST de cadastro).
- Header obrigatório: `Location: /api/v1/filiais/{id}` via `TypedResults.Created(...)`.
- Header obrigatório: `correlationId` ecoado em `Extensions` do ProblemDetails em qualquer erro (já garantido pelo `ExceptionHandlingMiddleware`).

### 4.2 `GET /api/v1/filiais`

**Query params:**

| Nome | Tipo | Default | Notas |
|------|------|---------|-------|
| `ativo` | `bool?` | `null` (sem filtro) | Frontend chama `?ativo=true`. |
| `busca` | `string?` | `null` | Casa em `nome`, `codigo`, `endereco_cidade` via `EF.Functions.ILike` (case-insensitive). Sem `unaccent` por enquanto; aceita-se entrar como follow-up se aparecer requisito concreto de busca tolerante a acentos. |
| `pagina` | `int` | `1` | Mín. 1 — 400 com chave `pagina` se inválido (idêntico a `ClientesEndpoints.ListarAsync`). |
| `tamanhoPagina` | `int` | `20` | 1..100 — 400 se fora da faixa. |

**Response 200 OK:**

```json
{
  "itens": [
    {
      "id": "8f3d...",
      "nome": "Filial Matriz",
      "codigo": "MTZ",
      "cidade": "São Paulo",
      "uf": "SP",
      "ativo": true
    }
  ],
  "total": 1,
  "pagina": 1,
  "tamanhoPagina": 20
}
```

- Compatível com `frontend/src/types/filial.ts` (`FilialResumo` + `ListaFiliais`): inclui `codigo` (campo extra, ignorado pelo TS atual), inclui paginação (`pagina`, `tamanhoPagina` — `ListaFiliais` atual só conhece `itens` e `total`; o front continua funcionando porque os campos extras são ignorados). O FE-01 do card 204 pode opcionalmente ampliar `ListaFiliais` para refletir paginação real.
- Sem `Cache-Control: no-store` aqui — `Filial` não é PII (diferente de `Cliente`/`Usuario`).

### 4.3 Tabela de respostas de erro

| Status | Quando | Slug (`type`) | Chave de campo |
|--------|--------|---------------|----------------|
| **400** | Validator falhou (nome, código formato, CNPJ DV, UF inválida, células fora 1..100, endereço incompleto) | (padrão `ValidationException` → `validation-error`) | `nome` / `codigo` / `cnpj` / `celulasAtivas` / `endereco.*` |
| **400** | Body ausente ou malformado | `validation-error` (chave de campo `body`) | `body` |
| **400** | `pagina` < 1 ou `tamanhoPagina` fora 1..100 (apenas GET) | `validation-error` | `pagina` / `tamanhoPagina` |
| **401** | Sem `Authorization: Bearer` | (padrão middleware auth) | — |
| **403** | Não aplicável no MVP (L5=a). Se RF-FUT003 introduzir policy, este status fica reservado. | — | — |
| **409** | `codigo` duplicado | `filial-codigo-ja-existe` | — |
| **409** | `cnpj` duplicado | `filial-cnpj-ja-existe` | — |
| **409** | `nome` duplicado (case-insensitive) | `filial-nome-ja-existe` | — |
| **422** | Não aplicável no POST (filial nasce ativa). Mantido para PATCH futuro (`recurso-inativo`) — documentado aqui. | `recurso-inativo` | — |
| **500** | Falha de infra | (mensagem genérica + `correlationId`) | — |

**Headers obrigatórios em toda resposta:**

- `traceId` no body de sucesso (campo do envelope).
- `correlationId` em `Extensions` de qualquer `ProblemDetails` (garantido pelo middleware).
- `Location` no 201 (garantido por `TypedResults.Created`).

---

## 5. Validação em camadas (defesa em profundidade)

| Campo | Validator FluentValidation | Invariante de domínio | Constraint de banco |
|-------|----------------------------|-----------------------|---------------------|
| `nome` | `NotEmpty` + 3..120 + `SanitizeTextOrNull` | Verificação repetida na fábrica `Filial.Criar` | UNIQUE `LOWER(nome)` |
| `codigo` | `NotEmpty` + regex `^[A-Z0-9]{2,20}$` após normalizar | Fábrica re-checa regex | CHECK `ck_filiais_codigo_formato` + UNIQUE parcial |
| `cnpj` | Opcional — quando presente: 14 dígitos + `DocumentoValidator.CnpjValido` | VO `Cnpj` valida formato e DV (lança `DomainException`) | UNIQUE parcial |
| `celulasAtivas` | `InclusiveBetween(1, 100)` (CA008) | Fábrica `Filial.Criar` (já existe) | CHECK `ck_filiais_celulas_faixa` |
| `endereco.cep` | 8 dígitos exatos | VO `Endereco` | — (sem CHECK próprio — VO basta) |
| `endereco.uf` | `Length(2)` | VO `Endereco` (lista das 27 UFs) | — |
| `endereco.*` (logradouro, número, bairro, cidade) | Mesmas regras do `CriarClienteCommandValidator` (linhas 105–128) | VO `Endereco` | — |

### 5.1 Normalização — onde

**No validator, antes de qualquer outra regra.** Justificativa: o domínio recebe valores já limpos (sem espaços em volta, em UPPER quando aplicável) e fica livre de re-normalizar. Padrão usa `InputNormalizer.SanitizeTextOrNull` (nome), `InputNormalizer.OnlyDigitsOrNull` (cnpj) e uma extensão local `codigo.Trim().ToUpperInvariant()`. O handler trata o campo como already-normalized.

### 5.2 Política para duplicidade — pré-check + UK

**Padrão `ClienteRepository`:**

1. Handler chama `repositorio.ExisteCodigoAsync(codigo)`, `ExisteCnpjAsync(cnpj)` e `ExisteNomeAsync(nome)` — este último faz igualdade case-insensitive em `LOWER(nome) = LOWER($1)` (lado direito pré-computado em C#), casando com o índice funcional `uk_filiais_nome_lower` e imune a curingas LIKE (`%`, `_`) no termo informado.
2. Se algum responder true, lança a exceção específica (`FilialCodigoJaExisteException`, `FilialCnpjJaExisteException`, `FilialNomeJaExisteException`), todas herdando de `ConflictException` com slug fixo.
3. `AdicionarAsync` envolve `SaveChangesAsync` em `try { ... } catch (DbUpdateException ex) when (IsUniqueViolation(ex)) { ... }`. Dentro do catch, **inspecionar `PostgresException.ConstraintName`** e mapear:

```csharp
constraintName switch
{
    "uk_filiais_codigo"      => throw new FilialCodigoJaExisteException(ex),
    "uk_filiais_cnpj"        => throw new FilialCnpjJaExisteException(ex),
    "uk_filiais_nome_lower"  => throw new FilialNomeJaExisteException(ex),
    _                         => throw new ConflictException("Conflito ao cadastrar filial.", ex)
};
```

Esta tradução é a defesa contra **race condition entre pré-check e insert** — o middleware traduz cada slug para 409 + mensagem certa.

---

## 6. Segurança e autorização

### 6.1 Autorização — L5 = (a)

O grupo `app.MapGroup("/api/v1/filiais")` recebe `.RequireAuthorization()` puro (sem policy). CA-204.8 fica documentado como "verificação adiada para RF-FUT003" — quando o roadmap entregar RBAC, criar policy `filiais.gerenciar` e aplicar.

### 6.2 `UsuarioId` no handler

Mesma extração de `ClientesEndpoints.ObterUsuarioId` (linhas 266–271): claim `sub` ou `ClaimTypes.NameIdentifier` parseado para `Guid`. O endpoint passa `usuarioId` ao command; o handler chama `filial.RegistrarCriadoPor(command.UsuarioId)` antes de `AdicionarAsync`.

### 6.3 Auditoria

- `AuditLogInterceptor` (linha 22 — `typeof(Filial)` já listada) gera linha em `audit_logs` automaticamente desde que o handler chame `_contexto.DefinirEvento("FilialCriada")` antes do `SaveChangesAsync` (padrão de `CriarUsuarioHandler:59`).
- **Decisão:** o handler **chama `DefinirEvento`**, não o repositório. Mantém Application no controle do "qual evento estamos emitindo" (consistência com `CriarUsuarioHandler`).
- Dados a registrar no `audit_logs.dados` (JSON): `Id`, `Nome`, `Codigo`, `Cidade`, `Uf`, `CelulasAtivas`, `PossuiCnpj` (bool, não o CNPJ em si). **CNPJ nunca em log** — PII fiscal.

### 6.4 Logs estruturados (RNF009 / DAT §9.1)

Campos obrigatórios no log de sucesso do endpoint:

```
"Filial cadastrada. TraceId: {TraceId}. FilialId: {FilialId}. Codigo: {Codigo}. UsuarioId: {UsuarioId}"
```

Proibido logar `Cnpj` em qualquer nível. Logar `Codigo` é seguro (não é PII fiscal — é identificador operacional).

---

## 7. Performance e escala

### 7.1 Índices justificados (GET)

- `idx_filiais_ativa` (parcial `WHERE ativa = true`) — cobre o filtro mais comum (`?ativo=true` do front).
- `idx_filiais_cidade_uf` — cobre busca por cidade/UF futura.
- `uk_filiais_nome_lower` — cobre tanto a unicidade quanto buscas `WHERE LOWER(nome) LIKE ?` rapidamente.
- `uk_filiais_codigo` — cobre lookup direto por código (caso de homologação manual).

Não é necessário índice em `cnpj` para leitura — a UK já faz isso.

### 7.2 Paginação

**Offset/limit** padrão (`Skip/Take`). Justificativa: a tabela `filiais` tem tamanho esperado em **dezenas de linhas** (não milhares) — keyset pagination seria overengineering. O mesmo padrão é usado em `ClientesEndpoints.ListarAsync`.

### 7.3 Cache

**Sem cache no MVP.** Volume baixíssimo, mudanças raras; o custo de invalidar cache supera o ganho. Reabrir se aparecer evidência de carga real.

---

## 8. Plano de testes

### 8.1 Pirâmide

**Unit — MUST**

- `Filial.Criar` falha com `DomainException` para: id vazio, nome vazio, nome > 120, código mal formado, células fora de 1..100, endereço inválido (delegado ao VO). [BE-15]
- `Filial.Criar` sucesso: instancia com `Ativa = true`, `Timezone = "America/Sao_Paulo"`, timestamps preenchidos pelo interceptor. [BE-15]
- `Filial.RegistrarCriadoPor` seta `CriadoPorUsuarioId`. [BE-15]
- `CriarFilialCommandValidator`: nome (3 casos), código (2 casos), CNPJ inválido, UF inválida (delega ao VO), células fora da faixa, payload válido. [BE-14]

**Integração (Testcontainers + Postgres real) — MUST**

- POST 201 com payload mínimo (sem cnpj e sem endereco) e completo. [CA-204.1 / BE-16]
- POST 400: 3 cenários representativos (nome inválido, UF inválida, células fora). [CA-204.2]
- POST 409: três UKs disparam, cada uma com slug correto. Para `nome`: tentar com case diferente, garantir bloqueio. [CA-204.3]
- POST 409 via race: dois inserts concorrentes do mesmo `codigo` — apenas um vence, o outro retorna 409 (tradução via constraint name). [CA-204.3]
- POST 401 sem JWT. [CA-204.7]
- POST + audit log: linha gravada em `audit_logs` com `evento = FilialCriada`, `entidade_id = filial.Id`, `usuario_id` correto. [CA-204.10]
- E2E: criar filial + chamar `POST /api/v1/agendamentos/pre-confirmacao` com aquele `filialId`. Pré-confirmação aceita (CA-204.4).
- GET 200: paginação válida, filtro `?ativo=true`, busca por nome. [BE-12]
- GET 400: `pagina=0`, `tamanhoPagina=101`. [BE-12]
- CA-204.5 e CA-204.6: já cobertos pela suíte de agendamento — confirmar que os testes existentes seguem verdes.

**E2E backend — MUST**

- O cenário "criar filial + pré-confirmar agendamento" acima já cumpre o E2E mínimo. Sem necessidade de teste Playwright agora (FE-02 está fora do card).

### 8.2 Opcionais (nice-to-have)

- Teste de propriedade do regex de `codigo` (FsCheck) — útil mas não bloqueante.
- Benchmark de GET com 1000 filiais — fora do escopo (volume real do MVP é dezenas).

---

## 9. Riscos e mitigações

### RAT01 — Governança de schema (DAT §11)

**Risco:** alterar migration existente ou criar migration fora de `Persistence/Migrations/`.
**Mitigação:** ADR 0006 já manda criar nova migration. Checklist do §3.4 obriga conferência de Designer + Snapshot. Code review tem itens explícitos para "nenhuma edição em `InitialSchema`".

### RAT03 — Validação server-side (DAT §11)

**Risco:** confiar só no frontend.
**Mitigação:** tripla defesa documentada em §5. Testes de integração cobrem cada camada explicitamente.

### RAT04 — Auditoria e logs (DAT §11 / DRP RNF009)

**Risco:** evento de criação não chegar ao `audit_logs`; vazamento de CNPJ em logs.
**Mitigação:** `_contexto.DefinirEvento("FilialCriada")` no handler (padrão já validado em `CriarUsuarioHandler`). Teste de integração explícito em §8.1 (`POST + audit log`). Política "nunca logar CNPJ" em §6.4 e §6.3.

### Risco específico: rollout aditivo deixa `codigo` nullable

**Risco:** uma inserção concorrente fora do fluxo da app (script SQL ad-hoc, EF Tools, seed externo) pode criar filial com `codigo = NULL`, ferindo a invariante de domínio e produzindo registros inconsistentes.
**Mitigação:**
- A fábrica `Filial.Criar` **exige** `codigo` — toda criação pela app já é segura.
- O CHECK `ck_filiais_codigo_formato` aceita NULL apenas durante o rollout, mas o card 207 está formalmente registrado neste ADR (§3.4) e na seção §12 abaixo. Quando 207 for entregue, `codigo` vira `NOT NULL` em definitivo e o CHECK perde o `IS NULL OR`.
- Documentação para o time: "Não escrever em `filiais` fora da app sem código." Reforçado neste ADR e no card 204.

### Risco operacional: race condition entre pré-check de duplicidade e insert

**Risco:** dois POSTs concorrentes com mesmo `codigo` passam ambos pelo `ExisteCodigoAsync` antes de o primeiro chegar a `SaveChangesAsync`.
**Mitigação:** UK no banco como rede de segurança + tradução de `DbUpdateException` por `ConstraintName` no repositório (§5.2). Teste de integração concorrente em §8.1.

### Risco de divergência de contrato HTTP com o front

**Risco:** o `ListaFiliais` do front (`frontend/src/types/filial.ts`) só conhece `itens` + `total`; o backend retorna `pagina` + `tamanhoPagina` extras.
**Mitigação:** `System.Text.Json` aceita campos extras silenciosamente no TS — o front continua funcionando. FE-01 do card 204 fica responsável por opcionalmente ampliar o tipo TS. Sem breaking change.

---

## 10. Checklist de DoR para os devs

### 10.1 O que está pronto ao final deste ADR

- [x] Modelo de domínio fechado (campos, fábrica, invariantes, `RegistrarCriadoPor`).
- [x] Esquema final da tabela `filiais` (colunas + tipos + nullability).
- [x] Lista exaustiva de constraints e índices, com nome canônico e justificativa por CA.
- [x] Estratégia da migration: aditiva, nullable, sem backfill automático, checklist de Designer + Snapshot.
- [x] Contrato HTTP de POST e GET (request, response, headers, paginação, tabela de erros com slugs).
- [x] Política de normalização e duplicidade (pré-check + tradução de UK no repositório).
- [x] Política de auditoria e logs (`DefinirEvento` no handler, proibição de logar CNPJ).
- [x] Plano de testes mínimo com pirâmide concreta.

### 10.2 O que cada dev precisa ler antes de tocar código

- **Dev backend:** este ADR (todo), `docs/backlog/card-204-rf017-cadastro-de-filiais.md` (tasks BE-01..BE-17), ADR 0003 (padrão de slice), ADR 0006 (regras de migration), `backend/src/CarWash.Application/Clientes/Criar/` (referência completa), `backend/src/CarWash.Infrastructure/Persistence/Repositories/ClienteRepository.cs` (padrão de tradução de UK), `backend/src/CarWash.Domain/Entities/Cliente.cs` (padrão de `RegistrarCriadoPor`).
- **Dev frontend (somente FE-01):** este ADR §4.2 (contrato do GET) e `frontend/src/services/filialService.ts` (remover banner pendente quando GET subir).
- **QA:** este ADR §8 (plano de testes) e `card-204` §CA-204.* (critérios). MUST executar o E2E `criar filial + pré-confirmar agendamento`.

### 10.3 O que muda em arquivos existentes

| Arquivo | Mudança |
|---------|---------|
| `backend/src/CarWash.Domain/Entities/Filial.cs` | Adicionar `Codigo`, `Cnpj?`, `EnderecoCep`..`EnderecoUf`, `CriadoPorUsuarioId`, getter computado `Endereco?`, `RegistrarCriadoPor`. **Substituir** fábrica antiga pela nova assinatura. |
| `backend/src/CarWash.Infrastructure/Persistence/Configurations/FilialConfiguration.cs` | Adicionar propriedades planas de endereço (espelha `ClienteConfiguration`), CHECK `ck_filiais_codigo_formato`, dropar `uk_filiais_nome` por `uk_filiais_nome_lower`, adicionar UKs parciais de `codigo` e `cnpj`, índice `idx_filiais_cidade_uf`, `builder.Ignore(x => x.Endereco)`. |
| `backend/src/CarWash.Api/Endpoints/EndpointRouteBuilderExtensions.cs` | Adicionar `app.MapFiliais()` entre `MapClientes()` e `MapAgendamentos()`. |
| `backend/src/CarWash.Infrastructure/Persistence/CarWashDbContextModelSnapshot.cs` | Atualizado automaticamente pela nova migration. **Conferir diff.** |

### 10.4 O que se cria do zero

- `backend/src/CarWash.Application/Filiais/Criar/CriarFilialCommand.cs`
- `backend/src/CarWash.Application/Filiais/Criar/CriarFilialRequest.cs`
- `backend/src/CarWash.Application/Filiais/Criar/CriarFilialResponse.cs`
- `backend/src/CarWash.Application/Filiais/Criar/CriarFilialCommandValidator.cs`
- `backend/src/CarWash.Application/Filiais/Criar/CriarFilialHandler.cs`
- `backend/src/CarWash.Application/Filiais/Listar/ListarFiliaisQuery.cs`
- `backend/src/CarWash.Application/Filiais/Listar/ListarFiliaisHandler.cs`
- `backend/src/CarWash.Application/Filiais/Listar/ListaFiliaisResponse.cs` (`FilialResumoResponse` + envelope)
- `backend/src/CarWash.Application/Filiais/Common/EnderecoFilialRequest.cs` (ou reaproveitar `Clientes/Common/EnderecoRequest.cs` — preferir reaproveitar para zero duplicação de validador)
- `backend/src/CarWash.Application/Filiais/Common/FilialCodigoJaExisteException.cs`
- `backend/src/CarWash.Application/Filiais/Common/FilialCnpjJaExisteException.cs`
- `backend/src/CarWash.Application/Filiais/Common/FilialNomeJaExisteException.cs`
- `backend/src/CarWash.Application/Filiais/Persistence/IFilialRepository.cs` (`ExisteCodigoAsync`, `ExisteCnpjAsync`, `ExisteNomeAsync`, `AdicionarAsync`, `ObterPorIdAsync`, `ListarAsync`)
- `backend/src/CarWash.Infrastructure/Persistence/Repositories/FilialRepository.cs`
- `backend/src/CarWash.Api/Endpoints/Filiais/FiliaisEndpoints.cs`
- `backend/src/CarWash.Infrastructure/Persistence/Migrations/<timestamp>_AdicionaCadastroFilial.cs` (+ `.Designer.cs`)
- Testes unitários e de integração conforme §8.

### 10.5 O que NÃO muda

- `20260513114525_InitialSchema.cs` — **proibido editar** (ADR 0006).
- `AuditLogInterceptor.cs` — `Filial` já está em `EntidadesAuditaveis` (linha 22). Nenhuma alteração necessária.
- `ExceptionHandlingMiddleware.cs` — `ConflictException.Slug` já é traduzido para 409 com `type` correto.
- `Cliente.cs` / `ClienteRepository.cs` / `ClientesEndpoints.cs` — apenas referência, sem alteração.
- Slice de agendamento — `CalculadoraResumoAgendamento` já lida com filial inativa/inexistente.

---

## 11. Implicações operacionais

### Endpoints expostos no MVP após este card

```
POST   /api/v1/filiais          (RequireAuthorization)
GET    /api/v1/filiais          (RequireAuthorization)
```

### Cards derivados deste ADR

- **Card 206 — `PATCH /api/v1/filiais/{id}/status`** (L8): inativar/reativar filial. Reaproveita `RecursoInativoException` para 422. Abrir após homologação do card 204.
- **Card 207 — Tornar `filiais.codigo` `NOT NULL`**: depende de backfill manual. Migration única, sem alteração de contrato HTTP.

### Pontos a confirmar com o proprietário (premissa A1, herdados do card 204)

1. Convenção interna de `codigo` (ex.: `MTZ`, `FILIAL01`).
2. Default operacional de `celulasAtivas` para filiais novas.
3. Confirmação prática de L2: filiais podem ou não compartilhar CNPJ raiz.

---

## 12. Re-avaliação

Esta ADR deve ser revisitada quando:

- O card 207 for entregue (transição de `codigo` para `NOT NULL`) — atualizar §3 para refletir a UK full.
- RF-FUT003 chegar — atualizar §6 para incluir policy de autorização e CA-204.8 sair de "verificação adiada".
- O card 206 for entregue — atualizar §11 com a rota `PATCH /status` e o slug `recurso-inativo` no contrato.
- O volume de filiais crescer para a casa das centenas (improvável no MVP) — reabrir §7.3 (cache) e §7.2 (paginação keyset).
- Aparecer integração externa que dependa de CNPJ obrigatório (NF-e, fiscal) — promover L2 para obrigatório.

---

## 13. Referências

- DRP — RF017, RF018, RF019, RF020, RN009, RN010, CA001, CA007, CA008, CA011, RNF009.
- DAT §4.1 (módulo Filiais), §5 (PostgreSQL), §8.2 (governança de migrations), §9.1 (auditoria), §11 (RAT01, RAT03, RAT04).
- DVP-E §4.1 — P6 (capacidade rígida), P7 (conflito entre filiais).
- Card de backlog — [`../backlog/card-204-rf017-cadastro-de-filiais.md`](../backlog/card-204-rf017-cadastro-de-filiais.md).
- ADR 0001 — [`./0001-geracao-de-uuid-pela-aplicacao.md`](./0001-geracao-de-uuid-pela-aplicacao.md) (UUID app-side).
- ADR 0003 — [`./0003-minimal-api-cqrs-vertical-slices.md`](./0003-minimal-api-cqrs-vertical-slices.md) (padrão de slices).
- ADR 0006 — [`./0006-consolidacao-arvore-migrations-ef-core.md`](./0006-consolidacao-arvore-migrations-ef-core.md) (governança de migrations).
- Código de referência — `backend/src/CarWash.Application/Clientes/Criar/` (slice completo), `backend/src/CarWash.Infrastructure/Persistence/Repositories/ClienteRepository.cs` (tradução de UK violation), `backend/src/CarWash.Api/Endpoints/Clientes/ClientesEndpoints.cs` (padrão Minimal API + paginação inline).
