# ADR 0001 — Geração de UUID pela aplicação .NET

- **Status:** Aceita
- **Data:** 2026-05-13
- **Autores:** Guilherme Brogio (arquiteto técnico) — decisão registrada a pedido do CEO.
- **Escopo:** Backend `.NET 8` + PostgreSQL 16 (todas as tabelas com PK `UUID`).

---

## Contexto

O schema do CarWash (DB001) usa `UUID` como chave primária em todas as tabelas de domínio e técnicas. A geração desses identificadores pode ser delegada ao banco (`DEFAULT gen_random_uuid()`, dependente da extensão `pgcrypto`) ou produzida pela aplicação .NET antes do `INSERT`.

A decisão precisa ser tomada **antes** da migration inicial, pois afeta:

- A presença/ausência da extensão `pgcrypto`.
- A presença/ausência de `DEFAULT gen_random_uuid()` nas colunas `id`.
- O mapeamento EF Core (`.ValueGeneratedNever()` vs deixar o provider Npgsql configurar `defaultValueSql`).
- Cenários transacionais futuros (outbox/eventos, idempotência, testes determinísticos).

---

## Decisão

**Todos os UUIDs de chave primária são gerados pela aplicação .NET, antes do `INSERT`.**

Concretamente:

- Variante: **UUIDv4** (`Guid.NewGuid()` da BCL .NET). Ver §"Alternativas" sobre UUIDv7.
- Banco: **sem `DEFAULT gen_random_uuid()`** em qualquer coluna. Apenas `UUID NOT NULL` + `PRIMARY KEY`.
- Extensão: **`pgcrypto` não é criada**. A única extensão habilitada no MVP é `btree_gist` (necessária para a EXCLUDE constraint do RN011 — assunto diferente desta ADR).
- EF Core: toda configuração de entidade define `builder.Property(x => x.Id).ValueGeneratedNever();`.
- Construtor das entidades: `Id = Guid.NewGuid()` quando criadas em domínio; reidratação aceita `Id` via construtor.
- Seed técnico: UUIDs **fixos** e determinísticos para os registros iniciais (admin: `00000000-0000-0000-0000-000000000001`; filial Matriz: `00000000-0000-0000-0000-000000000010`; serviços: `00000000-0000-0000-0000-00000000010X`).

---

## Justificativa

1. **Controle para futuras implementações.** Com o `Id` disponível **antes** do `INSERT`, padrões como Outbox transacional, idempotência por correlação, retorno de URL/Location em endpoints REST, agregação de eventos de domínio e testes determinísticos ficam mais simples. O código aplica `agendamento.Id` em logs, eventos e payloads de retorno sem precisar de `RETURNING id` ou round-trips adicionais.

2. **Probabilidade de colisão é desprezível para o nosso volume.** UUIDv4 tem 122 bits aleatórios. Pelo paradoxo do aniversário, seria necessário gerar **~1 bilhão de UUIDs por segundo durante ~85 anos** para que a probabilidade de uma única colisão chegue a 50%. O CarWash, mesmo em cenário otimista de 10 anos com 1 milhão de operações diárias, gera ordens de magnitude menos UUIDs — o risco de colisão é matemático mas operacionalmente nulo. A UNIQUE constraint da PK ainda funciona como rede de segurança final.

3. **Sistema pequeno e interno.** Não há requisito de geração distribuída em alta escala que justificaria coordenação ou IDs sequenciais centralizados.

4. **Independência do SGBD.** A geração em aplicação não amarra o domínio a uma função específica do PostgreSQL. Embora o DAT v1.0 fixe o PostgreSQL como SGBD oficial, eliminar uma dependência de extensão simplifica testes (in-memory providers, SQLite) e portabilidade futura.

5. **Schema mais limpo.** Sem `pgcrypto`, a migration inicial tem menos cerimônia e nenhuma dependência sobre permissões de extensão (alguns provedores cloud restringem `CREATE EXTENSION pgcrypto`).

---

## Consequências

### Positivas

- IDs determinísticos em seeds e fixtures de teste — facilita debugging e reprodução de bugs.
- Padrão Outbox e domain events ficam triviais (ID já existe quando o evento é emitido).
- Endpoints REST podem retornar `201 Created` com `Location: /agendamentos/{id}` sem buscar o ID gerado.
- Logs e tracing referenciam o ID desde o início da transação.
- Schema do banco mais simples (sem `pgcrypto`, sem `DEFAULT`).
- Independência maior do SGBD para testes.

### Negativas

