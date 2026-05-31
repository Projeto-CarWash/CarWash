# Estudo de Viabilidade Consolidado - CarWash

## 1. Objetivo

Este documento consolida o estudo de viabilidade exigido para o papel de Analista de Projetos, em formato direto e separado, sem substituir o conteúdo mais amplo de `dvs.md`.

---

## 2. Parecer executivo

O projeto CarWash é viável para execução, com recomendação de continuidade condicionada a:

- manutenção do escopo priorizado no MVP;
- uso predominante de ferramentas gratuitas;
- organização do backlog por prioridade;
- validação das regras críticas de negócio;
- controle de prazo dentro do bimestre.

---

## 3. Viabilidade técnica

### 3.1 Análise

O projeto é tecnicamente viável porque:

- a solução é uma aplicação web interna, de arquitetura conhecida e bem documentada;
- a stack definida (`React`, `.NET` e `PostgreSQL`) é adequada ao tipo de sistema;
- as regras críticas já estão mapeadas em documentos de requisitos e especificação;
- a equipe possui base documental suficiente para orientar implementação, testes e validação;
- a complexidade do MVP e controlável, desde que as funcionalidades futuras não sejam antecipadas.

### 3.2 Tecnologias previstas

| Camada | Tecnologia | Justificativa |
|---|---|---|
| Frontend | React | Boa produtividade e aderência ao perfil da equipe |
| Backend | C# com .NET | Boa estruturação para regras de negócio e API |
| Banco de dados | PostgreSQL | Integridade transacional e boa modelagem relacional |
| Segurança | HTTPS, hash de senha e sessão autenticada | Atendem os requisitos mínimos de proteção |

### 3.3 Capacidade técnica da equipe

| Item | Avaliação |
|---|---|
| Compreensão do domínio | Adequada |
| Capacidade de documentação | Adequada |
| Capacidade de implementação do MVP | Viável com foco em Must Have |
| Risco técnico principal | Crescimento de escopo fora do planejado |

### 3.4 Conclusão técnica

**Parecer:** viável.

**Condição:** manter o foco nas funcionalidades essenciais e nas validações críticas já aprovadas.

---

## 4. Viabilidade econômica

### 4.1 Premissa

O projeto deve perseguir custo zero ou custo muito baixo, priorizando ferramentas gratuitas.

### 4.2 Estimativa de custo

| Item | Estrategia de custo |
|---|---|
| Documentacao | Arquivos Markdown locais |
| Gestão de tarefas | Trello plano gratuito |
| Versionamento | GitHub público |
| Banco e deploy de estudo | Opcoes gratuitas ou ambiente local |
| Comunicação da equipe | Ferramentas gratuitas já disponíveis |

### 4.3 Análise

- Não há necessidade de compra imediata de licenças para a fase acadêmica.
- O maior custo do projeto é o tempo da equipe, não a infraestrutura.
- A documentação atual reduz retrabalho e melhora o aproveitamento do tempo.
- O uso de soluções gratuitas é coerente com o porte do MVP e com o contexto do projeto.

### 4.4 Conclusão econômica

**Parecer:** viável.

**Condição:** manter a execução apoiada em ferramentas gratuitas e evitar dependências pagas desnecessárias.

---

## 5. Viabilidade de prazo

### 5.1 Premissa de cronograma

O projeto deve ser executado dentro do bimestre, com prioridade para os itens Must do MVP.

### 5.2 Cronograma sugerido do bimestre

| Semana | Foco principal | Entregas esperadas |
|---|---|---|
| 1 | Alinhamento documental | Escopo, requisitos, backlog e viabilidade revisados |
| 2 | Refinamento funcional | Histórias, regras de negócio, endpoints e rastreabilidade |
| 3 | Planejamento técnico e testes | Plano de testes, critérios de aceite e priorização de backlog |
| 4 | Execucao do núcleo do MVP | Cadastros, agenda, filiais e regras críticas |
| 5 | Consolidacao do MVP | Dashboard, histórico, ajustes e testes funcionais |
| 6 | Validação final | Relatório de bugs, README, revisão de entrega e apresentação |

### 5.3 Análise de prazo

- O prazo é viável se o backlog for controlado.
- O prazo deixa de ser viável se itens Should e Could forem antecipados sem controle.
- O uso de backlog priorizado é essencial para garantir entrega dentro do bimestre.
- O acompanhamento semanal reduz risco de atraso silencioso.

### 5.4 Conclusão de prazo

**Parecer:** viável.

**Condição:** congelar o escopo essencial do MVP e acompanhar as entregas semanalmente.

---

## 6. Riscos do projeto e mitigação

| ID | Risco | Probabilidade | Impacto | Mitigação |
|---|---|---|---|---|
| R01 | Crescimento de escopo | Média | Alto | Priorizar backlog e congelar Must Have |
| R02 | Atraso no bimestre | Média | Alto | Revisão semanal e controle de dependências |
| R03 | Falha na validação de regras críticas | Média | Alto | Manter rastreabilidade e plano de testes focado |
| R04 | Configuração incorreta de filiais e capacidade | Média | Alto | Validar regras RN009 e RN010 desde cedo |
| R05 | Conflito global de veículo não coberto | Média | Alto | Tratar RF020, RN011 e CA006 como prioridade máxima |
| R06 | Resistência de usuário ao uso do sistema | Média | Médio | Fluxos simples, treinamento e validação com cliente |
| R07 | Dependência de internet | Média | Médio | Definir contingência operacional e comunicar limitações |
| R08 | Falta de padronização na entrega | Média | Médio | Usar documentos separados e índice de entregáveis |

---

## 7. Recomendação final

O projeto **CarWash** apresenta viabilidade:

- técnica;
- econômica;
- de prazo;
- de execução acadêmica dentro do bimestre;
- com riscos controláveis por planejamento e priorização.

**Decisão recomendada:** seguir com a execução do projeto, mantendo foco no MVP e nas regras críticas do negócio.

---

## 8. Referências

- `dvs.md`
- `dvp-e.md`
- `drp.md`
- `4-dat.md`
- `5-gdr.md`




