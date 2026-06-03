# Backlog Detalhado do Projeto - CarWash

## 1. Objetivo

Este backlog detalhado organiza o trabalho do projeto CarWash por épicos, histórias, prioridades, dependências e critérios de pronto documental.

Seu objetivo é traduzir os requisitos já definidos em uma visão de execução clara, utilizável para planejamento, acompanhamento e priorização das entregas.

---

## 2. Critérios de priorização

### 2.1 Escala de prioridade

| Prioridade | Significado |
|---|---|
| Alta | Necessário para o MVP e para validação do negócio |
| Média | Importante para consolidar uso e qualidade do sistema |
| Baixa | Evolutivo ou não bloqueante para a entrega do MVP |

### 2.2 Regra de sequenciamento

- Implementar primeiro o que destrava o fluxo principal.
- Priorizar requisitos Must antes dos Should.
- Antecipar validações críticas de negócio.
- Não puxar item futuro sem aprovação de mudança de escopo.

---

## 3. Backlog por épico

Esta seção consolida os épicos do projeto em formato aderente ao board, relacionando objetivo, cards principais, sprints predominantes e dependências.

| Épico | Objetivo | Cards principais | Sprints predominantes | Dependências principais |
|---|---|---|---|---|
| EP01 - Acesso e segurança | Garantir autenticação, sessão e controle inicial de acesso | `B001`, `RF001`, `RF014` | Sprint 01 | Estrutura inicial de banco e regras de autenticação |
| EP02 - Clientes e veículos | Estruturar cadastro de clientes, validações e vínculo de veículos | `RF002`, `RF003`, `RF004`, `RF005`, `RF021`, `RF022` | Sprints 01 e 02 | Acesso inicial e persistência de dados |
| EP03 - Filiados do titular | Permitir cadastro e uso de filiados vinculados ao cliente titular | `RF023`, `RF024` | Sprint 06 | Cadastro de cliente e fluxo de agendamento |
| EP04 - Serviços | Manter catálogo de serviços e permitir sua seleção no agendamento | `RF006`, `RF007.2` | Sprints 02 e 03 | Cadastro base e fluxo de agendamento |
| EP05 - Filiais e capacidade | Estruturar operação multiunidade e limite de capacidade por filial | `RF017`, `RF018`, `RF019`, `RF020` | Sprint 05 | Base da agenda e regras de negócio |
| EP06 - Agenda operacional | Construir o fluxo principal de agendamento e suas regras de estado | `RF007`, `RF007.1`, `RF007.3`, `RF008`, `RF009`, `RF010`, `RF011`, `RF015` | Sprints 03 e 04 | Clientes, veículos, serviços e filiais |
| EP07 - Histórico e indicadores | Disponibilizar histórico de atendimentos e métricas de negócio | `RF012`, `RF013` | Sprints 04 e 06 | Fluxo de agenda consolidado |
| EP08 - Experiência da interface | Melhorar usabilidade com configurações visuais e fluxo claro | `RF016` | Sprint 06 | Telas principais já implementadas |
| EP09 - Qualidade e governança | Garantir rastreabilidade, validação crítica, bugs e documentação final | `CA006 a CA011` | Transversal, com fechamento na Sprint 06 | Cobertura dos fluxos críticos e evidências de teste |

### 3.1 Mapa resumido de épicos para o board

