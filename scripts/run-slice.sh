#!/bin/bash
export ASPNETCORE_ENVIRONMENT=Development

# Base services
dotnet run --no-build --project services/access-service/src/AccessService.Api/AccessService.Api.csproj --urls "http://localhost:5190" &
dotnet run --no-build --project services/party-service/src/PartyService.Api/PartyService.Api.csproj --urls "http://localhost:5155" &
dotnet run --no-build --project services/platform-config-service/src/PlatformConfigService.Api/PlatformConfigService.Api.csproj --urls "http://localhost:5246" &

# Real estate core
dotnet run --no-build --project services/property-service/src/PropertyService.Api/PropertyService.Api.csproj --urls "http://localhost:5117" &
dotnet run --no-build --project services/supply-service/src/SupplyService.Api/SupplyService.Api.csproj --urls "http://localhost:5069" &
dotnet run --no-build --project services/demand-service/src/DemandService.Api/DemandService.Api.csproj --urls "http://localhost:5220" &
dotnet run --no-build --project services/matching-service/src/MatchingService.Api/MatchingService.Api.csproj --urls "http://localhost:5290" &

# Operations
dotnet run --no-build --project services/commercial-service/src/CommercialService.Api/CommercialService.Api.csproj --urls "http://localhost:5145" &
dotnet run --no-build --project services/activity-service/src/ActivityService.Api/ActivityService.Api.csproj --urls "http://localhost:5247" &

# Cross-cutting
dotnet run --no-build --project services/analytics-service/src/AnalyticsService.Api/AnalyticsService.Api.csproj --urls "http://localhost:5046" &
dotnet run --no-build --project services/automation-ai-service/src/AutomationAiService.Api/AutomationAiService.Api.csproj --urls "http://localhost:5015" &

# BFFs
dotnet run --no-build --project bffs/operations-bff/src/OperationsBff.Api/OperationsBff.Api.csproj --urls "http://localhost:5137" &
dotnet run --no-build --project bffs/platform-admin-bff/src/PlatformAdminBff.Api/PlatformAdminBff.Api.csproj --urls "http://localhost:5140" &

wait
