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

# Stack de testes (CA011) — autocontida, nome de projeto isolado para nunca
# colidir com a stack de dev. Ver docker-compose.test.yml.
TEST_PROJECT := carwash-test
TEST_COMPOSE := docker compose -p $(TEST_PROJECT) -f docker-compose.test.yml

.PHONY: help up down restart logs ps build pull migrate seed shell-back shell-front shell-db backup smoke certs-dev clean \
        test test-unit test-back test-front test-e2e clean-test

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
	@echo ""
	@echo "  --- Testes dockerizados (CA011) ---"
	@echo "  make test                       roda TODA a suíte (unit back+front + e2e)"
	@echo "  make test-unit                  testes unitários back e front (em containers)"
	@echo "  make test-back                  backend: unit + integration (Testcontainers)"
	@echo "  make test-front                 frontend: Vitest + cobertura"
	@echo "  make test-e2e                   sobe a stack e roda Playwright pelo proxy"
	@echo "  make clean-test                 derruba a stack de teste e remove volumes"

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

# =============================================================================
# TESTES DOCKERIZADOS (CA011) — mesmo comando local e em CI
# =============================================================================

# Backend: unit + integration num único container. IntegrationTests usa
# Testcontainers, que precisa do socket do Docker do host (montado no compose).
test-back:
	$(TEST_COMPOSE) build backend-tests
	$(TEST_COMPOSE) run --rm backend-tests

# Frontend: Vitest + cobertura (jsdom + MSW, sem stack no ar).
test-front:
	$(TEST_COMPOSE) build frontend-tests
	$(TEST_COMPOSE) run --rm frontend-tests

# Atalho para os dois conjuntos unitários (back inclui integration).
test-unit: test-back test-front

# E2E: sobe a stack (postgres+migrator+backend+frontend+proxy), o runner espera
# o /health pelo proxy, roda o Playwright e depois derruba tudo (mesmo em falha).
test-e2e:
	@set -e; \
	$(TEST_COMPOSE) --profile e2e build; \
	status=0; \
	$(TEST_COMPOSE) --profile e2e run --rm e2e || status=$$?; \
	$(TEST_COMPOSE) --profile e2e down -v --remove-orphans; \
	exit $$status

# Suíte completa: unitários (back+front) e depois E2E.
test: test-unit test-e2e

# Derruba a stack de teste e remove volumes/redes/órfãos (idempotente).
clean-test:
	-$(TEST_COMPOSE) --profile e2e --profile unit down -v --remove-orphans
