# Backlog — Card 203 / RNF003 + RF001 — Cobertura de testes de lockout de login (N tentativas)

> Status: refinamento do analista. Sem lacunas bloqueantes — execução direta.
> Rastreabilidade base: RNF003 (Segurança de acesso) + RF001 (autenticação) → UC001 → Módulo Autenticação (DAT §4.1) / Backend + Tests.
> Origem: o teste de integração `AuthFlowTests.cs`, que historicamente cobria o bloqueio temporário após N tentativas malsucedidas, foi removido porque referenciava entidades inexistentes (`User`/`Sessions`) no padrão atual do domínio (`Usuario`/`UsuarioSessao`). O substituto `AuthFlowEndToEndTests.cs` cobre login → refresh → logout → cookie, mas **não** cobre o lockout — deixando RNF003 sem proteção de regressão automatizada.

## Resumo executivo

Portar o cenário de bloqueio temporário após `LoginHandler.LimiteTentativasInvalidas`
falhas consecutivas (hoje 4 — ver `CarWash.Application/Auth/Login/LoginHandler.cs`)
para o padrão atual de testes de integração (Testcontainers + `CarWashWebApplicationFactory`),
acompanhando o estilo de `AuthFlowEndToEndTests.cs`. Cobre o caminho 401 → 401 → 401 →
**403 (bloqueio)** e a expiração do bloqueio após `DuracaoBloqueio` (15 minutos). Sem este
teste, a regra "tentativas 1..3 = 401; 4ª = 403 com lockout" pode regredir silenciosamente.

## User stories

### US-203.1 — Bloqueio após N tentativas malsucedidas
**Como** Administrador, **quero** que minha conta seja bloqueada temporariamente
após `LoginHandler.LimiteTentativasInvalidas` tentativas consecutivas com senha
incorreta **para que** um atacante não consiga força bruta de credenciais. (RNF003;
RF001.)

### US-203.2 — Mensagem unificada anti-enumeration
**Como** usuário legítimo, **quero** que mensagens de erro de credencial inválida
sejam consistentes (não revelando se o e-mail existe na base) **para que** o
sistema não vaze informação de existência de conta. (RNF003 + comentário do
`LoginHandler.Login` sobre `DummyPasswordHash`.)

### US-203.3 — Liberação automática após `DuracaoBloqueio`
**Como** Funcionário legítimo que errou a senha algumas vezes, **quero** que
minha conta seja desbloqueada automaticamente após o período de `DuracaoBloqueio`
**para que** eu não fique permanentemente travado do sistema sem precisar
acionar um Administrador. (RF001 — UX da autenticação.)

### US-203.4 — Cobertura de regressão automatizada
**Como** desenvolvedor do CarWash, **quero** um teste de integração contra
Postgres real que prove o ciclo "1..3 falhas → 401; 4ª falha → 403; tentativa
durante lockout → 403; após `DuracaoBloqueio` → 401/200" **para que**
regressões em `LoginHandler.HandleAsync` sejam detectadas antes do merge.
(RNF003; CA011.)

## Lacunas para decisão

- **L1 (não bloqueante)** — Como avançar o relógio de `DuracaoBloqueio`
  (15min) no teste sem `Thread.Sleep(15min)`? Opções:
  1. Injetar `IClock`/`ISystemClock` fake via `CarWashWebApplicationFactory`
     (verificar se já existe abstração na casa — provavelmente não para o
     handler de Auth).
  2. Manipular diretamente o campo `BloqueadoAte` do `Usuario` via repositório
     no teste (avanço manual de relógio aplicado).
  3. Não cobrir desbloqueio neste card (split em sub-card US-203.3) e
     começar pelo lockout em si.
  Default analista: **opção 2** para o MVP — mais simples, não exige refator
  do `LoginHandler`. Confirmar com arquiteto.

- **L2 (não bloqueante)** — A mensagem do 403 hoje é "Conta temporariamente
  bloqueada. Tente novamente em alguns minutos." (verificar
  `UsuarioBloqueadoException`). Teste deve asseverar a mensagem específica?
  Default: sim — qualquer mudança de texto deve ser uma decisão consciente.