| Épico | Frente dominante | Cards de maior impacto |
|---|---|---|
| EP01 - Acesso e segurança | `BACKEND`, `FRONTEND` | `B001`, `RF001`, `RF014` |
| EP02 - Clientes e veículos | `INTERLIGADOS` | `RF002`, `RF003`, `RF004`, `RF005`, `RF021`, `RF022` |
| EP03 - Filiados do titular | `BACKEND`, `FRONTEND` | `RF023`, `RF024` |
| EP04 - Serviços | `BACKEND`, `FRONTEND` | `RF006`, `RF007.2` |
| EP05 - Filiais e capacidade | `BACKEND`, `FRONTEND` | `RF017`, `RF018`, `RF019`, `RF020` |
| EP06 - Agenda operacional | `INTERLIGADOS` | `RF007`, `RF008`, `RF009`, `RF010`, `RF011`, `RF015` |
| EP07 - Histórico e indicadores | `BACKEND`, `FRONTEND` | `RF012`, `RF013` |
| EP08 - Experiência da interface | `FRONTEND` | `RF016` |
| EP09 - Qualidade e governança | `INTERLIGADOS` | `CA006 a CA011` |

---

## 4. Planejamento de sprints

Esta seção espelha a estrutura do board no Trello, com cards distribuídos por sprint, semanas e labels de frente de trabalho.

### 4.0 Padrão de labels do board

| Label | Uso |
|---|---|
| `BACKEND` | Implementações e regras do lado servidor |
| `FRONTEND` | Telas, fluxos visuais e validações de interface |
| `INTERLIGADOS` | Itens que dependem da integração entre backend e frontend |
| `BANCO DE DADOS` | Estrutura relacional, constraints, índices e persistência |

### 4.1 Sprint 01 - Semanas 1-2

**Foco:** base técnica, acesso, cadastro inicial de clientes e usuários.

| Card | Labels | Descrição |
|---|---|---|
| B001 - Estruturação completa do banco de dados | `BANCO DE DADOS`, `BACKEND` | Tabelas, constraints, índices e trilha de auditoria |
| RF001 - Autenticação com login e senha para usuários internos | `BACKEND` | Implementação da autenticação no servidor |
| RF002 - Cadastro de clientes com dados cadastrais e observações | `BACKEND` | Cadastro e persistência dos dados do cliente |
| RF003 - Validação de limites e formatos dos campos do cliente | `INTERLIGADOS`, `BACKEND` | Regras de validação para nome, CPF, CNPJ, telefone e celular |
| RF014 - Cadastro básico de usuários internos | `BACKEND` | Cadastro de usuários com controle de acesso |
| RF001 - Autenticação de usuários internos | `FRONTEND` | Tela e fluxo de login |
| RF002 - Cadastro de clientes com dados cadastrais e observações | `FRONTEND` | Tela de cadastro de clientes |
| RF003 - Validação de limites e formatos dos campos do cliente | `FRONTEND`, `INTERLIGADOS` | Validações visuais e mensagens de erro |
| RF014 - Cadastro básico de usuários internos | `FRONTEND` | Tela de cadastro de usuários |

### 4.2 Sprint 02 - Semanas 3-4

**Foco:** veículos, catálogo de serviços e integração com o fluxo de cadastro de cliente.

| Card | Labels | Descrição |
|---|---|---|
| RF004 - Cadastro de veículos vinculados a cliente existente | `INTERLIGADOS`, `BACKEND` | Regras de vínculo entre cliente e veículo |
| RF005 - Validação de placa e bloqueio de duplicidade | `INTERLIGADOS`, `BACKEND` | Regra de unicidade da placa |
| RF006 - Catálogo de serviços com tipo, preço e duração | `BACKEND` | Cadastro e manutenção do catálogo |
| RF021 - Adicionar veículo no fluxo de cadastro de cliente | `BACKEND` | Inclusão do veículo no mesmo fluxo do cliente |
| RF022 - Exibir veículos do cliente na visualização detalhada | `BACKEND` | Retorno dos dados vinculados ao cliente |
| RF004 - Cadastro de veículos vinculados a cliente existente | `FRONTEND`, `INTERLIGADOS` | Tela e fluxo de cadastro do veículo |
| RF005 - Validação de placa e bloqueio de duplicidade | `FRONTEND`, `INTERLIGADOS` | Tratamento visual das validações |
| RF006 - Catálogo de serviços com tipo, preço e duração | `FRONTEND` | Tela de catálogo de serviços |
| RF021 - Adicionar veículo no fluxo de cadastro de cliente | `FRONTEND` | Fluxo de inclusão no cadastro |
| RF022 - Exibir veículos do cliente na visualização detalhada | `FRONTEND` | Exibição dos veículos vinculados |

