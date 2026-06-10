# ADR 0002 — Hash de senha com Argon2id

- **Status:** Aceita
- **Data:** 2026-05-13
- **Autores:** Guilherme Brogio (arquiteto técnico) — decisão registrada após validação humana.
- **Escopo:** Backend `.NET 8` + PostgreSQL 16 — hash de senhas em `usuarios.senha_hash`.

---

## Contexto

A coluna `usuarios.senha_hash` armazena o digest da senha de cada usuário do CarWash. O algoritmo precisa ser:

1. Resistente a ataques offline em GPU/ASIC (relevante se um dump do banco vazar).
2. Configurável em custo (tempo + memória) para acompanhar a evolução de hardware.
3. Padrão moderno reconhecido pela OWASP/IETF — vencedor da Password Hashing Competition (PHC) e recomendado pelo RFC 9106.
4. Disponível no ecossistema .NET com biblioteca madura, sem necessidade de invocar binários externos.

Algoritmos modernos viáveis para hashing de senha:

- **BCrypt** (1999, Niels Provos / David Mazières) — apenas custo de CPU.
- **PBKDF2** (RFC 2898) — apenas custo de CPU; vulnerável a GPU/ASIC otimizado.
- **scrypt** (RFC 7914) — custo de CPU + memória, mas com PHC superado pelo Argon2.
- **Argon2id** (RFC 9106, vencedor do PHC em 2015) — custo de CPU + memória + paralelismo, resistente a side-channel attacks.

A decisão precisa ser tomada antes de T06 (helpers de segurança) e T08 (migration `InitialSchema` que injeta o admin seed) da DB001.

---

## Decisão

**Toda senha persistida em `usuarios.senha_hash` é hasheada com Argon2id.**

Concretamente:

- Biblioteca: **`Konscious.Security.Cryptography.Argon2`** (.NET).
- Variante: **Argon2id** (combinação `Argon2i` + `Argon2d` — resistente a ataques side-channel e a GPU).
- Parâmetros iniciais (perfil "interactive" da PHC, alvo ~50–100ms por hash em servidor moderno):
  - `m = 65536` KiB (`64 MB` de memória por hash).
  - `t = 3` iterações.
  - `p = 1` grau de paralelismo.
- Salt: **16 bytes aleatórios por hash** (gerados via `RandomNumberGenerator.Fill`).
- Storage: coluna `senha_hash TEXT` (já existente). Formato canônico PHC:

  ```text
  $argon2id$v=19$m=65536,t=3,p=1$<salt-base64>$<hash-base64>
  ```

  Esse formato é auto-descritivo — o verificador relê os parâmetros do próprio hash, viabilizando rotação de custo no futuro sem migration de banco.
- Verificação: lê parâmetros do hash armazenado, re-hasheia a senha submetida com os mesmos parâmetros e salt, compara em tempo constante.
- Benchmark obrigatório: o `BCrypt` foi cogitado primeiro; antes do merge final de T06, o `dev-dotnet-carwash` deve rodar um micro-benchmark do `Argon2id` com os parâmetros acima no hardware-alvo (Docker em servidor médio) e confirmar que o tempo cai entre 50ms e 150ms. Se ficar fora dessa faixa, ajustar `m`/`t` e atualizar este documento.

---

## Justificativa

1. **Resistência a GPU/ASIC.** Argon2id usa custo de memória, que é caro de paralelizar em GPU. BCrypt protege só com CPU, e ataques modernos com FPGA/GPU reduzem drasticamente o custo efetivo do BCrypt em volumes grandes.
2. **Recomendação OWASP atual.** A OWASP Password Storage Cheat Sheet (2024+) recomenda Argon2id como **primeira escolha**, com BCrypt apenas como fallback se Argon2id não estiver disponível.
3. **RFC 9106 (2021).** Argon2 é padrão IETF; Argon2id é a variante recomendada para hashing de senha. Não há padrão IETF para BCrypt.
4. **Formato PHC permite rotação.** Trocar parâmetros (ex.: aumentar `m` para 128 MB no futuro) é trivial — basta rehash no próximo login bem-sucedido. O formato canônico carrega os parâmetros, então hashes antigos continuam verificáveis enquanto a transição acontece.
5. **Ecossistema .NET maduro.** `Konscious.Security.Cryptography.Argon2` é a biblioteca de referência, mantida, sem dependências nativas. Não exige binding C, instalação de libs do SO, nem Docker base alternativa.
6. **Sistema interno, mas com defesa em profundidade.** Mesmo em sistema de baixa exposição, dump de banco é um cenário plausível (backup mal protegido, snapshot vazado). Argon2id eleva o custo de cracking offline em ordens de magnitude vs. BCrypt.

---

## Consequências

### Positivas