## Critérios de aceite (Given/When/Then)

1. **CA-203.1 — Lockout após N falhas (RNF003, RF001):** Dado um usuário ativo
   com senha correta `S`, quando o cliente fizer
   `LoginHandler.LimiteTentativasInvalidas - 1` POSTs em `/api/v1/auth/login`
   com senha errada (cada um retorna 401 com mensagem unificada), então o
   próximo POST com senha errada retorna `403 Forbidden` com mensagem de
   conta bloqueada.
2. **CA-203.2 — Tentativa durante lockout (RNF003):** Dado um usuário no
   estado `Bloqueado`, quando enviar POST em `/api/v1/auth/login` (com senha
   correta **ou** incorreta), então o sistema responde `403 Forbidden` **antes**
   de chamar o verifier de senha (`UsuarioBloqueadoException` lançada cedo
   no handler).
3. **CA-203.3 — Mensagem unificada anti-enumeration (RNF003):** Dado um
   e-mail que não existe na base, quando enviar POST em `/api/v1/auth/login`,
   então a latência da resposta é comparável à de credencial inválida (via
   `DummyPasswordHash`) e a mensagem é a mesma — teste compara textos.
4. **CA-203.4 — Liberação após `DuracaoBloqueio` (RF001):** Dado um usuário
   bloqueado cujo `BloqueadoAte` está no passado (simulado por manipulação
   direta do repositório no teste, L1), quando enviar POST com senha correta
   em `/api/v1/auth/login`, então o sistema responde `200 OK` e zera o
   contador de tentativas.
5. **CA-203.5 — Reset de contador após sucesso (RF001):** Dado 2 falhas
   seguidas (não atinge limite), quando o usuário acertar a senha, então o
   contador volta a 0 — uma nova rodada de falhas começa do zero. Provado
   por: 2 falhas → sucesso → 3 falhas (limite-1) → 4ª falha → 403.
6. **CA-203.6 — Auditoria do bloqueio (RNF009):** Dado o estado pós-CA-203.1,
   quando inspecionar o log estruturado da última requisição, então há entrada
   com `EventoUsuarioBloqueado = "UsuarioContaBloqueada"` (constante já
   definida em `LoginHandler`). Assertivel via `ITestOutputHelper` + um
   `ILoggerProvider` de teste, ou via inspeção do repositório de auditoria.
7. **CA-203.7 — Cobertura sob Testcontainers:** O teste roda na collection
   `PostgresCollection` (igual a `AuthFlowEndToEndTests`), em DB real, com
   limpeza por fixture entre fatos.

## Tarefas — trilha Backend Tests (.NET)

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| TE-01 | Criar `backend/tests/CarWash.IntegrationTests/Endpoints/Auth/AuthLockoutTests.cs` na collection `PostgresCollection`, herdando o padrão de `AuthFlowEndToEndTests` (factory + JSON options + helpers de cadastro/login) | P | — |
| TE-02 | Caso 1 (CA-203.1): `Lockout_apos_N_tentativas_invalidas_retorna_403` — laço de 1..`LimiteTentativasInvalidas` POSTs e assertions de status code + mensagem | P | TE-01 |
| TE-03 | Caso 2 (CA-203.2): `Login_durante_lockout_retorna_403_mesmo_com_senha_correta` — pós-lockout, POST com senha correta também retorna 403 | PP | TE-02 |
| TE-04 | Caso 3 (CA-203.3): `Email_inexistente_retorna_mesma_mensagem_que_credencial_invalida` — anti-enumeration | P | TE-01 |
| TE-05 | Caso 4 (CA-203.4): `Lockout_expira_apos_duracao_bloqueio` — manipulação direta de `BloqueadoAte` via repositório (L1 opção 2) e POST com senha correta resolvendo em 200 | M | TE-01, L1 |
| TE-06 | Caso 5 (CA-203.5): `Sucesso_zera_contador_de_tentativas_invalidas` — sequência 2 falhas → sucesso → 3 falhas → 4ª falha → 403 | M | TE-01 |
| TE-07 | Caso 6 (CA-203.6): assert que o log estruturado contém `EventoUsuarioBloqueado` — via `ILoggerProvider` de teste ou inspeção da tabela de auditoria | M | TE-02 |
| TE-08 | Documentar no XML doc da classe: o quê cada teste protege e referência ao CA do DRP | PP | TE-02..TE-07 |

