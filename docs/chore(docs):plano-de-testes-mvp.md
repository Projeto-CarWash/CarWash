# Plano de Testes do MVP - CarWash

## 1. Objetivo

Este plano de testes define a estratégia de validação funcional do MVP do projeto CarWash, com foco nas regras de negócio críticas, nos fluxos principais e nos critérios de aceite do produto.

---

## 2. Escopo do plano

O plano cobre a validação dos seguintes domínios:

- autenticação;
- cadastro de clientes;
- cadastro de veículos;
- cadastro de filiados;
- cadastro de filiais;
- catálogo de serviços;
- criação e gestão de agendamentos;
- histórico;
- dashboard;
- validações de negócio críticas.

---

## 3. Objetivos de teste

- comprovar aderência aos requisitos funcionais do MVP;
- validar regras de negócio de maior risco;
- gerar evidências para homologação;
- reduzir risco de regressão nos fluxos principais;
- apoiar o relatório final de bugs.

---

## 4. Tipos de teste previstos

| Tipo | Finalidade |
|---|---|
| Teste funcional | Validar comportamento esperado por fluxo de negócio |
| Teste de regra de negócio | Confirmar validações, bloqueios e permissões |
| Teste de integração | Verificar coerência entre cadastro, agenda e dashboard |
| Teste de homologação | Confirmar aderência do MVP aos critérios de aceite |

---

## 4.1 Padrão de organização no board

Para manter alinhamento com o backlog e com o board de sprints, este plano de testes adota a mesma lógica de acompanhamento:

| Elemento | Uso no plano |
|---|---|
| `RF` | Identifica o card funcional principal testado |
| `CA` | Identifica critérios de aceite críticos e homologatórios |
| `BACKEND` | Cobertura de regras, persistência e validações do servidor |
| `FRONTEND` | Cobertura de tela, fluxo e mensagens para o usuário |
| `INTERLIGADOS` | Cobertura do comportamento integrado entre frontend e backend |
| `BANCO DE DADOS` | Cobertura de constraints, integridade e consistência dos dados |

---

## 5. Ambiente de teste

| Item | Definição |
|---|---|
| Ambiente | Homologação ou ambiente controlado de validação |
| Base de dados | Massa de teste com clientes, veículos, filiais e serviços cadastrados |
| Perfis | Administrador e funcionário |
| Evidência | Captura de tela, vídeo curto, checklist ou registro textual estruturado |

---

## 6. Massa mínima de teste

Para executar os testes, recomenda-se preparar:

- 2 usuários internos ativos;
- 1 usuário inativo;
- 2 filiais ativas;
- 2 clientes cadastrados;
- ao menos 2 veículos;
- ao menos 2 filiados vinculados;
- ao menos 3 serviços ativos;
- horários que permitam validar capacidade e conflito global.

---

## 7. Cobertura de testes por sprint

Esta seção espelha o planejamento do board e distribui a validação funcional conforme a entrada dos cards nas sprints.

### 7.1 Sprint 01 - Semanas 1-2

**Foco de teste:** base técnica, autenticação, clientes e usuários.

| Card | Labels | Cobertura de teste |
|---|---|---|
| B001 - Estruturação completa do banco de dados | `BANCO DE DADOS`, `BACKEND` | Validar tabelas, constraints, unicidade e persistência inicial |
| RF001 - Autenticação com login e senha para usuários internos | `BACKEND`, `FRONTEND` | Login com credenciais válidas e inválidas |
| RF002 - Cadastro de clientes com dados cadastrais e observações | `BACKEND`, `FRONTEND` | Salvar e consultar cliente com observações |
| RF003 - Validação de limites e formatos dos campos do cliente | `INTERLIGADOS` | Bloqueio de formatos inválidos e mensagens claras |
| RF014 - Cadastro básico de usuários internos | `BACKEND`, `FRONTEND` | Cadastro, ativação e bloqueio de usuário inativo |

### 7.2 Sprint 02 - Semanas 3-4

**Foco de teste:** veículos, serviços e extensão do fluxo de cliente.

| Card | Labels | Cobertura de teste |
|---|---|---|
| RF004 - Cadastro de veículos vinculados a cliente existente | `INTERLIGADOS` | Veículo obrigatório com cliente existente |
| RF005 - Validação de placa e bloqueio de duplicidade | `INTERLIGADOS` | Rejeição de placa duplicada |
| RF006 - Catálogo de serviços com tipo, preço e duração | `BACKEND`, `FRONTEND` | Cadastro, edição e listagem do catálogo |
| RF021 - Adicionar veículo no fluxo de cadastro de cliente | `BACKEND`, `FRONTEND` | Inclusão de veículo no mesmo fluxo do cliente |
| RF022 - Exibir veículos do cliente na visualização detalhada | `BACKEND`, `FRONTEND` | Exibição correta dos veículos vinculados |

