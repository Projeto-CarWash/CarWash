# Desenho técnico — RF019 Seleção obrigatória de filial na criação do agendamento (card 142)

- Branch: `feat/rf019-filial-obrigatoria-agendamento` (base: `feat/rf018-celulas-ativas-filial`).
- Autor do desenho: Arquiteto de Software Sênior (CarWash).
- Status: pronto para implementação (somente ADR; nenhum código de produção alterado nesta entrega).
- DAT/DRP referenciados: DAT §3.1 (camadas), DAT §4.1 (módulo Agenda), DAT §4.2 (RN no backend), DAT §9.1 (observabilidade/eventos), DRP RF007/RF015/RF019/RF020, DRP RN010/RN011, DRP CA006/CA007/CA011, RAT01–RAT05, RF-FUT003 (RBAC).
- Referências canônicas (padrão a seguir):
  - `Application/Agendamentos/Common/CalculadoraResumoAgendamento.cs` (validação compartilhada dos 3 fluxos).
  - `Application/Agendamentos/Common/CapacidadeFilialEsgotadaException.cs` (receita de `ConflictException` → 409 + slug sem novo `catch`).
  - `Application/Filiais/AlterarCelulasAtivas/AlterarCelulasAtivasHandler.cs` (padrão de auditoria explícita do RF018).
  - `Api/Middleware/ExceptionHandlingMiddleware.cs` (mapeamento exceção → ProblemDetails + correlationId).
  - `Application/Abstractions/IAuditLogger.cs`, `ICurrentRequestContext.cs`, `Infrastructure/Auditing/AuditLogger.cs`.

> **Observação importante sobre o estado atual.** O RF019 está ~80% implementado. `FilialId` já é obrigatório no command/validator, a existência/estado da filial já é validada em `CalculadoraResumoAgendamento.GarantirFilialAsync` (compartilhado por criação, pré-confirmação e confirmação), e o schema já tem FK + NOT NULL + índice `(FilialId, Inicio)`. **Não tocaremos domínio nem schema/migration.** O trabalho é cirúrgico: alinhar mensagens/status HTTP ao card e adicionar a auditoria de erro com `motivo`.

---

## 1. Gap analysis (estado atual vs card 142) — confirmado por leitura de código

| Item do card | Estado atual (arquivo:trecho) | Gap |
|---|---|---|
| `FilialId` obrigatório no payload | `CriarAgendamentoRequest.FilialId` (Guid); validator `RuleFor(x => x.FilialId).NotEmpty()` — `Application/Agendamentos/Criar/CriarAgendamentoCommandValidator.cs`. Mesmo em `PreConfirmar*` e `Confirmar*`. | **OK** (presença coberta). Só muda a **mensagem**. |
| 400 ausência → "Selecione uma filial válida para prosseguir." | Mensagem atual: `"Filial é obrigatória para o agendamento (RF019)."` (`CriarAgendamentoCommandValidator.cs`). | **DIVERGE — mensagem.** |
| 400 formato UUID inválido | Binder `[FromBody]` com `Guid` → `BadHttpRequestException` → `ExceptionHandlingMiddleware.ClassificarBadRequest` (`ExceptionHandlingMiddleware.cs`, regex `RegexReadBody` + `ExtrairCampoDeJsonException`) → 400 `invalid-request` + `errors[filialId]` = "Valor inválido para o campo informado." | **OK por comportamento** (já 400). Documentar; **não customizar**. |
| 404 inexistente → "Filial não encontrada." | `GarantirFilialAsync` lança `NotFoundException("Filial informada não foi encontrada.")` → 404 slug `not-found` (`CalculadoraResumoAgendamento.cs` ~L181; middleware catch `NotFoundException`). | **DIVERGE — mensagem.** |
| 409 inativa → "A filial selecionada está inativa e não pode receber novos agendamentos." | `GarantirFilialAsync` lança `RecursoInativoException("A filial selecionada está inativa e não aceita agendamentos.")` → **422** slug `recurso-inativo` (`CalculadoraResumoAgendamento.cs` ~L185; middleware catch `RecursoInativoException` ~L120). | **DIVERGE — status (422→409) E mensagem.** Gap principal. |
| 201 sucesso → "Agendamento criado com sucesso." | `AgendamentoResponseFactory.MensagemSucesso = "Agendamento criado com sucesso."` no campo `Mensagem`. | **OK** (texto idêntico). |
| 401 → "Autenticação obrigatória..." | `grupo.RequireAuthorization()` (`AgendamentosEndpoints.cs`). Mensagem do challenge é do middleware de auth, não do nosso ProblemDetails. | **OK por contrato** (401 garantido). Mensagem não controlada por este slice. |
| 403 → "Você não possui permissão..." | Nenhuma policy de perfil hoje (só `RequireAuthorization()`). | **Intencionalmente não implementar** (ver Decisão 8). Contrato futuro RF-FUT003. |
| 500 → "Não foi possível concluir o agendamento no momento. Tente novamente." | Catch-all do middleware retorna `MensagemErroInterno = "Não foi possível concluir a operação no momento. Tente novamente."` (genérico, não específico de agendamento). | **DIVERGE levemente** — ver Decisão 6/Nota. Mantemos a genérica (decisão justificada). |
| Persistir `filial_id` + vínculo único 1→1 | `AgendamentoConfiguration`: `FilialId IsRequired()` + FK `HasOne<Filial>().HasForeignKey(x => x.FilialId)` + índice `(FilialId, Inicio)`. | **OK.** |
| Log sucesso/erro com traceId/agendamentoId/filialId/usuarioId/dataHoraUTC/resultado | Sucesso: `ILogger` no endpoint e no handler com TraceId/AgendamentoId/FilialId/UsuarioId. `Agendamento` está em `EntidadesAuditaveis` do `AuditLogInterceptor` (INSERT auto-auditado). | **Parcial** — falta `dataHoraUTC`/`resultado` explícitos e **falta auditoria de ERRO com `motivo=filial_*`**. |

