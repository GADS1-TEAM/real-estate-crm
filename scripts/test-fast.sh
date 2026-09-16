#!/usr/bin/env bash
# Job rápido de CI (V2-FND-003): build + tests que NO requieren Mongo/RabbitMQ/Keycloak
# reales + build de ambas webs. No levanta docker compose.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

echo "==> dotnet build"
dotnet build RealEstateCrm.slnx

echo "==> dotnet test (sin infraestructura externa)"
dotnet test RealEstateCrm.slnx --filter "Category!=RequiresMongo&Category!=RequiresRabbitMq&Category!=RequiresKeycloak"

echo "==> build de crm-web y platform-admin-web"
bash apps/scripts/build-webs.sh
