# 🚗 CarWash - Gestão de Estética Automotiva

O **CarWash** é um sistema ERP web de gestão interna projetado para profissionalizar estabelecimentos de estética automotiva. Ele substitui controles manuais por uma plataforma centralizada que gerencia desde a agenda inteligente até a capacidade operacional de múltiplas filiais.

---

## 🎯 Problemas que Resolvemos

O sistema foi projetado para atacar diretamente as seguintes dores de negócio:

* **Desorganização:** Elimina agendamentos via WhatsApp ou papel.
* **Ociosidade:** Otimiza a agenda com suporte a atendimentos simultâneos.
* **Conflitos de Frota:** Bloqueia o agendamento do mesmo veículo em horários sobrepostos, mesmo entre filiais diferentes.
* **Gestão de Capacidade:** Ajuste dinâmico de células de lavagem ativas (1 a 100 por unidade).

---

## ✨ Funcionalidades do MVP

### 📅 Agenda e Operação

* **Agendamento Inteligente:** Exige seleção de filial e valida conflitos globais de veículos.
* **Gestão Multiunidade:** Controle individual de capacidade e células por filial.
* **Responsáveis Vinculados:** Permite que pessoas autorizadas (além do titular) levem o veículo para atendimento.

### 👥 Cadastros e Dados

* **Clientes e Veículos:** Vínculo obrigatório entre dono e carro, com validação de placa única no sistema.
* **Catálogo de Serviços:** Definição de preços e tempos de duração estimada para cada tipo de lavagem.
* **Observações Logísticas:** Registro de estado do veículo e cuidados específicos por atendimento.

### 📊 Gestão e Segurança

* **Dashboard:** Indicadores de ocupação, faturamento estimado e total de atendimentos.
* **Segurança:** Autenticação via login/senha, sessões protegidas e tráfego HTTPS.
* **Temas:** Suporte nativo a Modo Claro e Modo Escuro (Dark Mode).

---

## 🛠️ Stack Tecnológica

| Camada | Tecnologia | Justificativa |
| --- | --- | --- |
| **Frontend** | React | Alta performance e componentização para UI responsiva. |
| **Backend** | .NET (C#) | Robustez para regras de negócio e modelagem POO. |
| **Banco de Dados** | PostgreSQL | Integridade transacional e consistência de dados (ACID). |

---

## 📂 Estrutura de Documentação Técnica

O projeto é guiado por 4 documentos fundamentais disponíveis na pasta `/docs`:

1. **DVP-E:** Visão do Produto, Escopo e priorização MoSCoW.
2. **DVS:** Estudo de viabilidade técnica, econômica e operacional.
3. **DRP:** Requisitos funcionais detalhados e critérios de aceitação.
4. **DAT:** Arquitetura lógica, modelo de dados e infraestrutura.

---

## 🚀 Como rodar o ambiente

O projeto roda 100% em Docker (backend .NET + frontend React + PostgreSQL). Para padronizar os comandos do time, há dois wrappers equivalentes que chamam `docker compose` por baixo:

* **Linux / macOS / WSL2** → `make` (usa o `Makefile`)
* **Windows (PowerShell nativo)** → `.\make.ps1` (usa o `make.ps1`)

Os dois suportam os mesmos alvos: `up`, `down`, `restart`, `logs`, `ps`, `build`, `pull`, `migrate`, `shell-back`, `shell-front`, `shell-db`, `backup`, `smoke`, `certs-dev`, `clean`. Rode `make help` ou `.\make.ps1 help` para a lista completa.

### Pré-requisitos

* Docker Desktop (com WSL2 backend, no Windows) ou Docker Engine + Compose v2 (Linux/macOS)
* Git
* **Windows apenas:** PowerShell 5+ (já vem no Windows 10/11)
* **Linux/macOS apenas:** GNU `make`

### Clonando

```bash
git clone https://github.com/MatheusMoreira08/CarWash.git
cd CarWash
```

### Linux / macOS / WSL2

```bash
make up                          # sobe ambiente dev em segundo plano
make logs ENV=dev SVC=backend    # segue os logs do backend
make shell-db ENV=dev            # abre psql no container do postgres
make migrate ENV=dev             # aplica migrations EF Core
make smoke ENV=hom               # smoke test pós-deploy
make down ENV=dev                # derruba mantendo volumes
```

Variáveis aceitas: `ENV=dev|hom|prod` (default `dev`) e `SVC=<serviço>` para `logs` e `restart`.

### Windows (PowerShell)

Na **primeira vez**, libere a execução de scripts locais (uma vez por usuário):

```powershell
Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
```

Depois, sempre rodando da raiz do repositório:

```powershell
.\make.ps1 up                            # sobe ambiente dev em segundo plano
.\make.ps1 logs -Env dev -Svc backend    # segue os logs do backend
.\make.ps1 shell-db -Env dev             # abre psql no container do postgres
.\make.ps1 migrate -Env dev              # aplica migrations EF Core
.\make.ps1 smoke -Env hom                # smoke test pós-deploy
.\make.ps1 down -Env dev                 # derruba mantendo volumes
```

Parâmetros aceitos: `-Env dev|hom|prod` (default `dev`) e `-Svc <serviço>` para `logs` e `restart`.

> **Alternativa Windows:** quem já usa WSL2 pode rodar `make` diretamente lá dentro — o comportamento é idêntico ao Linux.

### Dica

Após `make up` / `.\make.ps1 up`, os serviços ficam disponíveis nas portas definidas no `docker-compose.dev.yml` (backend, frontend e PostgreSQL). Para limpar tudo (containers, volumes e redes) use `make clean` ou `.\make.ps1 clean` — **isso apaga o banco de desenvolvimento**.