## Tarefas — trilha Backend de produção (opcional)

| ID | Tarefa | Esforço | Dep. |
|----|--------|---------|------|
| BE-01 | Avaliar se vale extrair `IClock`/`ISystemClock` para o módulo de Auth (refator do `LoginHandler` + `Usuario.RegistrarTentativaFalha`) — só se L1 = opção 1 | P | L1 |

> Nota: BE-01 fica **fora do escopo** deste card se L1 = opção 2 (default
> sugerido). Pode virar follow-up de teste de qualidade do código.

## Definition of Ready

- L1 decidida (opção de avanço de relógio escolhida).
- Acesso ao repositório `UsuarioRepository` via DI no teste mapeado.
- Constantes `LimiteTentativasInvalidas` e `DuracaoBloqueio` confirmadas no
  `LoginHandler` (atual: 4 e 15min — não mudar neste card).

## Definition of Done

- 6 testes verdes (CA-203.1 a CA-203.6) no pipeline de integração.
- Nenhuma alteração no `LoginHandler` ou em `Usuario` (a menos que L1 = opção 1
  seja escolhida e o arquiteto aprove o refator).
- Cobertura do `LoginHandler` no relatório de cobertura sobe (informativo, não
  bloqueia).
- Documentação atualizada se houver: `docs/arquitetura-backend.md` ou seção
  de Auth nos READMEs internos — referenciar este card no histórico.
- Code review aprovado por par do backend.

## Prioridade e estimativa

- **Prioridade:** Must para o MVP — RNF003 é cobertura de segurança e o
  `LoginHandler` já implementa o lockout em produção; ficar sem teste é
  risco de regressão. Subir prioridade se um pen-test detectar exposição.
- **Esforço total:** M (1 dev backend, ~1 dia).
- **Dependências externas:** decisão L1.
- **Bloqueia:** auditoria de segurança / Go do orientador para RNF003.

## Decisões do arquiteto (2026-05-25)

- **L1 (avanço de relógio) — CONFIRMADO opção 2: manipulação direta de `BloqueadoAte` via repositório no teste.** Não existe `IClock`/`ISystemClock`/`IDateTimeProvider` no projeto (grep limpo em `backend/src/`). `LoginHandler` usa `DateTime.UtcNow` direto. Introduzir uma abstração de relógio só para cobrir CA-203.4 é refator desproporcional ao ganho — toca `LoginHandler`, `Usuario.RegistrarTentativaFalha`, DI e qualquer outro consumidor de `UtcNow`. Para o MVP, **manipular `BloqueadoAte` via `IUsuarioRepository` no setup do teste** atende RNF003 com risco zero ao código de produção. BE-01 (extrair `IClock`) fica registrado como follow-up técnico opcional, fora do escopo deste card.
- **L2 (assertiva da mensagem do 403) — CONFIRMADO.** Teste deve asseverar literal de `UsuarioBloqueadoException`. Mudança de texto vira decisão consciente futura.
- **Dependência de migrations:** este card depende da Decisão 1 estar mergeada — a suíte de integração só sobe verde após consolidação para `Persistence/Migrations/`.

## Rastreabilidade resumida

| Rastreável | ID |
|------------|----|
| Problema (DVP-E §4.1) | — (não é problema de negócio; é cobertura de RNF) |
| Requisito (DRP §3) | RF001 (Must) |
| Requisito não-funcional (DRP §5) | RNF003 (Segurança), RNF009 (Observabilidade) |
| Regra de negócio (DRP §4) | — |
| Critério de aceite global (DRP §10) | CA011 (cobertura de testes de negócio para validações críticas) |
| Risco mitigado (DAT §11) | RAT03 (validação/segurança), risco de auth quebrar sem aviso |
| Módulo (DAT §4.1) | Autenticação |
| ADR base | ADR 0002 (Argon2id) — contexto de Auth |
