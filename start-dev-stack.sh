#!/usr/bin/env bash
set -e

echo "================================================="
echo " Starting Real Estate CRM Full Integration Stack "
echo "================================================="

# 1. Start Infrastructure Containers
echo "--> Starting Docker Compose infrastructure (MongoDB, RabbitMQ, Keycloak)..."
docker compose up -d

echo "--> Waiting for infrastructure services to be healthy..."
sleep 5

# 2. Check or environment file setup
if [ ! -f "apps/crm-web/.env.local" ]; then
    echo "--> Creating apps/crm-web/.env.local..."
    echo "NEXT_PUBLIC_CRM_WEB_MODE=bff" > apps/crm-web/.env.local
    echo "NEXT_PUBLIC_CRM_BFF_URL=http://localhost:5137" >> apps/crm-web/.env.local
fi

# 3. Inform user about running services
echo "Infrastructure is UP:"
echo " - MongoDB: mongodb://localhost:27017/?directConnection=true"
echo " - RabbitMQ: amqp://guest:guest@localhost:5672 (UI: http://localhost:15672)"
echo " - Keycloak: http://localhost:8080 (admin / admin-dev-only)"
echo " - Operations BFF: http://localhost:5137"
echo " - Platform Admin BFF: http://localhost:5140"
echo " - CRM Web Frontend: http://localhost:3000"
echo ""
echo "To start Operations BFF:"
echo "  dotnet run --project bffs/operations-bff/src/OperationsBff.Api/OperationsBff.Api.csproj --urls http://localhost:5137"
echo ""
echo "To start Platform Admin BFF:"
echo "  dotnet run --project bffs/platform-admin-bff/src/PlatformAdminBff.Api/PlatformAdminBff.Api.csproj --urls http://localhost:5140"
echo ""
echo "To start CRM Web Frontend:"
echo "  cd apps/crm-web && npm run dev"
echo "================================================="