**Conclusão:** os gaps são (a) 3 strings de mensagem, (b) 1 mudança de status (422→409) sem contaminar veículo/cliente/serviço, (c) auditoria de erro com `motivo` por tipo de falha de filial. Tudo isolável dentro de `GarantirFilialAsync` + 1 exceção nova + 1 constante de mensagens + ajustes de teste.

---

## 2. Decisões (1–8)

### Decisão 1 — 409 para filial inativa sem quebrar veículo/cliente/serviço (gap principal)

**Decisão:** criar `FilialInativaException : ConflictException` com slug `filial-inativa` e a mensagem exata do card, lançada **apenas** em `GarantirFilialAsync`. Manter `RecursoInativoException` (422) para veículo/cliente/serviço/responsável inativos.

**Justificativa:**
- `ConflictException` já carrega `Slug` e o middleware já tem o `catch (ConflictException)` → 409 + `type=https://carwash/errors/<slug>` (`ExceptionHandlingMiddleware.cs`). `CapacidadeFilialEsgotadaException` provou exatamente essa receita: herda `ConflictException` e **não precisou de novo `catch`**. Replicamos.
- A filial é a única dependência cujo card pede 409. Isolar a exceção na filial preserva 422 para os demais recursos (RAT03 — defesa server-side por recurso, sem regressão).
- Slug próprio (`filial-inativa`) é distinguível dos outros 409 do domínio (`agendamento-conflito-veiculo`, `capacidade-filial-esgotada`, `idempotencia-conflitante`), seguindo o mesmo princípio dos "três 409 com type distinto" já testado em `ConfirmarAgendamentoEndpointTests`.

**Alternativas descartadas:**
- (a) Adicionar um parâmetro de status em `RecursoInativoException`: vaza concern HTTP para uma exceção semântica de domínio/aplicação e arriscaria mudar os demais recursos por engano. Descartada.
- (b) Tratar no middleware por inspeção de mensagem: frágil (acopla a string), anti-padrão. Descartada.

**Impacto em testes (mudança de 422→409 na filial inativa):**
- `CriarAgendamentoEndpointTests.POST_com_filial_inativa_retorna_422` (L211–230): **quebra** — passa a esperar 409 + slug `filial-inativa`.
- `PreConfirmarAgendamentoEndpointTests.POST_filial_inativa_retorna_422` (L155–174): **quebra** — idem.
- `CriarAgendamentoHandlerTests.Filial_inativa_lanca_RecursoInativo` (L113–121): **quebra** — passa a esperar `FilialInativaException`.
- `ConfirmarAgendamentoEndpointTests`: **não testa filial inativa** hoje (confirmado por grep) — novo 409 não colide com os "três 409" já assertados (divergência/idempotência/veículo). Adicionar um teste de filial inativa no confirmar é opcional, mas recomendado (§7).
- Veículo/cliente/serviço inativos permanecem 422 (`POST_veiculo_inativo_retorna_422`, `POST_servico_inativo_retorna_422`, etc.) — **não quebram**.

