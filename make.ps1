<#
.SYNOPSIS
  Equivalente em PowerShell do Makefile do CarWash para devs no Windows.

.DESCRIPTION
  Espelha os alvos do Makefile (up, down, logs, migrate, smoke, backup, etc.)
  usando docker compose. Sempre rode da raiz do repositório.

.PARAMETER Target
  Alvo a executar (help, up, down, restart, logs, ps, build, pull, migrate,
  shell-back, shell-front, shell-db, backup, smoke, certs-dev, clean,
  test, test-unit, test-back, test-front, test-e2e, clean-test).

.PARAMETER Env
  Ambiente: dev | hom | prod (default: dev).

.PARAMETER Svc
  Serviço para logs/restart (opcional, ex.: backend, frontend, postgres).

.EXAMPLE
  .\make.ps1 up -Env dev
  .\make.ps1 logs -Env hom -Svc backend
  .\make.ps1 shell-db -Env dev
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [string]$Target = 'help',

    [ValidateSet('dev', 'hom', 'prod')]
    [string]$Env = 'dev',

    [string]$Svc = ''
)

$ErrorActionPreference = 'Stop'

$ComposeFiles = @('-f', 'docker-compose.yml', '-f', "docker-compose.$Env.yml")
$EnvFileCandidate = ".env.$Env"
$EnvFile = if (Test-Path $EnvFileCandidate) { $EnvFileCandidate } else { '.env' }
$ComposeEnvFile = if (Test-Path $EnvFile) { @('--env-file', $EnvFile) } else { @() }

# Stack de testes (CA011) — projeto isolado, nunca colide com a stack de dev.
$TestProject = 'carwash-test'
$TestCompose = @('-p', $TestProject, '-f', 'docker-compose.test.yml')

