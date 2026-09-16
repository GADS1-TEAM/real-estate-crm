# Job de integración de CI (V2-FND-003): levanta Mongo/RabbitMQ/Keycloak con el mismo
# docker-compose.yml que usa un desarrollador local, espera los healthchecks y corre los
# tests marcados RequiresMongo/RequiresRabbitMq/RequiresKeycloak. Este script es el que
# hace "CI local = CI remoto": lo corre tanto el workflow de GitHub Actions como cualquiera
# a mano en su máquina.
$ErrorActionPreference = "Stop"

$RootDir = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RootDir

$Containers = @("crm-mongo", "crm-rabbitmq", "crm-keycloak")

function Invoke-Cleanup {
    Write-Host "==> docker compose down"
    docker compose down -v
}

try {
    Write-Host "==> docker compose up -d"
    docker compose up -d

    Write-Host "==> esperando healthchecks (mongo, rabbitmq, keycloak)"
    foreach ($name in $Containers) {
        Write-Host "  -> $name"
        $healthy = $false
        for ($attempt = 1; $attempt -le 60; $attempt++) {
            $status = docker inspect --format='{{.State.Health.Status}}' $name 2>$null
            if ($status -eq "healthy") {
                Write-Host "     healthy (intento $attempt)"
                $healthy = $true
                break
            }
            Start-Sleep -Seconds 3
        }
        if (-not $healthy) {
            Write-Host "     $name no quedo healthy a tiempo (status=$status)"
            docker logs $name --tail 50
            throw "$name no quedo healthy"
        }
    }

    $env:MONGO_CONNECTION_STRING = "mongodb://localhost:27017/?directConnection=true"
    $env:MONGO_REPLICA_SET_CONNECTION_STRING = "mongodb://localhost:27017/?replicaSet=rs0&directConnection=true"
    $env:RABBITMQ_HOST = "localhost"
    $env:KEYCLOAK_BASE_URL = "http://localhost:8080"
    $env:KEYCLOAK_REALM = "crm-dev"
    $env:KEYCLOAK_CLIENT_ID = "operations-bff"
    $env:KEYCLOAK_DEV_USERNAME = "dev.vendedor"
    $env:KEYCLOAK_DEV_PASSWORD = "dev.vendedor"

    Write-Host "==> dotnet test (RequiresMongo | RequiresRabbitMq | RequiresKeycloak)"
    dotnet test RealEstateCrm.slnx --filter "Category=RequiresMongo|Category=RequiresRabbitMq|Category=RequiresKeycloak"
}
finally {
    Invoke-Cleanup
}