### 7.3 Sprint 03 - Semanas 5-6

**Foco de teste:** criação do agendamento e navegação inicial da agenda.

| Card | Labels | Cobertura de teste |
|---|---|---|
| RF007 - Criação de agendamento com cliente, veículo e serviços | `BACKEND`, `FRONTEND` | Criação do agendamento com todos os vínculos obrigatórios |
| RF007.1 - Agendamento: etapa de cliente e veículo | `FRONTEND`, `INTERLIGADOS` | Avanço correto entre etapas do fluxo |
| RF007.2 - Agendamento: seleção de múltiplos serviços | `FRONTEND`, `INTERLIGADOS` | Inclusão de um ou mais serviços |
| RF007.3 - Agendamento: resumo e confirmação final | `FRONTEND`, `INTERLIGADOS` | Revisão final antes da gravação |
| RF009 - Visualização da agenda em formato simples e detalhado | `BACKEND`, `FRONTEND` | Consulta e detalhe da agenda |
| RF015 - Confirmação das informações antes de concluir agendamento | `BACKEND`, `FRONTEND` | Confirmação explícita antes de salvar |

### 7.4 Sprint 04 - Semanas 7-8

**Foco de teste:** simultaneidade, status, observações e histórico.

| Card | Labels | Cobertura de teste |
|---|---|---|
| RF008 - Permitir agendamentos simultâneos no mesmo horário | `BACKEND`, `FRONTEND` | Permitir simultaneidade quando houver capacidade |
| RF008.1 a RF008.3 - Agenda simultânea | `FRONTEND`, `INTERLIGADOS` | Exibição, criação sem bloqueio indevido e tratamento de conflitos |
| RF010 - Cancelamento e bloqueio de edição de agendamento finalizado | `BACKEND`, `FRONTEND` | Cancelar e bloquear edição de finalizado |
| RF011 - Registro de observações logísticas por agendamento | `BACKEND`, `FRONTEND` | Salvar e consultar observações |
| RF012 - Consulta de histórico de atendimentos por cliente | `BACKEND`, `FRONTEND` | Retorno cronológico do histórico |

### 7.5 Sprint 05 - Semanas 9-10

**Foco de teste:** filiais, capacidade e conflito global.

| Card | Labels | Cobertura de teste |
|---|---|---|
| RF017 - Cadastro de filiais para operação multiunidade | `BACKEND`, `FRONTEND` | Cadastro e consulta de filiais |
| RF018 - Configuração de células ativas por filial entre 1 e 100 | `BACKEND`, `FRONTEND` | Aceite da faixa válida e bloqueio de inválida |
| RF019 - Seleção obrigatória de filial na criação do agendamento | `BACKEND`, `FRONTEND` | Falha obrigatória sem filial |
| RF020 - Bloqueio de conflito do mesmo veículo no mesmo horário | `BACKEND`, `FRONTEND` | Bloqueio do conflito na mesma ou em outra filial |

### 7.6 Sprint 06 - Semanas 11-12

**Foco de teste:** indicadores, tema e filiados.

| Card | Labels | Cobertura de teste |
|---|---|---|
| RF013 - Dashboard com métricas operacionais e financeiras | `BACKEND`, `FRONTEND` | Exibir total de atendimentos, ocupação e faturamento |
| RF016 - Alternância entre tema claro e escuro | `BACKEND`, `FRONTEND` | Troca de tema e persistência da preferência |
| RF023 - Cadastro de filiados vinculados ao cliente titular | `BACKEND`, `FRONTEND` | Cadastro correto do filiado com dados mínimos |
| RF024 - Seleção de filiado no momento do agendamento | `BACKEND`, `FRONTEND` | Registro do titular e do filiado no atendimento |

---

## 8. Cenários de teste prioritários por card