### Decisão 2 — 404 inexistente: mensagem "Filial não encontrada."

**Decisão:** trocar a mensagem de `GarantirFilialAsync` de `"Filial informada não foi encontrada."` para `"Filial não encontrada."` (string exata do card). Centralizar em constante.

**Justificativa:** a mensagem é **exclusiva da filial** (cada recurso tem sua própria string em `GarantirVeiculoAsync`/`GarantirClienteAsync`/etc.) — confirmado por leitura: não há reúso. Coincide com `AlterarCelulasAtivasHandler.MensagemNaoEncontrado = "Filial não encontrada."` (RF018), então usamos a **mesma frase** já consolidada no projeto. Mantemos uma constante local (`MensagensFilialAgendamento.NaoEncontrada`) para evitar string mágica e facilitar o teste.

**Impacto em testes:** os testes de 404 inexistente (`POST_filial_inexistente_retorna_404` em Criar L194 e PreConfirmar L137) só checam `StatusCode == NotFound`, **não** a mensagem — **não quebram**. `CriarAgendamentoHandlerTests.Filial_inexistente_lanca_NotFound` (L101) só checa o tipo `NotFoundException` — **não quebra**.

### Decisão 3 — 400 ausência: mensagem do validator "Selecione uma filial válida para prosseguir."

**Decisão:** trocar a mensagem do `RuleFor(x => x.FilialId).NotEmpty()` nos **três** validators (`CriarAgendamentoCommandValidator`, `PreConfirmarAgendamentoCommandValidator`, `ConfirmarAgendamentoCommandValidator`) para `"Selecione uma filial válida para prosseguir."`.

**Justificativa:** alinha a UX ao card. A mensagem nova cobre semanticamente tanto ausência quanto `Guid.Empty` (formato vazio). Aplicar nos três validators mantém o contrato consistente em todos os caminhos que criam agendamento (vertical slices independentes, sem abstração compartilhada de validator — fiel ao padrão atual).

**Impacto em testes:**
- `CriarAgendamentoCommandValidatorTests.Filial_vazia_falha_RF019` (L18–26): **quebra** — a asserção `e.ErrorMessage.Contains("RF019")` (L26) falha, pois a nova mensagem não cita "RF019". A asserção de `PropertyName` (L24) continua válida. QA deve trocar a asserção de mensagem para `Contains("Selecione uma filial válida")`. A tag "RF019" permanece no comentário XML do validator (rastreabilidade), não na mensagem ao usuário.
- Verificar validators de `PreConfirmar`/`Confirmar`: se houver teste unitário assertando a mensagem antiga, ajustar (grep não encontrou asserção de string nesses, mas o QA deve revalidar ao tocar os três).

### Decisão 4 — 400 formato UUID inválido: comportamento do binder (não customizar)

**Decisão:** **não** customizar. Documentar o comportamento real (como feito no RF018 com decimal/string).

**Comportamento real (confirmado em `ExceptionHandlingMiddleware.cs`):** com `[FromBody] CriarAgendamentoRequest` e `FilialId` do tipo `Guid`, um valor não-UUID (ex.: `"filialId": "abc"`) faz o `System.Text.Json` falhar a desserialização → o framework lança `BadHttpRequestException` ("Failed to read parameter ... from the request body as JSON") com `InnerException` `JsonException` cujo `Path = "$.filialId"`. O middleware:
1. casa `RegexReadBody`;
2. extrai o campo via `ExtrairCampoDeJsonException` → `filialId`;
3. responde **400** `application/problem+json`, `type=https://carwash/errors/invalid-request`, `title="Corpo da requisição inválido. Verifique o JSON e tente novamente."`, `errors: { "filialId": ["Valor inválido para o campo informado."] }`, com `correlationId`.

**Justificativa:** o card pede status 400 para formato inválido — já atendido. A mensagem do card ("Selecione uma filial válida para prosseguir.") é a do validator de **presença** (campo ausente / `Guid.Empty`), que só é alcançada quando o JSON desserializa. Strings não-UUID nunca chegam ao validator (falham antes, no binder). Customizar exigiria binder custom ou mudar `FilialId` para `string` + parse manual — complexidade desproporcional, contra YAGNI e contra o padrão já documentado no RF018. **Documentamos a divergência de mensagem como aceitável** (o cliente ainda recebe 400 + campo `filialId` apontado).

### Decisão 5 — Auditoria com `motivo=filial_*` (observabilidade)