function Invoke-Compose {
    param([string[]]$ComposeArgs)
    & docker compose @ComposeFiles @ComposeEnvFile @ComposeArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

function Invoke-TestCompose {
    param([string[]]$ComposeArgs)
    & docker compose @TestCompose @ComposeArgs
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

function Show-Help {
    Write-Host "CarWash - comandos disponiveis (Env=$Env)"
    Write-Host ""
    Write-Host "  .\make.ps1 up        -Env dev|hom|prod        sobe o ambiente em segundo plano"
    Write-Host "  .\make.ps1 down      -Env ...                 derruba mantendo volumes"
    Write-Host "  .\make.ps1 restart   -Env ... [-Svc backend]  reinicia"
    Write-Host "  .\make.ps1 logs      -Env ... [-Svc backend]  segue logs"
    Write-Host "  .\make.ps1 ps        -Env ...                 status dos containers"
    Write-Host "  .\make.ps1 build     -Env ...                 rebuild das imagens"
    Write-Host "  .\make.ps1 pull      -Env ...                 baixa imagens"
    Write-Host "  .\make.ps1 migrate   -Env ...                 aplica migrations EF Core"
    Write-Host "  .\make.ps1 smoke     -Env ...                 smoke test pos-deploy (DAT 8.2)"
    Write-Host "  .\make.ps1 backup    -Env hom|prod            dump do PostgreSQL para docker/postgres/backups"
    Write-Host "  .\make.ps1 certs-dev                          gera cert auto-assinado para hom local"
    Write-Host "  .\make.ps1 shell-back|shell-front|shell-db    abre shell no container"
    Write-Host "  .\make.ps1 clean     -Env ...                 remove containers, volumes e redes (CUIDADO)"
    Write-Host ""
    Write-Host "  --- Testes dockerizados (CA011) ---"
    Write-Host "  .\make.ps1 test                               suite completa (unit back+front + e2e)"
    Write-Host "  .\make.ps1 test-unit                          unitarios back e front (containers)"
    Write-Host "  .\make.ps1 test-back                          backend: unit + integration (Testcontainers)"
    Write-Host "  .\make.ps1 test-front                         frontend: Vitest + cobertura"
    Write-Host "  .\make.ps1 test-e2e                           sobe a stack e roda Playwright pelo proxy"
    Write-Host "  .\make.ps1 clean-test                         derruba a stack de teste e remove volumes"
}

switch ($Target) {
    'help'    { Show-Help }

    'up'      { Invoke-Compose 'up', '-d', '--build' }
    'down'    { Invoke-Compose 'down' }
    'restart' {
        if ($Svc) { Invoke-Compose 'restart', $Svc }
        else      { Invoke-Compose 'restart' }
    }
    'logs'    {
        if ($Svc) { Invoke-Compose 'logs', '-f', $Svc }
        else      { Invoke-Compose 'logs', '-f' }
    }
    'ps'      { Invoke-Compose 'ps' }
    'build'   { Invoke-Compose 'build', '--pull' }
    'pull'    { Invoke-Compose 'pull' }

    # Em dev, migrator esta em profile "manual"; em hom/prod sobe automatico no up
    'migrate' { Invoke-Compose 'run', '--rm', 'migrator' }

    'shell-back' {
        & docker compose @ComposeFiles @ComposeEnvFile exec backend /bin/bash
        if ($LASTEXITCODE -ne 0) {
            & docker compose @ComposeFiles @ComposeEnvFile exec backend /bin/sh
        }
    }
    'shell-front' { Invoke-Compose 'exec', 'frontend', '/bin/sh' }
    'shell-db' {
        $user = if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { 'carwash_owner' }
        $db   = if ($env:POSTGRES_DB)   { $env:POSTGRES_DB }   else { 'carwash' }
        Invoke-Compose 'exec', 'postgres', 'psql', '-U', $user, '-d', $db
    }

    'backup' {
        $user = if ($env:POSTGRES_USER) { $env:POSTGRES_USER } else { 'carwash_owner' }
        $db   = if ($env:POSTGRES_DB)   { $env:POSTGRES_DB }   else { 'carwash' }
        New-Item -ItemType Directory -Force -Path 'docker/postgres/backups' | Out-Null
        $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
        $out   = "docker/postgres/backups/carwash-$stamp.sql.gz"
        # PowerShell nao tem pipe binario seguro -> usamos cmd para pipear pg_dump | gzip
        $cmd = "docker compose $($ComposeFiles -join ' ') $($ComposeEnvFile -join ' ') exec -T postgres pg_dump -U $user -d $db | gzip > `"$out`""
        cmd.exe /c $cmd
        if ($LASTEXITCODE -ne 0) { Write-Error "Backup falhou"; exit $LASTEXITCODE }
        Write-Host "Backup salvo em $out"
    }

    'smoke' {
        Write-Host "-> /health do backend..."
        & docker compose @ComposeFiles @ComposeEnvFile exec -T backend wget -qO- http://localhost:8080/health | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Error "FALHOU: backend /health"; exit 1 }
        Write-Host ""
        Write-Host "-> index do frontend..."
        & docker compose @ComposeFiles @ComposeEnvFile exec -T frontend wget -qO- http://localhost:8080/ | Out-Null
        if ($LASTEXITCODE -ne 0) { Write-Error "FALHOU: frontend index"; exit 1 }
        Write-Host "Smoke OK."
    }

    'certs-dev' {
        New-Item -ItemType Directory -Force -Path 'docker/proxy/certs' | Out-Null
        & openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
            -keyout docker/proxy/certs/server.key `
            -out    docker/proxy/certs/server.crt `
            -subj "/C=BR/ST=SP/L=Local/O=CarWash/CN=carwash.local"
        if ($LASTEXITCODE -ne 0) {
            Write-Error "openssl falhou. Instale via 'choco install openssl' ou use o Git Bash."
            exit $LASTEXITCODE
        }
        Write-Host "Cert auto-assinado gerado em docker/proxy/certs/"
    }

    'clean' { Invoke-Compose 'down', '-v', '--remove-orphans' }

    # --- Testes dockerizados (CA011) ---
    'test-back' {
        Invoke-TestCompose 'build', 'backend-tests'
        Invoke-TestCompose 'run', '--rm', 'backend-tests'
    }
    'test-front' {
        Invoke-TestCompose 'build', 'frontend-tests'
        Invoke-TestCompose 'run', '--rm', 'frontend-tests'
    }
    'test-unit' {
        & $PSCommandPath 'test-back'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        & $PSCommandPath 'test-front'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    'test-e2e' {
        & docker compose @TestCompose --profile e2e build
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        & docker compose @TestCompose --profile e2e run --rm e2e
        $e2eStatus = $LASTEXITCODE
        & docker compose @TestCompose --profile e2e down -v --remove-orphans
        if ($e2eStatus -ne 0) { exit $e2eStatus }
    }
    'test' {
        & $PSCommandPath 'test-unit'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
        & $PSCommandPath 'test-e2e'; if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    'clean-test' {
        & docker compose @TestCompose --profile e2e --profile unit down -v --remove-orphans
    }

    default {
        Write-Error "Alvo desconhecido: '$Target'. Rode '.\make.ps1 help' para a lista."
        exit 2
    }
}