- **Inserts manuais (psql, scripts ad-hoc)** precisam fornecer o `id` explicitamente. Sem default no banco.
- Toda configuração EF Core precisa lembrar de `.ValueGeneratedNever()`. Sem isso, o Npgsql provider tenta usar `gen_random_uuid()` ou `Guid.Empty` (comportamentos diferentes por versão). Mitigado por convenção e revisão de PR.
- UUIDv4 é **aleatório**: índice BTREE da PK sofre com fragmentação por inserts não sequenciais. Em volumes muito altos isso vira hotspot. Para o CarWash o volume é baixo demais para sentir — registrado como follow-up se houver hotspot futuro (migrar para UUIDv7 sem mudar schema).

---

## Alternativas consideradas

### A. `DEFAULT gen_random_uuid()` no banco (extensão `pgcrypto`)

Banco gera o UUID. Aplicação envia `INSERT` sem `id` e usa `RETURNING id`.

- **Prós:** menor risco de esquecimento; default declarativo no schema.
- **Contras:** ID só existe após `INSERT` (complica outbox, domain events, retorno HTTP); requer round-trip ou `RETURNING`; depende de extensão `pgcrypto` (pode ser restrita em ambientes gerenciados); amarra schema ao Postgres.
- **Veredito:** Rejeitada. Os ganhos do controle em aplicação superam a comodidade do default.

### B. UUIDv7 gerado em aplicação (`UUIDNext` ou `Medo.Uuid7`)

UUIDv7 inclui timestamp Unix de 48 bits nos bits altos. Mantém localidade temporal — inserts próximos no tempo ficam próximos no índice BTREE.

- **Prós:** melhor localidade de cache em índice BTREE; ordenação cronológica natural.
- **Contras:** dependência de biblioteca externa; novidade no .NET (sem suporte BCL — chegará no .NET 9+); cenários de teste menos previsíveis (timestamp embutido); rastreabilidade do timestamp pode ser indesejada (pequena fuga de informação).
- **Veredito:** Não adotada **agora**, mas mantida em backlog. Migrar para UUIDv7 no futuro **não requer migração de schema** — só troca da função de geração. Reservado para o dia em que aparecer hotspot real no índice da PK.

### C. ULID

Identificador alternativo (Crockford base32, 26 chars, ordenável). Não é nativo no Postgres.

- **Prós:** ordenável temporalmente; legível.
- **Contras:** não há tipo nativo no Postgres — armazenado como `TEXT` ou `BYTEA`, perdendo a otimização do tipo `UUID`; sem suporte no `Guid` da BCL; ecossistema fragmentado.
- **Veredito:** Rejeitada. Não compensa abandonar o tipo `UUID` nativo do Postgres.

### D. `bigint` autoincrement / `IDENTITY`

Inteiro sequencial gerado pelo banco.

- **Prós:** menor; mais rápido em BTREE; humanamente legível.
- **Contras:** expõe contagem (info leak — atacante pode estimar volume), exige round-trip para o ID, dificulta integração futura entre instâncias/serviços, e não casa com o padrão de UUID adotado pelo restante da indústria moderna para entidades de negócio.
- **Veredito:** Rejeitada. UUID é o padrão do DAT.

---

## Implicações operacionais

- **DB001:** nenhuma extensão `pgcrypto`. Apenas `btree_gist`. Migration inicial não usa `defaultValueSql: "gen_random_uuid()"`.
- **Code review:** PRs que adicionam entidades precisam ter `.ValueGeneratedNever()` checado. Pode entrar como regra Roslyn no futuro.
- **Testes:** Fixtures podem usar `Guid.Parse("...")` para IDs determinísticos sem qualquer adaptação.
- **Seeds:** UUIDs fixos (determinísticos) para o conjunto inicial do MVP — admin, filial Matriz e serviços de exemplo.
- **Inserts manuais (DBA / psql):** **sempre** fornecer `id` na lista de colunas do `INSERT`.

---

## Re-avaliação

Esta ADR deve ser revisitada quando:

- Surgir hotspot mensurável no índice da PK de qualquer tabela de alta inserção (revisar UUIDv7).
- O sistema deixar de ser exclusivamente PostgreSQL (revalidar se a decisão segue válida).
- Houver requisito legal/compliance que exija identificadores de outro tipo.
- O .NET BCL passar a oferecer UUIDv7 nativo (avaliar migração).

---

## Referências

- [RFC 4122 — UUID v4](https://www.rfc-editor.org/rfc/rfc4122)
- [RFC 9562 — UUID v7](https://www.rfc-editor.org/rfc/rfc9562)
- [Paradoxo do aniversário aplicado a UUIDv4](https://en.wikipedia.org/wiki/Universally_unique_identifier#Collisions)
- DAT v1.0 §4.2 (Encapsulamento) e §5 (Persistência) — Guilherme Brogio.
