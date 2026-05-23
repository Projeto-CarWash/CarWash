# Makefile do CarWash — comandos comuns para o time
# Sempre rode da raiz do repo.
#
# =============================================================================
# COMO USAR
# =============================================================================
#
# --- Linux / macOS / WSL ---------------------------------------------------
#   Pré-requisitos: GNU make, docker, docker compose v2.
#   Sintaxe:
#       make <alvo> [ENV=dev|hom|prod] [SVC=backend|frontend|postgres]
#   Exemplos:
#       make up                       # sobe ambiente dev
#       make up ENV=hom               # sobe homologação
#       make logs ENV=dev SVC=backend # segue logs do backend
#       make shell-db ENV=dev         # abre psql no postgres
#       make smoke ENV=hom            # smoke test
#
# --- Windows (PowerShell) --------------------------------------------------
#   Devs no Windows NÃO precisam de `make`. Use o script `make.ps1` ao lado
#   deste arquivo, que espelha exatamente os mesmos alvos via docker compose.
#
#   Pré-requisitos: Docker Desktop (com WSL2 backend) e PowerShell 5+.
#   Na primeira vez, libere a execução de scripts locais (uma vez por usuário):
#       Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
#
#   Sintaxe:
#       .\make.ps1 <alvo> [-Env dev|hom|prod] [-Svc backend|frontend|postgres]
#   Exemplos:
#       .\make.ps1 up                          # sobe ambiente dev
#       .\make.ps1 up -Env hom                 # sobe homologação
#       .\make.ps1 logs -Env dev -Svc backend  # segue logs do backend
#       .\make.ps1 shell-db -Env dev           # abre psql no postgres
#       .\make.ps1 smoke -Env hom              # smoke test
#       .\make.ps1 help                        # lista os alvos
#
#   Alternativa Windows: rode `make` dentro do WSL2 — funciona idêntico ao Linux.
#
# =============================================================================

SHELL := /bin/bash

ENV ?= dev
COMPOSE_FILES := -f docker-compose.yml -f docker-compose.$(ENV).yml
COMPOSE := docker compose $(COMPOSE_FILES)
SAFE_COMPOSE_FILES := -f docker-compose.yml -f docker-compose.dev.yml -f docker-compose.safe.yml
SAFE_COMPOSE := docker compose $(SAFE_COMPOSE_FILES)

.PHONY: help up down restart logs ps build pull migrate seed shell-back shell-front shell-db backup smoke certs-dev clean

help:
	@echo "CarWash — comandos disponíveis (ENV=$(ENV))"
	@echo ""
	@echo "  make up ENV=dev|hom|prod        sobe o ambiente em segundo plano"
	@echo "  make down ENV=...               derruba mantendo volumes"
	@echo "  make restart ENV=...            reinicia"
	@echo "  make logs ENV=... [SVC=backend] segue logs"
	@echo "  make ps ENV=...                 status dos containers"
	@echo "  make build ENV=...              rebuild das imagens"
	@echo "  make migrate ENV=...            aplica migrations EF Core (dev sob demanda)"
	@echo "  make smoke ENV=...              smoke test pós-deploy (DAT §8.2)"
	@echo "  make backup ENV=hom|prod        dump do PostgreSQL para docker/postgres/backups"
	@echo "  make certs-dev                  gera cert auto-assinado para hom local"
	@echo "  make shell-back|shell-front|shell-db   abre shell no container"
	@echo "  make clean                      remove containers, volumes e redes (CUIDADO)"

up:
	$(COMPOSE) up -d --build

down:
	$(COMPOSE) down

restart:
	$(COMPOSE) restart $(SVC)

logs:
	$(COMPOSE) logs -f $(SVC)

ps:
	$(COMPOSE) ps

build:
	$(COMPOSE) build --pull

pull:
	$(COMPOSE) pull

migrate:
	$(if $(filter dev,$(ENV)),$(SAFE_COMPOSE),$(COMPOSE)) run --rm migrator

shell-back:
	$(COMPOSE) exec backend /bin/bash || $(COMPOSE) exec backend /bin/sh

shell-front:
	$(COMPOSE) exec frontend /bin/sh

shell-db:
	$(COMPOSE) exec postgres psql -U $${POSTGRES_USER:-carwash_owner} -d $${POSTGRES_DB:-carwash}

backup:
	@mkdir -p docker/postgres/backups
	$(COMPOSE) exec -T postgres pg_dump -U $${POSTGRES_USER:-carwash_owner} -d $${POSTGRES_DB:-carwash} \
	  | gzip > docker/postgres/backups/carwash-$$(date +%Y%m%d-%H%M%S).sql.gz
	@echo "Backup salvo em docker/postgres/backups/"

# Smoke test pós-deploy — verifica /health do backend e index do front (DAT §8.2)
smoke:
	@echo "→ /health do backend..."
	@docker compose $(COMPOSE_FILES) exec -T backend wget -qO- http://localhost:8080/health \
	  || (echo "FALHOU: backend /health" && exit 1)
	@echo ""
	@echo "→ index do frontend..."
	@docker compose $(COMPOSE_FILES) exec -T frontend wget -qO- http://localhost:8080/ > /dev/null \
	  || (echo "FALHOU: frontend index" && exit 1)
	@echo "Smoke OK."

# Cert auto-assinado para hom local (NÃO usar em produção)
certs-dev:
	@mkdir -p docker/proxy/certs
	@openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
	  -keyout docker/proxy/certs/server.key \
	  -out docker/proxy/certs/server.crt \
	  -subj "/C=BR/ST=SP/L=Local/O=CarWash/CN=carwash.local"
	@echo "Cert auto-assinado gerado em docker/proxy/certs/"

clean:
	$(COMPOSE) down -v --remove-orphans
