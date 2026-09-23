#!/usr/bin/env bash
# Version bash de scripts/start-remote-demo.ps1. Ver DEVELOPMENT.md, "Demo remota por tunel".
set -euo pipefail

if [[ -z "${DEMO_PUBLIC_DOMAIN:-}" ]]; then
  echo "Falta DEMO_PUBLIC_DOMAIN. Ejemplo:" >&2
  echo '  export DEMO_PUBLIC_DOMAIN="tu-dominio.ngrok-free.dev"' >&2
  exit 1
fi

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
export Keycloak__Authority="https://${DEMO_PUBLIC_DOMAIN}/auth/realms/crm-dev"

echo "==> dotnet build"
dotnet build RealEstateCrm.slnx

echo "==> servicios + BFF"
bash scripts/run-slice.sh

cat <<MSG

Backend arriba. En tres terminales mas:
  1) cd apps/crm-web && npm install && npm run build && npm run start
  2) caddy run --config infra/demo/Caddyfile
  3) ngrok http --domain=${DEMO_PUBLIC_DOMAIN} 8000

Abrir https://${DEMO_PUBLIC_DOMAIN} (dev.vendedor / dev.vendedor)
Datos de prueba: node scripts/seed-data.js
MSG