| ID | Card | Labels | Cenário | Resultado esperado |
|---|---|---|---|---|
| CT-001 | RF001 | `BACKEND`, `FRONTEND` | Realizar login com credenciais válidas | Usuário autenticado com sucesso |
| CT-002 | RF014 | `BACKEND`, `FRONTEND` | Tentar login com usuário inativo | Acesso bloqueado |
| CT-003 | RF002 | `BACKEND`, `FRONTEND` | Cadastrar cliente com dados válidos | Cliente salvo com sucesso |
| CT-004 | RF003 | `INTERLIGADOS` | Cadastrar cliente com dado inválido | Registro bloqueado com mensagem clara |
| CT-005 | RF004 | `INTERLIGADOS` | Cadastrar veículo vinculado a cliente | Veículo salvo corretamente |
| CT-006 | RF005 | `INTERLIGADOS` | Tentar cadastrar placa duplicada | Operação bloqueada |
| CT-007 | RF023 | `BACKEND`, `FRONTEND` | Cadastrar filiado com telefone e CPF ou RG | Filiado salvo corretamente |
| CT-008 | RF017, RF018 | `BACKEND`, `FRONTEND` | Cadastrar filial com capacidade válida | Filial salva com sucesso |
| CT-009 | RF018, CA008 | `BACKEND`, `FRONTEND` | Informar capacidade fora da faixa 1 a 100 | Operação bloqueada |
| CT-010 | RF007, RF019 | `BACKEND`, `FRONTEND` | Criar agendamento com cliente, veículo, serviço e filial | Agendamento salvo com sucesso |
| CT-011 | RF019, CA007 | `BACKEND`, `FRONTEND` | Tentar criar agendamento sem filial | Operação bloqueada |
| CT-012 | RF008 | `BACKEND`, `FRONTEND` | Criar agendamentos simultâneos dentro da capacidade | Operações permitidas |
| CT-013 | RF018, RN009 | `BACKEND`, `INTERLIGADOS` | Exceder capacidade da filial | Operação bloqueada |
| CT-014 | RF020, CA006 | `BACKEND`, `FRONTEND` | Tentar conflito do mesmo veículo no mesmo horário em outra filial | Operação bloqueada |
| CT-015 | RF024, CA009 | `BACKEND`, `FRONTEND` | Selecionar filiado autorizado no agendamento | Agendamento registra titular e filiado |
| CT-016 | RF023, RF024, CA010 | `INTERLIGADOS` | Cadastrar novo filiado e validar disponibilidade no próximo agendamento | Novo filiado aparece para seleção |
| CT-017 | RF010 | `BACKEND`, `FRONTEND` | Finalizar agendamento e tentar editar | Edição bloqueada |
| CT-018 | RF011 | `BACKEND`, `FRONTEND` | Registrar observação logística | Informação fica disponível em consulta |
| CT-019 | RF012 | `BACKEND`, `FRONTEND` | Consultar histórico por cliente | Lista cronológica retornada |
| CT-020 | RF013 | `BACKEND`, `FRONTEND` | Consultar dashboard | Indicadores básicos exibidos |

---

## 9. Cobertura dos critérios de aceite críticos

| CA | Validação prevista |
|---|---|
| CA006 | Bloqueio do mesmo veículo no mesmo horário em qualquer filial |
| CA007 | Falha obrigatória de agendamento sem filial |
| CA008 | Aceite apenas de capacidade inteira entre 1 e 100 |
| CA009 | Registro do filiado autorizado no agendamento |
| CA010 | Disponibilização imediata do novo filiado para uso |
| CA011 | Execução e evidência de todos os testes críticos anteriores |

---

## 10. Matriz resumida de cobertura RF x CA

| Card / RF | Critérios de aceite ou regra crítica relacionados |
|---|---|
| RF007 | CA001, CA002 |
| RF008 | RN005 |
| RF010 | RN004 |
| RF017 | CA001 |
| RF018 | CA008 |
| RF019 | CA007 |
| RF020 | CA006 |
| RF023 | CA010 |
| RF024 | CA009 |

---

## 11. Critérios de entrada

Os testes devem começar quando:

1. o backlog prioritário estiver refinado;
2. os fluxos principais estiverem disponíveis para validação;
3. a massa mínima de teste estiver preparada;
4. as regras de negócio críticas estiverem identificadas;
5. o ambiente estiver estável para execução.

---

## 12. Critérios de saída

Os testes podem ser considerados concluídos quando:

1. os cenários prioritários tiverem sido executados;
2. houver evidência registrada para os critérios críticos;
3. bugs de severidade alta estiverem corrigidos ou formalmente aceitos;
4. o relatório de bugs estiver atualizado;
5. o parecer de homologação estiver apto para apresentação.

---

## 13. Registro de evidências

| Campo | Descrição |
|---|---|
| ID do teste | Identificador único do cenário |
| Data | Data da execução |
| Responsável | Quem executou o teste |
| Resultado | Aprovado, reprovado ou bloqueado |
| Evidência | Print, vídeo, comentário ou link |
| Observação | Notas relevantes |

---

## 14. Responsabilidades

| Papel | Responsabilidade |
|---|---|
| Analista | Garantir cobertura de requisitos e critérios de aceite |
| QA/Testador | Executar testes e registrar evidências |
| Desenvolvedor | Corrigir falhas identificadas |
| PO/Cliente | Participar da homologação funcional final |

---

## 15. Referências

- `drp.md`
- `us.md`
- `5-gdr.md`
- `especificacao-tecnica-regras-e-dados.md`
- `definicao-de-endpoints-e-regras-de-negocio.md`






