# Definição de Endpoints e Regras de Negócio - CarWash

## 1. Objetivo

Este documento consolida os endpoints principais do sistema e os relaciona às respectivas regras de negócio, em formato funcional e analítico.

Seu papel é complementar `api-banco-carwash-especificacao-completa.md` com uma visão mais direta para backlog, validação e apresentação acadêmica.

---

## 2. Diretrizes gerais

- Base da API: `/api/v1`
- Formato de comunicação: JSON
- Autenticação obrigatória para endpoints de negócio
- Identificadores: UUID
- Datas e horários em padrão ISO-8601

---

## 3. Mapa de endpoints no board

Esta seção relaciona os grupos de endpoints aos cards funcionais, às sprints e às frentes de trabalho do board.

### 3.1 Padrão de organização

| Elemento | Uso neste documento |
|---|---|
| `RF` | Identifica o card funcional principal relacionado ao endpoint |
| `Sprint` | Indica em qual sprint o grupo de endpoints ganha foco principal |
| `BACKEND` | Define a frente principal de implementação da API |
| `INTERLIGADOS` | Indica forte dependência de integração com fluxos do frontend |

### 3.2 Mapa resumido por sprint

| Sprint | Cards principais | Grupos de endpoints relacionados |
|---|---|---|
| Sprint 01 | `B001`, `RF001`, `RF002`, `RF003`, `RF014` | Autenticação, usuários e clientes |
| Sprint 02 | `RF004`, `RF005`, `RF006`, `RF021`, `RF022` | Veículos, serviços e clientes |
| Sprint 03 | `RF007`, `RF007.1`, `RF007.2`, `RF007.3`, `RF009`, `RF015` | Agendamentos e consulta da agenda |
| Sprint 04 | `RF008`, `RF010`, `RF011`, `RF012` | Agendamentos, histórico e regras de estado |
| Sprint 05 | `RF017`, `RF018`, `RF019`, `RF020` | Filiais e regras críticas multiunidade |
| Sprint 06 | `RF013`, `RF016`, `RF023`, `RF024` | Dashboard e filiados |

---

## 4. Endpoints por domínio

Os endpoints abaixo foram organizados por domínio funcional para facilitar a rastreabilidade entre requisitos, backlog, regras de negócio e validação.

## 4.1 Autenticação

| Método | Endpoint | Objetivo funcional | Card / Sprint | Labels | Regras relacionadas |
|---|---|---|---|---|---|
| POST | `/auth/login` | Autenticar usuário interno | `RF001` / Sprint 01 | `BACKEND`, `INTERLIGADOS` | RF001, RNF003 |
| POST | `/auth/refresh` | Renovar sessão autenticada | `RF001` / Sprint 01 | `BACKEND` | RNF004 |
| POST | `/auth/logout` | Encerrar sessão | `RF001` / Sprint 01 | `BACKEND` | RNF004 |

## 4.2 Usuários

| Método | Endpoint | Objetivo funcional | Card / Sprint | Labels | Regras relacionadas |
|---|---|---|---|---|---|
| GET | `/usuarios` | Listar usuários internos | `RF014` / Sprint 01 | `BACKEND` | RF014 |
| POST | `/usuarios` | Cadastrar usuário interno | `RF014` / Sprint 01 | `BACKEND` | RF014 |
| GET | `/usuarios/{id}` | Consultar usuário específico | `RF014` / Sprint 01 | `BACKEND` | RF014 |
| PATCH | `/usuarios/{id}` | Atualizar dados do usuário | `RF014` / Sprint 01 | `BACKEND` | RF014 |
| PATCH | `/usuarios/{id}/status` | Ativar ou inativar usuário | `RF014` / Sprint 01 | `BACKEND` | RF014, RNF003 |

## 4.3 Filiais

| Método | Endpoint | Objetivo funcional | Card / Sprint | Labels | Regras relacionadas |
|---|---|---|---|---|---|
| GET | `/filiais` | Listar filiais | `RF017` / Sprint 05 | `BACKEND` | RF017 |
| POST | `/filiais` | Cadastrar filial | `RF017` / Sprint 05 | `BACKEND` | RF017 |
| GET | `/filiais/{id}` | Consultar filial | `RF017` / Sprint 05 | `BACKEND` | RF017 |
| PATCH | `/filiais/{id}` | Atualizar filial e capacidade | `RF017`, `RF018` / Sprint 05 | `BACKEND` | RF017, RF018, RN009 |

## 4.4 Clientes

