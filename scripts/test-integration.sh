#!/usr/bin/env bash
# Job de integración de CI (V2-FND-003): levanta Mongo/RabbitMQ/Keycloak con el mismo
# docker-compose.yml que usa un desarrollador local, espera los healthchecks y corre los
# tests marcados RequiresMongo/RequiresRabbitMq/RequiresKeycloak. Este script es el que
# hace "CI local = CI remoto": lo corre tanto el workflow de GitHub Actions como cualquiera
# a mano en su máquina.
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

CONTAINERS=(crm-mongo crm-rabbitmq crm-keycloak)

cleanup() {
  echo "==> docker compose down"
  docker compose down -v
}
trap cleanup EXIT

echo "==> docker compose up -d"
docker compose up -d

echo "==> esperando healthchecks (mongo, rabbitmq, keycloak)"
for name in "${CONTAINERS[@]}"; do
  echo "  -> $name"
  for attempt in $(seq 1 60); do
    status="$(docker inspect --format='{{.State.Health.Status}}' "$name" 2>/dev/null || echo "unknown")"
    if [ "$status" = "healthy" ]; then
      echo "     healthy (intento $attempt)"
      break
    fi
    if [ "$attempt" -eq 60 ]; then
      echo "     $name no quedó healthy a tiempo (status=$status)"
      docker logs "$name" --tail 50 || true
      exit 1
    fi
    sleep 3
  done
done

export MONGO_CONNECTION_STRING="mongodb://localhost:27017/?directConnection=true"
export MONGO_REPLICA_SET_CONNECTION_STRING="mongodb://localhost:27017/?replicaSet=rs0&directConnection=true"
export RABBITMQ_HOST="localhost"
export KEYCLOAK_BASE_URL="http://localhost:8080"
export KEYCLOAK_REALM="crm-dev"
export KEYCLOAK_CLIENT_ID="operations-bff"
export KEYCLOAK_DEV_USERNAME="dev.vendedor"
export KEYCLOAK_DEV_PASSWORD="dev.vendedor"

echo "==> dotnet test (RequiresMongo | RequiresRabbitMq | RequiresKeycloak)"
dotnet test RealEstateCrm.slnx --filter "Category=RequiresMongo|Category=RequiresRabbitMq|Category=RequiresKeycloak"
