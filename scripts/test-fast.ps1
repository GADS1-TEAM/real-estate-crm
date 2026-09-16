# Job rápido de CI (V2-FND-003): build + tests que NO requieren Mongo/RabbitMQ/Keycloak
# reales + build de ambas webs. No levanta docker compose.
$ErrorActionPreference = "Stop"

$RootDir = Resolve-Path (Join-Path $PSScriptRoot "..")
Set-Location $RootDir

Write-Host "==> dotnet build"
dotnet build RealEstateCrm.slnx

Write-Host "==> dotnet test (sin infraestructura externa)"
dotnet test RealEstateCrm.slnx --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"

Write-Host "==> build de crm-web y platform-admin-web"
& (Join-Path $RootDir "apps\scripts\build-webs.ps1")
