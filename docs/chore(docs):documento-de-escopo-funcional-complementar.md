# Documento de Escopo Funcional Complementar - CarWash

## 1. Objetivo

O presente documento consolida o escopo funcional do projeto CarWash sob a perspectiva do Analista de Projetos, complementando o conteúdo já definido em `dvp-e.md` e `drp.md`.

Seu propósito é explicitar, de forma objetiva, o que integra o MVP, o que permanece fora do escopo imediato e quais regras orientam a entrega.

---

## 2. Declaração de escopo

O projeto CarWash tem como objetivo disponibilizar um sistema web interno para estabelecimentos de estética automotiva, centralizando:

- autenticação de usuários internos;
- cadastro de clientes, veículos e filiados;
- catálogo de serviços;
- agenda digital com controle por filial;
- registro de observações e histórico;
- dashboard com métricas operacionais;
- estrutura básica de governança documental e rastreabilidade.

---

## 3. Problemas de negócio cobertos

| ID | Problema | Como o escopo responde |
|---|---|---|
| P1 | Agendamento manual e descentralizado | Agenda digital com visualização simples e detalhada |
| P2 | Ausência de cadastro estruturado | Cadastro centralizado de clientes, veículos e filiados |
| P3 | Falta de observações operacionais | Campo de observações logísticas por agendamento |
| P4 | Ausência de histórico | Consulta de atendimentos por cliente |
| P5 | Falta de visibilidade do negócio | Dashboard com métricas básicas |
| P6 | Capacidade operacional rígida | Configuração de células ativas por filial |
| P7 | Conflito entre filiais | Filial obrigatória e bloqueio global por veículo e horário |

---

## 4. Escopo funcional do MVP

### 4.1 Incluído no MVP

| Domínio | Escopo funcional |
|---|---|
| Acesso | Login de usuários internos com controle de sessão |
| Clientes | Cadastro, consulta e validação de dados principais |
| Veículos | Cadastro vinculado ao cliente e bloqueio de placa duplicada |
| Filiados | Cadastro de autorizados vinculados ao cliente titular |
| Serviços | Catálogo básico com nome, preço e duração |
| Filiais | Cadastro de filiais e parametrização de capacidade |
| Agenda | Criação, consulta, cancelamento e finalização de agendamentos |
| Regras de agenda | Filial obrigatória, capacidade por filial e bloqueio global de conflito |
| Operação | Registro de observações logísticas e histórico de alterações |
| Gestão | Dashboard com total de atendimentos, ocupação e faturamento estimado |

### 4.2 Regras críticas do escopo

- Todo agendamento deve possuir cliente, veículo, serviço e filial.
- O mesmo veículo não pode possuir dois agendamentos no mesmo horário, na mesma filial ou em filiais diferentes.
- A capacidade operacional da filial deve respeitar a faixa de 1 a 100 células ativas.
- Filiados só podem atuar em nome do cliente titular se estiverem previamente vinculados ao cadastro.
- Agendamento finalizado não pode ser editado.

---

## 5. Itens fora do escopo imediato

| Item | Situação |
|---|---|
| Marketplace ou portal para cliente final | Fora do escopo do MVP |
| Pagamentos integrados | Fora do escopo do MVP |
| Funcionamento offline | Fora do escopo do MVP |
| Recuperação de senha automatizada | Evolução futura |
| Exportação avançada de relatórios | Evolução futura |
| Permissões detalhadas por perfil | Evolução futura |
| Automação completa de notificações | Dependente de CR e evolução formal de escopo |

---

## 6. Stakeholders e responsabilidades funcionais

| Papel | Responsabilidade no escopo |
|---|---|
| Proprietário | Validar a aderência do sistema ao processo real do negócio |
| Administrador | Operar todas as funções do sistema no MVP |
| Funcionário | Executar agenda, cadastros e operação diária |
| Analista de Projetos | Manter escopo, requisitos, backlog e consistência documental |
| Equipe técnica | Implementar a solução conforme a base documental aprovada |

---

## 7. Premissas do escopo

- O sistema será utilizado apenas internamente.
- A operação depende de acesso à internet.
- O cliente participará de validações durante o andamento do projeto.
- A equipe usará ferramentas gratuitas sempre que possível.
- O backlog deverá priorizar, em primeiro lugar, os requisitos Must.

---

## 8. Restrições do projeto

- O prazo de execução deve caber dentro do bimestre.
- A entrega deve manter rastreabilidade entre escopo, requisitos, backlog, testes e bugs.
- O projeto deve buscar custo zero ou o menor custo possível com ferramentas gratuitas.
- Mudanças estruturais de escopo devem seguir governança documental.

---

## 9. Critérios de aceite do escopo

O escopo será considerado adequadamente definido quando:

1. houver alinhamento entre problema de negócio, requisitos e backlog;
2. os itens do MVP estiverem claramente separados dos itens futuros;
3. as regras de negócio críticas estiverem explicitadas;
4. os entregáveis obrigatórios do papel de analista estiverem produzidos e localizáveis;
5. a equipe conseguir derivar endpoints, testes e tarefas a partir da documentação.

---

## 10. Referências

- `dvp-e.md`
- `drp.md`
- `dvs.md`
- `5-gdr.md`
- `us.md`