**Decisão:** registrar a auditoria do **caminho de falha de filial dentro de `GarantirFilialAsync`**, imediatamente antes de lançar a exceção, via `IAuditLogger` + `ILogger` estruturado — opção (a)/(c) combinadas do enunciado. No **sucesso**, manter o `IAuditLogger`/INSERT automático (Agendamento já está em `EntidadesAuditaveis`) e enriquecer o log estruturado do handler com `dataHoraUTC` e `resultado`.

**Onde e como:**
- `CalculadoraResumoAgendamento` passa a receber `IAuditLogger` e `ILogger<CalculadoraResumoAgendamento>` por construtor (DI já registra ambos; é uma dependência scoped/singleton-friendly como nos handlers do RF018). `ICurrentRequestContext` fornece `CorrelationId` e `UsuarioId` ao `AuditLogger` automaticamente — não precisamos passar manualmente.
- Em `GarantirFilialAsync`, antes de cada `throw`, logar o evento de auditoria `AgendamentoFilialRejeitada` com `dados = { motivo, filialId }` e um `ILogger.LogWarning` estruturado com `TraceId`/`FilialId`/`UsuarioId`/`Motivo`. Valores de `motivo`:
  - `filial_inexistente` → antes do `NotFoundException`.
  - `filial_inativa` → antes do `FilialInativaException`.
- `filial_ausente` e `filial_invalida` ocorrem **antes** de `GarantirFilialAsync` (no validator e no binder, respectivamente). Para esses dois:
  - `filial_ausente`: logar no `EnsureValid`/no endpoint quando o `ValidationException` contém a chave `filialId` — porém, para manter mudança cirúrgica e evitar varrer o dicionário de erros, **registramos via `ILogger.LogWarning` no próprio validator não é possível** (validators não têm logger). Decisão: capturar `filial_ausente`/`filial_invalida` por **enriquecimento no log estruturado do endpoint** já existente no `CriarAsync`/`PreConfirmarAsync`/`ConfirmarAsync` quando a validação falha — adicionamos um `LogWarning` no ponto onde `ValidationProblems.EnsureValid` lançaria, **ou** aceitamos que esses dois motivos fiquem cobertos pelo log padrão de 400 do middleware (`invalid-request`). **Recomendação:** cobrir `filial_inexistente` e `filial_inativa` com auditoria explícita (são decisões de negócio server-side, alto valor de auditoria); tratar `filial_ausente`/`filial_invalida` como erros estruturais já logados pelo fluxo de 400 (não-auditáveis em `audit_logs`, apenas log de aplicação) — coerente com o RF018, que **não** audita falhas de validação estrutural, só mudanças de estado.

**Justificativa:**
- Lançar a exceção e enriquecer no middleware (opção b) exigiria o middleware conhecer `motivo` por mensagem/slug — acoplamento frágil e mistura concerns. Descartada.
- Logar no ponto de falha (a) mantém o `motivo` exatamente onde a decisão é tomada, sem poluir os handlers (`CriarAgendamentoHandler`/`Confirmar`/`PreConfirmar` não mudam — todos delegam a `GarantirFilialAsync`). É o ponto único compartilhado pelos três fluxos → auditoria consistente sem duplicação.
- Reusa o padrão do RF018 (`AlterarCelulasAtivasHandler`: `IAuditLogger.LogAsync(evento, entidade, entidadeId, dados)` + `ILogger.LogInformation/Warning`). Sem novas abstrações (RAT — simplicidade).

**Nota sobre "não expor detalhes internos":** o `motivo` e o `usuarioId` vão **apenas** para `audit_logs`/log de aplicação. A resposta HTTP continua sendo o ProblemDetails com a mensagem do card — nunca expomos `motivo` no corpo. `AuditDataMasker.Mask` já protege dados sensíveis.

### Decisão 6 — Envelope de resposta de sucesso

**Decisão:** **não** adotar o envelope `{ message, data, traceId }` do card. Tratar o exemplo do card como **ilustrativo** e manter `AgendamentoResponse` plano, garantindo que `filialId`, `Mensagem` (= "Agendamento criado com sucesso.") e `TraceId` já estão presentes.

