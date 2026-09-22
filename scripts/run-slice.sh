#!/bin/bash
export ASPNETCORE_ENVIRONMENT=Development

pids=()

cleanup() {
    echo ""
    echo "🛑 Deteniendo todos los microservicios y BFFs..."
    trap - SIGINT SIGTERM EXIT

    # Enviar SIGTERM a todos los subprocesos iniciados por el script
    for pid in "${pids[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            pkill -P "$pid" 2>/dev/null
            kill -TERM "$pid" 2>/dev/null
        fi
    done

    # Esperar 1 segundo para apagado ordenado
    sleep 1

    # Asegurar que se liberen todos los puertos de los servicios si alguno quedó colgado
    for port in 5190 5155 5246 5117 5069 5220 5290 5145 5247 5046 5015 5137 5140; do
        fuser -k -9 "${port}/tcp" 2>/dev/null
    done

    echo "✅ Todos los servicios se han detenido correctamente."
}

trap cleanup SIGINT SIGTERM EXIT

run_service() {
    dotnet run --no-build --project "$1" --urls "$2" &
    pids+=($!)
}

# Base services
run_service "services/access-service/src/AccessService.Api/AccessService.Api.csproj" "http://localhost:5190"
run_service "services/party-service/src/PartyService.Api/PartyService.Api.csproj" "http://localhost:5155"
run_service "services/platform-config-service/src/PlatformConfigService.Api/PlatformConfigService.Api.csproj" "http://localhost:5246"

# Real estate core
run_service "services/property-service/src/PropertyService.Api/PropertyService.Api.csproj" "http://localhost:5117"
run_service "services/supply-service/src/SupplyService.Api/SupplyService.Api.csproj" "http://localhost:5069"
run_service "services/demand-service/src/DemandService.Api/DemandService.Api.csproj" "http://localhost:5220"
run_service "services/matching-service/src/MatchingService.Api/MatchingService.Api.csproj" "http://localhost:5290"

# Operations
run_service "services/commercial-service/src/CommercialService.Api/CommercialService.Api.csproj" "http://localhost:5145"
run_service "services/activity-service/src/ActivityService.Api/ActivityService.Api.csproj" "http://localhost:5247"

# Cross-cutting
run_service "services/analytics-service/src/AnalyticsService.Api/AnalyticsService.Api.csproj" "http://localhost:5046"
run_service "services/automation-ai-service/src/AutomationAiService.Api/AutomationAiService.Api.csproj" "http://localhost:5015"

# BFFs
run_service "bffs/operations-bff/src/OperationsBff.Api/OperationsBff.Api.csproj" "http://localhost:5137"
run_service "bffs/platform-admin-bff/src/PlatformAdminBff.Api/PlatformAdminBff.Api.csproj" "http://localhost:5140"

echo "🚀 Todos los microservicios y BFFs iniciados."
echo "Presioná Ctrl+C para detener todos los procesos."

wait

