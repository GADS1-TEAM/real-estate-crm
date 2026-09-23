# Levanta los servicios .NET + el BFF apuntando al dominio publico de la demo remota.
# Requiere: infra arriba con el overlay de demo.
#   $env:DEMO_PUBLIC_DOMAIN="tu-dominio.ngrok-free.dev"
#   docker compose -f docker-compose.yml -f docker-compose.demo.yml up -d
# Pasos completos en DEVELOPMENT.md, seccion "Demo remota por tunel".
$ErrorActionPreference = "Stop"

$domain = $env:DEMO_PUBLIC_DOMAIN
if ([string]::IsNullOrWhiteSpace($domain)) {
    Write-Host 'Falta DEMO_PUBLIC_DOMAIN. Ejemplo:' -ForegroundColor Red
    Write-Host '  $env:DEMO_PUBLIC_DOMAIN="tu-dominio.ngrok-free.dev"' -ForegroundColor Yellow
    exit 1
}

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_FORWARDEDHEADERS_ENABLED = "true"
$env:Keycloak__Authority = "https://$domain/auth/realms/crm-dev"

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Compilando..." -ForegroundColor Cyan
dotnet build RealEstateCrm.slnx

Write-Host "Levantando servicios + BFF (una ventana por servicio)..." -ForegroundColor Cyan
& "$root\scripts\run-slice.ps1"

Write-Host ""
Write-Host "Backend arriba. Ahora, en tres terminales mas:" -ForegroundColor Green
Write-Host "  1) cd apps\crm-web ; npm install ; npm run build ; npm run start"
Write-Host "  2) caddy run --config infra\demo\Caddyfile"
Write-Host "  3) ngrok http --domain=$domain 8000"
Write-Host ""
Write-Host "Despues abrir: https://$domain   (usuario dev.vendedor / dev.vendedor)" -ForegroundColor Green
Write-Host "Datos de prueba: node scripts\seed-data.js" -ForegroundColor Green