### 4.3 Sprint 03 - Semanas 5-6

**Foco:** criação do fluxo principal de agendamento e visualização inicial da agenda.

| Card | Labels | Descrição |
|---|---|---|
| RF007 - Criação de agendamento com cliente, veículo e serviços | `BACKEND` | Persistência e regras básicas do agendamento |
| RF009 - Visualização da agenda em formato simples e detalhado | `BACKEND` | Consulta da agenda por dia e detalhe |
| RF015 - Confirmação das informações antes de concluir agendamento | `BACKEND` | Etapa de confirmação final no fluxo |
| RF007 - Criação de agendamento com cliente, veículo e serviços | `FRONTEND` | Tela principal do agendamento |
| RF007.1 - Agendamento: etapa de cliente e veículo | `FRONTEND`, `INTERLIGADOS` | Primeira etapa do fluxo de agendamento |
| RF007.2 - Agendamento: seleção de múltiplos serviços | `FRONTEND`, `INTERLIGADOS` | Escolha de serviços no agendamento |
| RF007.3 - Agendamento: resumo e confirmação final | `FRONTEND`, `INTERLIGADOS` | Revisão final antes de salvar |
| RF009 - Visualização da agenda em formato simples e detalhado | `FRONTEND` | Lista e detalhe da agenda |
| RF015 - Confirmação das informações antes de concluir agendamento | `FRONTEND` | Fluxo de confirmação da tela |

### 4.4 Sprint 04 - Semanas 7-8

**Foco:** simultaneidade, cancelamento, observações e histórico de atendimento.

| Card | Labels | Descrição |
|---|---|---|
| RF008 - Permitir agendamentos simultâneos no mesmo horário | `BACKEND` | Regra de simultaneidade respeitando capacidade |
| RF010 - Cancelamento e bloqueio de edição de agendamento finalizado | `BACKEND` | Regras de estado do agendamento |
| RF011 - Registro de observações logísticas por agendamento | `BACKEND` | Persistência das observações |
| RF012 - Consulta de histórico de atendimentos por cliente | `BACKEND` | Consulta do histórico por cliente |
| RF008.1 - Agenda: visualização simultânea por horário | `FRONTEND`, `INTERLIGADOS` | Exibição de múltiplos agendamentos no mesmo horário |
| RF008.2 - Agenda: criação sem bloqueio indevido | `FRONTEND`, `INTERLIGADOS` | Fluxo de criação quando há capacidade disponível |
| RF008.3 - Agenda: tratamento de conflito real | `FRONTEND`, `INTERLIGADOS` | Tratamento visual de conflitos válidos |
| RF010 - Cancelamento e bloqueio de edição de agendamento finalizado | `FRONTEND` | Ações de cancelamento e bloqueio visual |
| RF011 - Registro de observações logísticas por agendamento | `FRONTEND` | Campo de observações na interface |
| RF012 - Consulta de histórico de atendimentos por cliente | `FRONTEND` | Exibição do histórico na tela |

### 4.5 Sprint 05 - Semanas 9-10

**Foco:** multiunidade, capacidade operacional e bloqueio global por veículo.

