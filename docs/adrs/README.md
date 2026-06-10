# Architectural Decision Records (ADR)

Esta pasta concentra os **Architectural Decision Records** do CarWash. Cada ADR documenta uma decisão técnica relevante: contexto, decisão tomada, alternativas consideradas e consequências.

## Formato

Seguimos uma variante leve do [Michael Nygard ADR](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) / [MADR](https://adr.github.io/madr/) simplificado, com as seções mínimas:

- **Status**: `Proposta` · `Aceita` · `Substituída` · `Revogada`.
- **Data**.
- **Contexto**.
- **Decisão**.
- **Consequências** (positivas e negativas).
- **Alternativas consideradas**.

## Convenções

- Nome do arquivo: `NNNN-titulo-em-kebab-case.md` com `NNNN` sequencial zero-padded (`0001`, `0002`, ...).
- Uma vez aceita, uma ADR não é editada — uma nova ADR a substitui (`Substituída por ADR XXXX`).
- Linkagem cruzada: documentos da pasta `tasks/` referenciam ADRs com link relativo.

## Índice

| # | Título | Status | Data |
| --- | --- | --- | --- |
| [0001](0001-geracao-de-uuid-pela-aplicacao.md) | Geração de UUID pela aplicação .NET | Aceita | 2026-05-13 |
| [0002](0002-hash-de-senha-com-argon2id.md) | Hash de senha com Argon2id | Aceita | 2026-05-13 |
| [0003](0003-minimal-api-cqrs-vertical-slices.md) | Minimal API + CQRS com Vertical Slices (sem MediatR) | Aceita | 2026-05-17 |

> Guia complementar à ADR 0003: [`../arquitetura-backend.md`](../arquitetura-backend.md) — definições da literatura, comparações lado a lado, template para novas features.
