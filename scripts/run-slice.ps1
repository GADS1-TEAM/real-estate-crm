$env:ASPNETCORE_ENVIRONMENT="Development"

Start-Process dotnet -ArgumentList "run --project services/access-service/src/AccessService.Api/AccessService.Api.csproj --urls http://localhost:5190"
Start-Process dotnet -ArgumentList "run --project services/party-service/src/PartyService.Api/PartyService.Api.csproj --urls http://localhost:5155"
Start-Process dotnet -ArgumentList "run --project services/platform-config-service/src/PlatformConfigService.Api/PlatformConfigService.Api.csproj --urls http://localhost:5246"

Start-Process dotnet -ArgumentList "run --project services/property-service/src/PropertyService.Api/PropertyService.Api.csproj --urls http://localhost:5117"
Start-Process dotnet -ArgumentList "run --project services/supply-service/src/SupplyService.Api/SupplyService.Api.csproj --urls http://localhost:5069"
Start-Process dotnet -ArgumentList "run --project services/demand-service/src/DemandService.Api/DemandService.Api.csproj --urls http://localhost:5220"
Start-Process dotnet -ArgumentList "run --project services/matching-service/src/MatchingService.Api/MatchingService.Api.csproj --urls http://localhost:5290"

Start-Process dotnet -ArgumentList "run --project services/commercial-service/src/CommercialService.Api/CommercialService.Api.csproj --urls http://localhost:5260"
Start-Process dotnet -ArgumentList "run --project services/activity-service/src/ActivityService.Api/ActivityService.Api.csproj --urls http://localhost:5270"

Start-Process dotnet -ArgumentList "run --project services/analytics-service/src/AnalyticsService.Api/AnalyticsService.Api.csproj --urls http://localhost:5280"
Start-Process dotnet -ArgumentList "run --project services/automation-ai-service/src/AutomationAiService.Api/AutomationAiService.Api.csproj --urls http://localhost:5295"

Start-Process dotnet -ArgumentList "run --project bffs/operations-bff/src/OperationsBff.Api/OperationsBff.Api.csproj --urls http://localhost:5137"
