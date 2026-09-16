# Instala y construye crm-web y platform-admin-web sin modificar su código.
$ErrorActionPreference = "Stop"

$RootDir = Resolve-Path (Join-Path $PSScriptRoot "..\..")

foreach ($app in @("crm-web", "platform-admin-web")) {
    $appDir = Join-Path $RootDir "apps\$app"

    Write-Host "==> $app: npm install"
    npm install --prefix $appDir

    Write-Host "==> $app: npm run build"
    npm run build --prefix $appDir
}