**Justificativa:**
- `AgendamentoResponse` já é semanticamente equivalente: `Mensagem` ≡ `message`, `TraceId` ≡ `traceId`, e os campos do `data` (`id`, `filialId`, `clienteId`, `veiculoId`, `status`, `inicio`) já existem no nível raiz. O `correlationId`/traceId também vai no header via `CorrelationIdMiddleware`.
- Adotar o envelope quebraria o **contrato de TODOS os caminhos** que retornam `AgendamentoResponse` (criação direta RF007 + confirmação RF015 via `AgendamentoResponseFactory.Montar`), com impacto em frontend e em muitos testes de integração que leem `corpo.GetProperty("inicio"/"fim"/"itens"/...)` no nível raiz (ex.: `CriarAgendamentoEndpointTests.POST_multiplos_servicos_retorna_201_com_totais_somados`, `POST_janelas_adjacentes...`). Custo alto, ganho nulo — viola YAGNI e o escopo cirúrgico do card.
- O card 142 é sobre **filial obrigatória**, não sobre redesenho do contrato de resposta. Mudar o envelope seria escopo de um card transversal de API (escalar ao PO/PM se desejado).

**Sobre a mensagem de 500:** mantemos a genérica `MensagemErroInterno`. Criar uma mensagem de 500 específica de agendamento exigiria um catch dedicado por rota (anti-padrão; o middleware é global por design e nunca vaza stack trace). O texto genérico cumpre o requisito de não expor detalhes internos (DAT §9.1) — divergência textual aceitável e documentada.

### Decisão 7 — Endpoint `POST /api/v1/agendamentos` e cobertura em todos os caminhos

**Confirmação por leitura (`AgendamentosEndpoints.cs`):**
- `POST /api/v1/agendamentos/` → `CriarAsync` → `ICommandHandler<CriarAgendamentoCommand, AgendamentoResponse>` = `CriarAgendamentoHandler` (marcado `[Obsolete]`, mas **mapeado e funcional** — mantido para integrações/testes por decisão do ADR 0004 do RF015).
- `POST /api/v1/agendamentos/pre-confirmacao` → `PreConfirmarAsync` → `PreConfirmarAgendamentoHandler`.
- `POST /api/v1/agendamentos/confirmar` → `ConfirmarAsync` → `ConfirmarAgendamentoHandler` (fluxo canônico do frontend).

**Garantia de cobertura:** os **três** handlers chamam `CalculadoraResumoAgendamento.CalcularAsync`, que chama `GarantirFilialAsync`. Logo, **toda** rota que cria/valida agendamento passa pela validação de filial (existência + estado + 404/409 + auditoria). As mudanças das Decisões 1, 2 e 5 são feitas **uma única vez** em `GarantirFilialAsync` e propagam para criação direta, pré-confirmação e confirmação simultaneamente. A Decisão 3 (mensagem do validator) é feita nos três validators.

**Nota:** o card aponta `POST /api/v1/agendamentos`. Esse endpoint existe e roteia para o handler `[Obsolete]`. **Não removemos nem alteramos o `[Obsolete]`** — o card 142 valida filial, não decide a depreciação. A validação de filial fica garantida nos dois mundos (direto + confirmar) sem divergência, exatamente porque o concern está centralizado.

### Decisão 8 — 403 "permissão para criar agendamentos"

**Decisão:** **não** introduzir policy de perfil nem 403 por RBAC agora. Manter apenas `RequireAuthorization()` (401 para não-autenticado). Documentar o 403 como **contrato futuro (RF-FUT003 — RBAC)**.

**Justificativa:** segue exatamente a decisão do RF018 ("sem Admin agora"). No MVP todos os perfis autenticados têm acesso completo (DAT — RBAC é pós-MVP). Introduzir policy agora seria abstração prematura sem requisito ativo (YAGNI / RAT). A arquitetura já mantém autenticação desacoplada de autorização (`RequireAuthorization()` é o ponto de extensão), então o 403 entra no futuro sem reescrita. O endpoint **documenta** `ProducesProblem(403)` (já presente em pre-confirmação/confirmação) como contrato reservado.

---

## 3. Constantes de mensagem propostas (strings exatas do card)

Criar `Application/Agendamentos/Common/MensagensFilialAgendamento.cs` (constantes centralizadas — evita string mágica e facilita teste):

```csharp
public static class MensagensFilialAgendamento
{
    // 400 (presença/vazio) — usada nos três validators (RF019).
    public const string Obrigatoria = "Selecione uma filial válida para prosseguir.";

    // 404 (inexistente) — usada em GarantirFilialAsync. Mesma frase do RF018.
    public const string NaoEncontrada = "Filial não encontrada.";

    // 409 (inativa) — usada em FilialInativaException.
    public const string Inativa =
        "A filial selecionada está inativa e não pode receber novos agendamentos.";
}
```

Motivos de auditoria (constantes para evitar divergência de string):