| Método | Endpoint | Objetivo funcional | Card / Sprint | Labels | Regras relacionadas |
|---|---|---|---|---|---|
| GET | `/clientes` | Listar clientes | `RF002` / Sprint 01 | `BACKEND` | RF002 |
| POST | `/clientes` | Cadastrar cliente | `RF002`, `RF003` / Sprint 01 | `BACKEND`, `INTERLIGADOS` | RF002, RF003 |
| GET | `/clientes/{id}` | Consultar detalhe do cliente | `RF002`, `RF022` / Sprints 01 e 02 | `BACKEND` | RF002, RF022 |
| PATCH | `/clientes/{id}` | Atualizar cadastro do cliente | `RF002`, `RF003` / Sprint 01 | `BACKEND` | RF002, RF003 |
| GET | `/clientes/{id}/historico` | Consultar histórico de atendimentos | `RF012` / Sprint 04 | `BACKEND` | RF012 |

## 4.5 Filiados

| Método | Endpoint | Objetivo funcional | Card / Sprint | Labels | Regras relacionadas |
|---|---|---|---|---|---|
| GET | `/clientes/{clienteId}/filiados` | Listar filiados do titular | `RF023` / Sprint 06 | `BACKEND` | RF023 |
| POST | `/clientes/{clienteId}/filiados` | Cadastrar filiado | `RF023` / Sprint 06 | `BACKEND`, `INTERLIGADOS` | RF023, RN013, RN014 |
| PATCH | `/clientes/{clienteId}/filiados/{id}` | Atualizar filiado | `RF023` / Sprint 06 | `BACKEND` | RF023 |
| PATCH | `/clientes/{clienteId}/filiados/{id}/status` | Ativar ou inativar filiado | `RF023` / Sprint 06 | `BACKEND` | RF023 |

## 4.6 Veículos

| Método | Endpoint | Objetivo funcional | Card / Sprint | Labels | Regras relacionadas |
|---|---|---|---|---|---|
| GET | `/clientes/{clienteId}/veiculos` | Listar veículos do cliente | `RF022` / Sprint 02 | `BACKEND` | RF022, RN012 |
| POST | `/clientes/{clienteId}/veiculos` | Cadastrar veículo vinculado | `RF004`, `RF021` / Sprint 02 | `BACKEND`, `INTERLIGADOS` | RF004, RF021 |
| PATCH | `/clientes/{clienteId}/veiculos/{id}` | Atualizar veículo | `RF004`, `RF005` / Sprint 02 | `BACKEND`, `INTERLIGADOS` | RF004, RF005 |
| GET | `/veiculos/{id}` | Consultar veículo | `RF022` / Sprint 02 | `BACKEND` | RF022 |

## 4.7 Serviços

| Método | Endpoint | Objetivo funcional | Card / Sprint | Labels | Regras relacionadas |
|---|---|---|---|---|---|
| GET | `/servicos` | Listar catálogo de serviços | `RF006` / Sprint 02 | `BACKEND` | RF006 |
| POST | `/servicos` | Cadastrar serviço | `RF006` / Sprint 02 | `BACKEND` | RF006 |
| PATCH | `/servicos/{id}` | Atualizar serviço | `RF006` / Sprint 02 | `BACKEND` | RF006 |
| PATCH | `/servicos/{id}/status` | Ativar ou inativar serviço | `RF006` / Sprint 02 | `BACKEND` | RF006 |

## 4.8 Agendamentos

| Método | Endpoint | Objetivo funcional | Card / Sprint | Labels | Regras relacionadas |
|---|---|---|---|---|---|
| GET | `/agendamentos` | Listar agenda com filtros | `RF009` / Sprint 03 | `BACKEND`, `INTERLIGADOS` | RF009 |
| POST | `/agendamentos` | Criar agendamento | `RF007`, `RF015`, `RF019`, `RF020` / Sprints 03 e 05 | `BACKEND`, `INTERLIGADOS` | RF007, RF015, RF019, RF020 |
| GET | `/agendamentos/{id}` | Consultar detalhe do agendamento | `RF009` / Sprint 03 | `BACKEND` | RF009 |
| PATCH | `/agendamentos/{id}` | Editar agendamento em estado permitido | `RF010` / Sprint 04 | `BACKEND`, `INTERLIGADOS` | RN004, RN006 |
| PATCH | `/agendamentos/{id}/cancelar` | Cancelar agendamento | `RF010` / Sprint 04 | `BACKEND` | RF010 |
| PATCH | `/agendamentos/{id}/finalizar` | Finalizar atendimento | `RF010` / Sprint 04 | `BACKEND` | RF010 |
| GET | `/agendamentos/{id}/historico` | Consultar histórico do agendamento | `RF012` / Sprint 04 | `BACKEND` | RN007 |

## 4.9 Dashboard

| Método | Endpoint | Objetivo funcional | Card / Sprint | Labels | Regras relacionadas |
|---|---|---|---|---|---|
| GET | `/dashboard/resumo` | Exibir indicadores gerais | `RF013` / Sprint 06 | `BACKEND`, `INTERLIGADOS` | RF013 |
| GET | `/dashboard/ocupacao` | Exibir taxa de ocupação | `RF013` / Sprint 06 | `BACKEND`, `INTERLIGADOS` | RF013 |
| GET | `/dashboard/faturamento` | Exibir faturamento estimado | `RF013` / Sprint 06 | `BACKEND`, `INTERLIGADOS` | RF013 |