| Card | Labels | Descrição |
|---|---|---|
| RF017 - Cadastro de filiais para operação multiunidade | `BACKEND` | Cadastro de unidades operacionais |
| RF018 - Configuração de células ativas por filial entre 1 e 100 | `BACKEND` | Regra de capacidade operacional |
| RF019 - Seleção obrigatória de filial na criação do agendamento | `BACKEND` | Obrigatoriedade da filial no fluxo |
| RF020 - Bloqueio de conflito do mesmo veículo no mesmo horário | `BACKEND` | Regra global de conflito por horário |
| RF017 - Cadastro de filiais para operação multiunidade | `FRONTEND` | Tela de cadastro de filiais |
| RF018 - Configuração de células ativas por filial entre 1 e 100 | `FRONTEND` | Configuração visual da capacidade |
| RF019 - Seleção obrigatória de filial na criação do agendamento | `FRONTEND` | Campo obrigatório no fluxo de agendamento |
| RF020 - Bloqueio de conflito do mesmo veículo no mesmo horário | `FRONTEND` | Exibição do bloqueio e mensagens de erro |

### 4.6 Sprint 06 - Semanas 11-12

**Foco:** dashboard, tema visual e cadastro/uso de filiados no agendamento.

| Card | Labels | Descrição |
|---|---|---|
| RF013 - Dashboard com métricas operacionais e financeiras | `BACKEND` | Consolidação dos indicadores do negócio |
| RF016 - Alternância entre tema claro e escuro | `BACKEND` | Persistência ou configuração da preferência de tema |
| RF023 - Cadastro de filiados vinculados ao cliente titular | `BACKEND` | Cadastro e vínculo do filiado ao titular |
| RF024 - Seleção de filiado no momento do agendamento | `BACKEND` | Uso do filiado no fluxo do agendamento |
| RF013 - Dashboard com métricas operacionais e financeiras | `FRONTEND` | Tela de indicadores e métricas |
| RF016 - Alternância entre tema claro e escuro | `FRONTEND` | Componente e aplicação do tema |
| RF023 - Cadastro de filiados vinculados ao cliente titular | `FRONTEND` | Tela de cadastro de filiados |
| RF024 - Seleção de filiado no momento do agendamento | `FRONTEND` | Seleção do filiado na interface |

---

## 5. Itens com prioridade máxima de negócio

Os cards abaixo devem receber atenção imediata, em razão do impacto direto no fluxo principal do produto e do risco funcional elevado no MVP:

| Prioridade | Card | Labels | Justificativa |
|---|---|---|---|
| 1 | RF007 - Criação de agendamento com cliente, veículo e serviços | `BACKEND`, `FRONTEND` | Constitui o núcleo funcional do produto e viabiliza o fluxo principal da operação |
| 2 | RF019 - Seleção obrigatória de filial na criação do agendamento | `BACKEND`, `FRONTEND` | Garante contexto operacional correto por unidade |
| 3 | RF020 - Bloqueio de conflito do mesmo veículo no mesmo horário | `BACKEND`, `FRONTEND` | Evita conflito global crítico entre filiais |
| 4 | RF018 - Configuração de células ativas por filial entre 1 e 100 | `BACKEND`, `FRONTEND` | Sustenta a regra de capacidade operacional do negócio |
| 5 | RF024 - Seleção de filiado no momento do agendamento | `BACKEND`, `FRONTEND` | Assegura a identificação formal de quem representa o titular no atendimento |
| 6 | B001 - Estruturação completa do banco de dados | `BANCO DE DADOS`, `BACKEND` | Dá suporte estrutural para constraints, rastreabilidade e integridade |
| 7 | CA006 a CA011 - Validação dos critérios de aceite críticos | `INTERLIGADOS` | Reduz risco de falha nos cenários mais sensíveis do MVP |

---

## 6. Definição de pronto do backlog

Um item do backlog só deve ser considerado pronto quando:

1. estiver vinculado a RF, RN ou CA;
2. possuir descrição clara e sem ambiguidade;
3. tiver dependência conhecida;
4. possuir critério de validação definido;
5. estiver apto a gerar tarefa técnica, teste e evidência.

---

## 7. Referências

- `drp.md`
- `us.md`
- `5-gdr.md`
- `api-banco-carwash-especificacao-completa.md`




