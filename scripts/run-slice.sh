#!/bin/bash
# Runs all implemented services + BFF using dotnet run

set -e

# Define services to run
SERVICES=(
    "services/access-service/src/AccessService.Api"
    "services/party-service/src/PartyService.Api"
    "services/platform-config-service/src/PlatformConfigService.Api"
    "services/property-service/src/PropertyService.Api"
    "services/supply-service/src/SupplyService.Api"
    "services/demand-service/src/DemandService.Api"
    "services/matching-service/src/MatchingService.Api"
    "bffs/operations-bff/src/OperationsBff.Api"
)

# Start each service in the background
PIDS=()
for service in "${SERVICES[@]}"; do
    echo "Starting $service..."
    (cd "$service" && dotnet run) &
    PIDS+=($!)
done

# Function to stop all background processes
cleanup() {
    echo "Stopping all services..."
    for pid in "${PIDS[@]}"; do
        kill $pid 2>/dev/null || true
    done
    exit 0
}

# Register the cleanup function for SIGINT and SIGTERM
trap cleanup SIGINT SIGTERM

echo "All services started. Press Ctrl+C to stop."
wait