---

## 5. Regras de negócio por domínio

## 5.1 Regras de cadastro

| ID | Regra |
|---|---|
| RN002 | Todo veículo deve estar vinculado a um cliente |
| RN003 | Não pode existir placa duplicada no sistema |
| RN012 | O cliente deve exibir seus veículos vinculados |
| RN013 | Filiado pode agir em nome do titular quando vinculado |
| RN014 | Novo filiado deve ser cadastrado antes da conclusão do agendamento |

## 5.2 Regras de agenda

| ID | Regra |
|---|---|
| RN004 | Agendamento finalizado não pode ser editado |
| RN005 | Agendamentos simultâneos são permitidos conforme capacidade |
| RN006 | Serviços podem ser alterados enquanto o agendamento estiver ativo |
| RN007 | Alterações relevantes devem gerar histórico |
| RN009 | Filial deve possuir de 1 a 100 células ativas |
| RN010 | Todo agendamento deve possuir filial obrigatória |
| RN011 | O mesmo veículo não pode ter conflito de horário em nenhuma filial |

---

## 6. Validações críticas de entrada

| Validação | Quando aplicar | Resultado esperado |
|---|---|---|
| Cliente obrigatório | Criação de agendamento | Bloquear quando ausente |
| Veículo vinculado ao cliente | Criação e edição de agendamento | Bloquear quando inconsistente |
| Filiado vinculado ao titular | Criação de agendamento | Bloquear quando não autorizado |
| Filial obrigatória | Criação de agendamento | Bloquear quando ausente |
| Capacidade da filial | Criação de agendamento | Bloquear quando capacidade estiver excedida |
| Conflito global de veículo | Criação e edição de agendamento | Bloquear duplicidade de horário |
| Status do agendamento | Edição/cancelamento/finalização | Permitir somente transições válidas |

---

## 7. Erros de negócio esperados

| Código de negócio sugerido | Situação |
|---|---|
| AGENDAMENTO_SEM_FILIAL | Tentativa de salvar agendamento sem filial |
| CAPACIDADE_FILIAL_EXCEDIDA | Capacidade operacional da filial excedida |
| AGENDAMENTO_CONFLITO_GLOBAL | Mesmo veículo no mesmo horário em outra janela ativa |
| VEICULO_NAO_PERTENCE_AO_CLIENTE | Veículo inconsistente com o titular |
| FILIADO_NAO_AUTORIZADO | Filiado não vinculado ao cliente titular |
| AGENDAMENTO_FINALIZADO_IMUTAVEL | Tentativa de editar agendamento finalizado |

---

## 8. Matriz resumida de rastreabilidade com o board

| Grupo de endpoints | Cards relacionados | Sprint principal | Frente dominante |
|---|---|---|---|
| Autenticação | `RF001` | Sprint 01 | `BACKEND` |
| Usuários | `RF014` | Sprint 01 | `BACKEND` |
| Clientes | `RF002`, `RF003`, `RF012` | Sprints 01 e 04 | `BACKEND`, `INTERLIGADOS` |
| Veículos | `RF004`, `RF005`, `RF021`, `RF022` | Sprint 02 | `INTERLIGADOS` |
| Serviços | `RF006` | Sprint 02 | `BACKEND` |
| Agendamentos | `RF007`, `RF008`, `RF009`, `RF010`, `RF011`, `RF015`, `RF019`, `RF020` | Sprints 03, 04 e 05 | `BACKEND`, `INTERLIGADOS` |
| Filiais | `RF017`, `RF018`, `RF019`, `RF020` | Sprint 05 | `BACKEND` |
| Dashboard | `RF013` | Sprint 06 | `BACKEND`, `INTERLIGADOS` |
| Filiados | `RF023`, `RF024` | Sprint 06 | `BACKEND`, `INTERLIGADOS` |

---

## 9. Rastreabilidade resumida

| Entregável | Base principal |
|---|---|
| Escopo funcional | `dvp-e.md` |
| Requisitos e regras | `drp.md` |
| Histórias de usuário | `us.md` |
| Endpoints completos | `api-banco-carwash-especificacao-completa.md` |
| Regras técnicas verificáveis | `especificacao-tecnica-regras-e-dados.md` |

---

## 10. Conclusão

Os endpoints definidos atendem ao escopo funcional do CarWash e estão alinhados às regras de negócio já aprovadas no projeto.

Este documento pode ser utilizado como elemento de ligação entre:

- backlog funcional;
- especificação da API;
- plano de testes;
- apresentação da entrega do analista.