```csharp
public static class MotivosFalhaFilial
{
    public const string Ausente = "filial_ausente";
    public const string Invalida = "filial_invalida";
    public const string Inexistente = "filial_inexistente";
    public const string Inativa = "filial_inativa";
}
```

Eventos de auditoria (seguindo o naming `EventoAuditoria` do RF018):
- Sucesso: já coberto pelo INSERT auto-auditado (`Agendamento` em `EntidadesAuditaveis`). Evento do interceptor definido por `ICurrentRequestContext.DefinirEvento("AgendamentoCriado")` — **opcional** adicionar nos handlers se quisermos nomear o evento; hoje não definem. Recomendação: definir `"AgendamentoCriado"` no `ConfirmarAgendamentoHandler.PersistirAsync` e no `CriarAgendamentoHandler` antes do `Adicionar*` (mudança mínima, melhora o `audit_logs.evento`).
- Falha de filial: `"AgendamentoFilialRejeitada"`, entidade `"Agendamento"`, `entidadeId = null` (ainda não há id), `dados = { motivo, filialId }`.

---

## 4. Mapeamento de exceções → status HTTP (após o RF019)

| Exceção | Slug (`type`) | Status | Quando | catch necessário |
|---|---|---|---|---|
| `ValidationException` (filialId vazio/ausente) | `validation-error` | 400 | Validator `NotEmpty` falha (msg `MensagensFilialAgendamento.Obrigatoria`) | já existe |
| `BadHttpRequestException` (UUID malformado) | `invalid-request` | 400 | Binder JSON falha; `errors[filialId]` | já existe |
| `NotFoundException` (msg `Filial não encontrada.`) | `not-found` | 404 | `GarantirFilialAsync`: filial não existe | já existe |
| **`FilialInativaException` (NOVA : ConflictException)** | **`filial-inativa`** | **409** | `GarantirFilialAsync`: `filial.Ativa == false` | **reusa `catch (ConflictException)` — SEM novo catch** |
| `RecursoInativoException` (veículo/cliente/serviço/responsável) | `recurso-inativo` | 422 | demais recursos inativos | já existe — **inalterado** |
| `AgendamentoConflitanteException` | `agendamento-conflito-veiculo` | 409 | RN011 (conflito de veículo) | já existe |
| `CapacidadeFilialEsgotadaException` | `capacidade-filial-esgotada` | 409 | RF008/RF018 capacidade | já existe |
| catch-all | `internal-error` | 500 | erro não tratado | já existe |

Ponto-chave: **nenhuma alteração no `ExceptionHandlingMiddleware`** — `FilialInativaException` cai no `catch (ConflictException)` existente, exatamente como `CapacidadeFilialEsgotadaException`.

---

## 5. Arquivos a criar e a editar (caminhos absolutos)

### 5.1 Criar

1. `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Common/FilialInativaException.cs`
   - `sealed class FilialInativaException : ConflictException` com `MensagemPadrao = MensagensFilialAgendamento.Inativa`, `SlugPadrao = "filial-inativa"`. Espelho de `CapacidadeFilialEsgotadaException`.
2. `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Common/MensagensFilialAgendamento.cs`
   - Constantes da §3 (`Obrigatoria`, `NaoEncontrada`, `Inativa`).
3. `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Common/MotivosFalhaFilial.cs`
   - Constantes de `motivo` da §3 (pode ser aninhado no arquivo acima se preferir 1 arquivo; manter separado é mais limpo).

### 5.2 Editar

4. `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Common/CalculadoraResumoAgendamento.cs`
   - `GarantirFilialAsync`: trocar `NotFoundException("Filial informada não foi encontrada.")` → `NotFoundException(MensagensFilialAgendamento.NaoEncontrada)`; trocar `RecursoInativoException(...)` → `FilialInativaException()`.
   - Injetar `IAuditLogger` + `ILogger<CalculadoraResumoAgendamento>` no construtor; emitir auditoria/log de `filial_inexistente` e `filial_inativa` **antes** de cada throw (Decisão 5).
5. `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Criar/CriarAgendamentoCommandValidator.cs`
   - Mensagem do `RuleFor(x => x.FilialId).NotEmpty()` → `MensagensFilialAgendamento.Obrigatoria`.
6. `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/PreConfirmar/PreConfirmarAgendamentoCommandValidator.cs`
   - Idem (alinhar mensagem do `FilialId`).
7. `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Confirmar/ConfirmarAgendamentoCommandValidator.cs`
   - Idem (alinhar mensagem do `FilialId`).
