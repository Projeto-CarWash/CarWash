# Relatório de Bugs — Semana 24-30 (24 a 30 de maio)

**Projeto:** CarWash  ·  **Período:** 24 a 30 de maio de 2026  ·  **Gerado em:** 29/05/2026  ·  **Base:** template oficial `relatorio-de-bugs-template.md`

## 1. Objetivo

Registrar e rastrear os defeitos identificados durante testes, homologação e revisões do projeto CarWash nesta semana, facilitando priorização e acompanhamento das correções.

## 2. Resumo da semana

- **Total de bugs identificados:** 14
- **Alta severidade (URGENTE):** 5
- **Em aberto ao fim do período:** 6  ·  **Já corrigidos:** 6
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
| BUG-108 | Autenticação com login e senha para usuários internos - | RF001 | FRONTEND | Baixa | Corrigido | 27/05 | Lucas Gabriel | Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva |
| BUG-109 | Cadastro de clientes com dados cadastrais e observações | RF002 | FRONTEND | Alta | Em correção | 27/05 | Lucas Gabriel | Lucas Arruda, Guilherme Brogio Macedo da Silva |
| BUG-115 | Cadastro de veículos vinculados a cliente existente | RF004 | FRONTEND | Alta | Em correção | 27/05 | Lucas Gabriel | Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva |
| BUG-117 | Catálogo de serviços com tipo, preço e duração | RF006 | FRONTEND | Alta | Aberto | 27/05 | Lucas Gabriel | Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva |
| BUG-118 | Adicionar veículo no fluxo de cadastro de cliente | RF021 | FRONTEND | Alta | Aberto | 27/05 | Lucas Gabriel | Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva |
| BUG-120 | Criação de agendamento com cliente, veículo e serviços  | RF007 | FRONTEND | Baixa | Aberto | 27/05 | Lucas Gabriel | Lucas Arruda, Guilherme Brogio Macedo da Silva |
| BUG-121 | Agendamento: etapa de cliente e veículo | RF007.1 | FRONTEND | Baixa | Corrigido | 27/05 | Lucas Gabriel | Lucas Arruda, Guilherme Brogio Macedo da Silva |
| BUG-122 | Agendamento: seleção de múltiplos serviços | RF007.2 | FRONTEND | Baixa | Corrigido | 27/05 | Lucas Gabriel | Lucas Arruda, Guilherme Brogio Macedo da Silva |
| BUG-123 | Agendamento: resumo e confirmação final | RF007.3 | FRONTEND | Baixa | Corrigido | 27/05 | Lucas Gabriel | Lucas Arruda, Guilherme Brogio Macedo da Silva |
| BUG-125 | Confirmação das informações antes de concluir agendamen | RF015 | FRONTEND | Baixa | Corrigido | 27/05 | Lucas Gabriel | Lucas Arruda, Guilherme Brogio Macedo da Silva |
| BUG-127 | Validação de placa e bloqueio de duplicidade | RF005 | BACKEND | Baixa | Aberto | 29/05 | Lucas Gabriel | matheus moreira |
| BUG-129 | Adicionar veículo no fluxo de cadastro de cliente | RF021 | BACKEND | Baixa | Aberto | 28/05 | Lucas Gabriel | Vinicius Tomazi |
| BUG-130 | Exibir veículos do cliente na visualização detalhada | RF022 | BACKEND | Baixa | Aberto | 28/05 | Lucas Gabriel | Vinicius Tomazi |
| BUG-135 | SWITCH DE INATIVO E ATIVO DOS CADASTROS DOS USUARIOS | - | FRONTEND | Alta | Corrigido | 27/05 | Lucas Gabriel | Thiago Cezario Da Silva |

## 5. Detalhamento

### BUG-108 — RF001 - Autenticação com login e senha para usuários internos - Frontend

- **Referência documental:** RF001
- **Labels:** FRONTEND
- **Severidade:** Baixa  ·  **Status:** Corrigido
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva
- **Coluna atual:** CONCLUIDO

### BUG-109 — RF002 - Cadastro de clientes com dados cadastrais e observações

- **Referência documental:** RF002
- **Labels:** FRONTEND, INTERLIGADOS, BUG, URGENTE
- **Severidade:** Alta  ·  **Status:** Em correção
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Lucas Arruda, Guilherme Brogio Macedo da Silva
- **Coluna atual:** EM DESENVOLVIMENTO

### BUG-115 — RF004 - Cadastro de veículos vinculados a cliente existente

