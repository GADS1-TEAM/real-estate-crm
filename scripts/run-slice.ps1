# Runs all implemented services + BFF using dotnet run

$ErrorActionPreference = "Stop"

$Services = @(
    "services\access-service\src\AccessService.Api",
    "services\party-service\src\PartyService.Api",
    "services\platform-config-service\src\PlatformConfigService.Api",
    "services\property-service\src\PropertyService.Api",
    "services\supply-service\src\SupplyService.Api",
    "services\demand-service\src\DemandService.Api",
    "services\matching-service\src\MatchingService.Api",
    "bffs\operations-bff\src\OperationsBff.Api"
)

$Jobs = @()

try {
    foreach ($service in $Services) {
        Write-Host "Starting $service..."
        $job = Start-Job -ScriptBlock {
            param($path)
            Set-Location $path
            dotnet run
        } -ArgumentList (Join-Path $PWD $service)
        $Jobs += $job
    }

    Write-Host "All services started. Press Ctrl+C to stop."
    while ($true) {
        Start-Sleep -Seconds 1
    }
}
finally {
    Write-Host "Stopping all services..."
    foreach ($job in $Jobs) {
        Stop-Job $job
    }
}