8. (Opcional, recomendado) `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Confirmar/ConfirmarAgendamentoHandler.cs` e `/home/gbrogio/university/carwash/backend/src/CarWash.Application/Agendamentos/Criar/CriarAgendamentoHandler.cs`
   - `ICurrentRequestContext.DefinirEvento("AgendamentoCriado")` antes do `Adicionar*` (nomeia o evento do INSERT auto-auditado) e enriquecer o `LogInformation` de sucesso com `DataHoraUtc`/`Resultado="criado"`. **Sem mudança de comportamento**, só observabilidade.

### 5.3 Verificação de DI

9. `/home/gbrogio/university/carwash/backend/src/CarWash.Infrastructure/DependencyInjection.cs` (ou onde `CalculadoraResumoAgendamento` é registrada — confirmar)
   - Confirmar que `CalculadoraResumoAgendamento` é resolvível com as novas dependências (`IAuditLogger`, `ILogger<>`). `IAuditLogger` já é registrado (RF018). **Provável zero alteração**, apenas verificar o registro da calculadora (hoje recebe só `IAgendamentoCatalogoRepository`).

**Não tocar:** `ExceptionHandlingMiddleware.cs`, `Agendamento.cs` (domínio), `AgendamentoConfiguration.cs` (schema/FK/índice), nenhuma migration, `RecursoInativoException.cs`, validators/handlers de veículo/cliente/serviço/responsável.

---

## 6. Ordem de implementação (commits convencionais)

1. `feat(agendamentos): adiciona FilialInativaException (409) e constantes de mensagem RF019`
   - Cria `FilialInativaException`, `MensagensFilialAgendamento`, `MotivosFalhaFilial`. Sem uso ainda (compila isolado).
2. `feat(agendamentos): retorna 409 para filial inativa e 404 'Filial não encontrada' (RF019)`
   - Edita `GarantirFilialAsync` (status/mensagens). Reflete nos três fluxos via calculadora compartilhada.
3. `feat(agendamentos): alinha mensagem de filial obrigatória nos validators (RF019)`
   - Edita os três validators (`Criar`/`PreConfirmar`/`Confirmar`).
4. `feat(agendamentos): audita falha de filial com motivo estruturado (RF019)`
   - Injeta `IAuditLogger`/`ILogger` na calculadora; auditoria de `filial_inexistente`/`filial_inativa`; enriquece logs de sucesso (DefinirEvento + dataHoraUTC/resultado).
5. `test(agendamentos): ajusta e adiciona testes do RF019 (409 inativa, 404 msg, validators, auditoria)`
   - Ajusta os testes que quebraram (§7) e adiciona os novos.
6. `docs(adr): registra desenho técnico do RF019 (filial obrigatória no agendamento)`
   - Este arquivo (pode ser o primeiro commit, conforme convenção do RF018).

---

## 7. Testes — quebram (QA ajustar) e novos a criar

### 7.1 Provavelmente quebram com as mudanças (status/mensagem)

| Teste | Arquivo:linha | Quebra porque | Ajuste |
|---|---|---|---|
| `POST_com_filial_inativa_retorna_422` | `CriarAgendamentoEndpointTests.cs` ~L211 | status 422→409; slug `recurso-inativo`→`filial-inativa` | esperar 409 + `type` contém `filial-inativa` + `title` == `MensagensFilialAgendamento.Inativa`; renomear método para `..._retorna_409` |
| `POST_filial_inativa_retorna_422` | `PreConfirmarAgendamentoEndpointTests.cs` ~L155 | idem | idem |
| `Filial_inativa_lanca_RecursoInativo` | `CriarAgendamentoHandlerTests.cs` ~L113 | exceção `RecursoInativoException`→`FilialInativaException` | esperar `FilialInativaException`; renomear |
| `Filial_vazia_falha_RF019` | `CriarAgendamentoCommandValidatorTests.cs` ~L22 | asserção `ErrorMessage.Contains("RF019")` (L26) falha | trocar para `Contains("Selecione uma filial válida")`; manter asserção de `PropertyName` |

> Atenção QA: como a calculadora passa a depender de `IAuditLogger`/`ILogger`, **todos** os testes unitários que instanciam `CalculadoraResumoAgendamento` diretamente (ex.: `CriarAgendamentoHandlerTests`, `ConfirmarAgendamentoHandlerTests`, `PreConfirmarAgendamentoHandlerTests`) precisam passar mocks/`NullLogger` no construtor. Isso é uma mudança de assinatura — revisar o setup desses arquivos. (Sem isso, não compilam.)