- **Referência documental:** RF004
- **Labels:** FRONTEND, INTERLIGADOS, BUG, URGENTE
- **Severidade:** Alta  ·  **Status:** Em correção
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *A FAZER - QA* → *BUGS*)
- **Responsável(is) pela correção:** Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva
- **Coluna atual:** EM DESENVOLVIMENTO

### BUG-117 — RF006 - Catálogo de serviços com tipo, preço e duração

- **Referência documental:** RF006
- **Labels:** FRONTEND, URGENTE, BUG
- **Severidade:** Alta  ·  **Status:** Aberto
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva
- **Coluna atual:** BUGS

### BUG-118 — RF021 - Adicionar veículo no fluxo de cadastro de cliente

- **Referência documental:** RF021
- **Labels:** FRONTEND, BUG, URGENTE
- **Severidade:** Alta  ·  **Status:** Aberto
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Thiago Cezario Da Silva, Guilherme Brogio Macedo da Silva
- **Coluna atual:** BUGS

### BUG-120 — RF007 - Criação de agendamento com cliente, veículo e serviços (Card Pai)

- **Referência documental:** RF007
- **Labels:** FRONTEND, INTERLIGADOS
- **Severidade:** Baixa  ·  **Status:** Aberto
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *A FAZER - QA* → *BUGS*)
- **Responsável(is) pela correção:** Lucas Arruda, Guilherme Brogio Macedo da Silva
- **Coluna atual:** BLOQUEADO

### BUG-121 — RF007.1 - Agendamento: etapa de cliente e veículo

- **Referência documental:** RF007.1
- **Labels:** FRONTEND, INTERLIGADOS
- **Severidade:** Baixa  ·  **Status:** Corrigido
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *A FAZER - QA* → *BUGS*)
- **Responsável(is) pela correção:** Lucas Arruda, Guilherme Brogio Macedo da Silva
- **Coluna atual:** CONCLUIDO

### BUG-122 — RF007.2 - Agendamento: seleção de múltiplos serviços

- **Referência documental:** RF007.2
- **Labels:** FRONTEND, INTERLIGADOS
- **Severidade:** Baixa  ·  **Status:** Corrigido
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *A FAZER - QA* → *BUGS*)
- **Responsável(is) pela correção:** Lucas Arruda, Guilherme Brogio Macedo da Silva
- **Coluna atual:** CONCLUIDO

### BUG-123 — RF007.3 - Agendamento: resumo e confirmação final

- **Referência documental:** RF007.3
- **Labels:** FRONTEND, INTERLIGADOS
- **Severidade:** Baixa  ·  **Status:** Corrigido
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *A FAZER - QA* → *BUGS*)
- **Responsável(is) pela correção:** Lucas Arruda, Guilherme Brogio Macedo da Silva
- **Coluna atual:** CONCLUIDO

### BUG-125 — RF015 - Confirmação das informações antes de concluir agendamento

- **Referência documental:** RF015
- **Labels:** FRONTEND
- **Severidade:** Baixa  ·  **Status:** Corrigido
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Lucas Arruda, Guilherme Brogio Macedo da Silva
- **Coluna atual:** CONCLUIDO

### BUG-127 — RF005 - Validação de placa e bloqueio de duplicidade

- **Referência documental:** RF005
- **Labels:** BACKEND, INTERLIGADOS
- **Severidade:** Baixa  ·  **Status:** Aberto
- **Identificado em:** 29/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** matheus moreira
- **Coluna atual:** BUGS

### BUG-129 — RF021 - Adicionar veículo no fluxo de cadastro de cliente

- **Referência documental:** RF021
- **Labels:** BACKEND
- **Severidade:** Baixa  ·  **Status:** Aberto
- **Identificado em:** 28/05 por Lucas Gabriel (movido de *A FAZER - QA* → *BUGS*)
- **Responsável(is) pela correção:** Vinicius Tomazi
- **Coluna atual:** BUGS

### BUG-130 — RF022 - Exibir veículos do cliente na visualização detalhada

- **Referência documental:** RF022
- **Labels:** BACKEND
- **Severidade:** Baixa  ·  **Status:** Aberto
- **Identificado em:** 28/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Vinicius Tomazi
- **Coluna atual:** BUGS

### BUG-135 — SWITCH DE INATIVO E ATIVO DOS CADASTROS DOS USUARIOS

- **Referência documental:** -
- **Labels:** FRONTEND, URGENTE
- **Severidade:** Alta  ·  **Status:** Corrigido
- **Identificado em:** 27/05 por Lucas Gabriel (movido de *QUALIDADE/TEST EM ANDAMENTO* → *BUGS*)
- **Responsável(is) pela correção:** Thiago Cezario Da Silva
- **Coluna atual:** CONCLUIDO

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
