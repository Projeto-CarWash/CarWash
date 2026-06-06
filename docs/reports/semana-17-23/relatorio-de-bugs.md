# Relatório de Bugs — Semana 17-23 (17 a 23 de maio)

**Projeto:** CarWash  ·  **Período:** 17 a 23 de maio de 2026  ·  **Gerado em:** 29/05/2026  ·  **Base:** template oficial `relatorio-de-bugs-template.md`

## 1. Objetivo

Registrar e rastrear os defeitos identificados durante testes, homologação e revisões do projeto CarWash nesta semana, facilitando priorização e acompanhamento das correções.

## 2. Resumo da semana

- **Total de bugs identificados:** 8
- **Alta severidade (URGENTE):** 4
- **Em aberto ao fim do período:** 3  ·  **Já corrigidos:** 3
- **Reportados majoritariamente por:** Lucas Gabriel (QA)

## 3. Classificação (severidade e status)

| Severidade | Descrição |
|---|---|
| Crítica | Impede uso do fluxo principal ou falha grave de negócio |
| Alta | Afeta fortemente funcionalidade importante do MVP (cards marcados *URGENTE*) |
| Média | Afeta comportamento relevante, mas há contorno |
| Baixa | Impacto pequeno, visual ou pontual |

**Status:** Aberto · Em análise · Em correção · Corrigido · Validado · Rejeitado

## 4. Tabela consolidada de bugs da semana

| ID | Título | Ref. | Módulo | Severidade | Status | Reportado em | Por | Responsável |
|---|---|---|---|---|---|---|---|---|
| BUG-103 | Autenticação com login e senha para usuários internos | RF001 | BACKEND | Baixa | Corrigido | 23/05 | Lucas Gabriel | matheus moreira, Lucas Gabriel |
| BUG-104 | Cadastro de clientes com dados cadastrais e observações | RF002 | BACKEND | Baixa | Corrigido | 23/05 | Lucas Gabriel | Vinicius Tomazi, Lucas Gabriel |
| BUG-105 | Validação de limites e formatos dos campos de cliente | RF003 | BACKEND | Baixa | Corrigido | 23/05 | Lucas Gabriel | Vinicius Tomazi, Lucas Gabriel |
| BUG-107 | Estruturação completa do banco de dados (tabelas, const | DB001 | BACKEND, BANCO DE DADOS | Baixa | Aberto | 23/05 | Lucas Gabriel | Guilherme Brogio Macedo da Silva, Lucas Gabriel |
| BUG-109 | Cadastro de clientes com dados cadastrais e observações | RF002 | FRONTEND | Alta | Em correção | 22/05 | Lucas Gabriel | Lucas Arruda, Guilherme Brogio Macedo da Silva |
| BUG-115 | Cadastro de veículos vinculados a cliente existente | RF004 | FRONTEND | Alta | Em correção | 18/05 | - | Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva |
| BUG-117 | Catálogo de serviços com tipo, preço e duração | RF006 | FRONTEND | Alta | Aberto | 18/05 | - | Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva |
| BUG-118 | Adicionar veículo no fluxo de cadastro de cliente | RF021 | FRONTEND | Alta | Aberto | 18/05 | - | Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva |

## 5. Detalhamento

### BUG-103 — RF001 - Autenticação com login e senha para usuários internos

- **Referência documental:** RF001
- **Labels:** BACKEND
- **Severidade:** Baixa  ·  **Status:** Corrigido
- **Identificado em:** 23/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** matheus moreira, Lucas Gabriel
- **Coluna atual:** CONCLUIDO

### BUG-104 — RF002 - Cadastro de clientes com dados cadastrais e observações

- **Referência documental:** RF002
- **Labels:** BACKEND, INTERLIGADOS
- **Severidade:** Baixa  ·  **Status:** Corrigido
- **Identificado em:** 23/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Vinicius Tomazi, Lucas Gabriel
- **Coluna atual:** CONCLUIDO

### BUG-105 — RF003 - Validação de limites e formatos dos campos de cliente

- **Referência documental:** RF003
- **Labels:** BACKEND, INTERLIGADOS
- **Severidade:** Baixa  ·  **Status:** Corrigido
- **Identificado em:** 23/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Vinicius Tomazi, Lucas Gabriel
- **Coluna atual:** CONCLUIDO

### BUG-107 — DB001 - Estruturação completa do banco de dados (tabelas, constraints, índices e trilha de auditoria)

- **Referência documental:** DB001
- **Labels:** BACKEND, BANCO DE DADOS
- **Severidade:** Baixa  ·  **Status:** Aberto
- **Identificado em:** 23/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Guilherme Brogio Macedo da Silva, Lucas Gabriel
- **Coluna atual:** BLOQUEADO

### BUG-109 — RF002 - Cadastro de clientes com dados cadastrais e observações

- **Referência documental:** RF002
- **Labels:** FRONTEND, INTERLIGADOS, BUG, URGENTE
- **Severidade:** Alta  ·  **Status:** Em correção
- **Identificado em:** 22/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Lucas Arruda, Guilherme Brogio Macedo da Silva
- **Coluna atual:** EM DESENVOLVIMENTO

### BUG-115 — RF004 - Cadastro de veículos vinculados a cliente existente

- **Referência documental:** RF004
- **Labels:** FRONTEND, INTERLIGADOS, BUG, URGENTE
- **Severidade:** Alta  ·  **Status:** Em correção
- **Identificado em:** 18/05 por - (movido de *(label BUG)* → *BUGS*)
- **Responsável(is) pela correção:** Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva
- **Coluna atual:** EM DESENVOLVIMENTO

### BUG-117 — RF006 - Catálogo de serviços com tipo, preço e duração

- **Referência documental:** RF006
- **Labels:** FRONTEND, URGENTE, BUG
- **Severidade:** Alta  ·  **Status:** Aberto
- **Identificado em:** 18/05 por - (movido de *(label BUG)* → *BUGS*)
- **Responsável(is) pela correção:** Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva
- **Coluna atual:** BUGS

### BUG-118 — RF021 - Adicionar veículo no fluxo de cadastro de cliente

- **Referência documental:** RF021
- **Labels:** FRONTEND, BUG, URGENTE
- **Severidade:** Alta  ·  **Status:** Aberto
- **Identificado em:** 18/05 por - (movido de *(label BUG)* → *BUGS*)
- **Responsável(is) pela correção:** Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva
- **Coluna atual:** BUGS

## 6. Riscos prováveis a monitorar (referência do template)

| ID sugerido | Risco monitorado | Relação documental |
|---|---|---|
| BUG-001 | Agendamento salvo sem filial | CA007 |
| BUG-002 | Capacidade de filial aceita valor fora da faixa | CA008 |
| BUG-003 | Mesmo veículo agendado no mesmo horário em filiais diferentes | CA006 |
| BUG-004 | Filiado não aparece no agendamento após cadastro | CA010 |
| BUG-005 | Agendamento finalizado permite edição | RN004 |

## 7. Processo recomendado

1. Registrar o bug com o máximo de contexto. 2. Associar ao requisito/regra/CA. 3. Priorizar por severidade. 4. Atualizar status conforme a correção. 5. Registrar reteste e evidências.

## 8. Referências

- `plano-de-testes-mvp.md` · `drp.md` · `5-gdr.md` · `relatorio-de-bugs-template.md`