### 7.2 Não quebram (confirmado por leitura)

- `POST_filial_inexistente_retorna_404` (Criar/PreConfirmar): só checam status 404.
- `Filial_inexistente_lanca_NotFound` (unit): só checa tipo.
- Testes de veículo/cliente/serviço inativos (422): inalterados.
- `ConfirmarAgendamentoEndpointTests` "três 409": o novo slug `filial-inativa` não é exercitado lá.

### 7.3 Novos testes a criar

- Integração (Criar): `POST_com_filial_inativa_retorna_409_com_slug_filial_inativa` — assert 409, `type` contém `filial-inativa`, `title` == mensagem do card.
- Integração (PreConfirmar): idem.
- Integração (Confirmar): `POST_filial_inativa_retorna_409` — cobrir o terceiro caminho (filial desativada entre prévia e confirmação). Garante a regra nos três fluxos (CA006/CA011-adjacente).
- Integração (Criar): `POST_filial_inexistente_retorna_404_com_mensagem_do_card` — assert `title` == `Filial não encontrada.`.
- Integração (Criar): `POST_sem_filialId_retorna_400_com_mensagem_do_card` — payload sem `filialId` (ou `Guid.Empty`); assert 400 + `errors.filialId` contém `Selecione uma filial válida`.
- Integração (Criar): `POST_filialId_uuid_invalido_retorna_400` — `"filialId": "abc"`; assert 400, `type` contém `invalid-request`, `errors.filialId` presente (documenta Decisão 4).
- Unit (validator): `Filial_vazia_usa_mensagem_RF019_do_card` para os três validators.
- Unit (calculadora/handler): `Filial_inativa_lanca_FilialInativaException`; verificar (com mock de `IAuditLogger`) que `LogAsync` foi chamado com `dados.motivo == "filial_inativa"` e que `filial_inexistente` é auditado no caminho 404.
- Auditoria (integração, se houver helper de leitura de `audit_logs` como no RF018): confirmar registro `AgendamentoFilialRejeitada` com `motivo` correto em filial inexistente/inativa, e `AgendamentoCriado` no sucesso.

---

## 8. Riscos endereçados (RAT01–RAT05)

- **RAT01 (migrations versionadas):** nenhuma mudança de schema — FK/NOT NULL/índice já existem. Zero risco de migration.
- **RAT02 (índices coerentes):** consultas de validação de filial usam PK/lookup simples; capacidade usa `(FilialId, Inicio)` já existente. Sem novo índice.
- **RAT03 (validação server-side em camadas):** RF019 defendido em validator (400), aplicação (`GarantirFilialAsync` 404/409) e banco (FK NOT NULL). Frontend só dá feedback — nunca decide. Reforçado.
- **RAT04 (observabilidade):** auditoria de erro com `motivo` + INSERT auto-auditado no sucesso atende DAT §9.1 (eventos de mudança de status/exceção). Mensagem de erro nunca vaza detalhe interno.
- **RAT05 (não introduzir abstração desnecessária):** uma exceção nova (espelho de padrão existente), constantes, e injeção de duas deps já existentes. Sem envelope novo, sem policy RBAC, sem binder custom, sem mudança no middleware. YAGNI respeitado.

---

## 9. Resumo executivo das decisões

1. Filial inativa → **409** via nova `FilialInativaException : ConflictException` (slug `filial-inativa`); veículo/cliente/serviço seguem 422. Sem novo `catch` no middleware.
2. Filial inexistente → mensagem `"Filial não encontrada."` (constante; mesma frase do RF018).
3. Filial ausente/vazia → mensagem `"Selecione uma filial válida para prosseguir."` nos três validators.
4. UUID inválido → 400 do binder já atende; **não customizar**; documentado.
5. Auditoria com `motivo=filial_inexistente|filial_inativa` em `GarantirFilialAsync` (ponto único dos três fluxos), via `IAuditLogger`+`ILogger`, padrão RF018; sucesso pelo INSERT auto-auditado + log enriquecido.
6. **Não** adotar envelope `{message,data,traceId}`; `AgendamentoResponse` plano já é equivalente (`Mensagem`/`TraceId`/`filialId`); evitar quebra de contrato e de testes.
7. `POST /api/v1/agendamentos` roteia para `CriarAgendamentoHandler` (`[Obsolete]` mas ativo); validação de filial garantida nos três caminhos pela calculadora compartilhada.
8. **Não** introduzir 403/RBAC agora (RF-FUT003); manter só 401; 403 documentado como contrato futuro.