- Defesa robusta contra cracking offline mesmo se o banco vazar.
- Parâmetros embutidos no hash permitem rotação sem migration de banco.
- Alinhamento com a recomendação atual da OWASP e RFC 9106.
- Sem dependência nativa (lib pure .NET).
- Mesmo algoritmo entre seed do admin (migration) e fluxo normal de cadastro/troca de senha — sem código diferente para os dois caminhos.

### Negativas

- Custo de memória por hash (`64 MB`). Em pico de logins concorrentes, o servidor consome memória proporcional. Mitigado por (a) volume baixo do CarWash, (b) rate limiting no endpoint de login (entra em tarefa de auth, não da DB001), (c) parâmetros revisáveis.
- Biblioteca externa adicional no `Directory.Packages.props` (`Konscious.Security.Cryptography.Argon2`). Risco operacional baixo — projeto popular e estável.
- Latência de login fica em ~50–100ms só pelo hash. Aceitável (login não é endpoint quente).

---

## Alternativas consideradas

### A. BCrypt (`BCrypt.Net-Next`, work factor 12)

Recomendação original do `05-seed-tecnico.md` e do `06-auditoria-seguranca.md`.

- **Prós:** maturidade no ecossistema .NET, ampla adoção, lib estável.
- **Contras:** apenas custo de CPU — vulnerável a ataques modernos com GPU/ASIC; work factor é o único knob; sem padronização IETF; OWASP recomenda Argon2id como primeira escolha em 2024+.
- **Veredito:** Rejeitada. Pode ser reintroduzida como fallback se Argon2id apresentar problema operacional, mas não é o default.

### B. PBKDF2 (RFC 2898, `Rfc2898DeriveBytes` da BCL)

- **Prós:** disponível na BCL, sem dependência externa, padrão FIPS.
- **Contras:** apenas custo de CPU; vulnerável a GPU; precisaria de iteração extremamente alta para equivaler a Argon2id; OWASP recomenda apenas quando Argon2id/BCrypt indisponíveis.
- **Veredito:** Rejeitada. Pior perfil de defesa que BCrypt; só faria sentido se houvesse exigência FIPS — não é o caso.

### C. scrypt

- **Prós:** custo de memória + CPU.
- **Contras:** padronizado mas superado pelo Argon2 (vencedor da PHC); ecossistema .NET menos consolidado; sem ganho claro sobre Argon2id.
- **Veredito:** Rejeitada.

---

## Implicações operacionais

- **Seed do admin:** gera o hash em runtime usando Argon2id com a senha lida da variável de ambiente `CARWASH_SEED_ADMIN_PASSWORD`.
- **Dependência:** `Konscious.Security.Cryptography.Argon2` adicionada ao `Directory.Packages.props`. `BCrypt.Net-Next` **não** é utilizado para hash de senha.
- **Implementação:** `Argon2idPasswordHasher` em `backend/src/CarWash.Infrastructure/Security/`, exposto via interface `IPasswordHasher` em `backend/src/CarWash.Application/Abstractions/`. Cobrir com unit tests (hash + verify + verify com hash inválido + verify em tempo constante).
- **Rotação futura:** se OWASP recomendar `m=128 MB` no futuro, basta atualizar o helper. Hashes antigos continuam válidos (parâmetros embutidos no PHC); a aplicação reidrata o hash no próximo login bem-sucedido se detectar parâmetros desatualizados via `NeedsRehash`.
- **Refresh token:** **não usa Argon2id**. Refresh tokens são alta entropia (≥256 bits aleatórios), e o hash em `usuario_sessoes.refresh_token_hash` continua sendo SHA-256 com comparação em tempo constante (`CryptographicOperations.FixedTimeEquals`).

---

## Re-avaliação

Esta ADR deve ser revisitada quando:

- A OWASP publicar recomendações de parâmetros diferentes (m/t/p) que invalidem os atuais.
- Surgir vulnerabilidade conhecida em Argon2id.
- O ambiente alvo deixar de comportar `m=64 MB` por hash em pico (medido — não suposto).
- Houver requisito legal/compliance que exija outro algoritmo (ex.: FIPS estrito sem variant approved de Argon2).
- O .NET BCL passar a oferecer Argon2id nativo (avaliar troca da dependência externa pela BCL).

---

## Referências

- [RFC 9106 — Argon2 Memory-Hard Function for Password Hashing and Proof-of-Work Applications](https://www.rfc-editor.org/rfc/rfc9106)
- [OWASP Password Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html)
- [Konscious.Security.Cryptography.Argon2 (NuGet)](https://www.nuget.org/packages/Konscious.Security.Cryptography.Argon2)
- [Password Hashing Competition — Argon2 winner (2015)](https://www.password-hashing.net/)
- ADR 0001 — [`./0001-geracao-de-uuid-pela-aplicacao.md`](./0001-geracao-de-uuid-pela-aplicacao.md).
