<#
.SYNOPSIS
  Sincroniza .github/skills/ (canónico, consumido por GitHub Copilot) hacia
  .claude/skills/ (consumido por Claude Code). Excluye _archived/.

.EXAMPLE
  pwsh scripts/sync-skills.ps1
  pwsh scripts/sync-skills.ps1 -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param()

$ErrorActionPreference = 'Stop'
$root   = Split-Path $PSScriptRoot -Parent
$source = Join-Path $root '.github\skills'
$dest   = Join-Path $root '.claude\skills'

if (-not (Test-Path $source)) { throw "No existe $source" }
New-Item -ItemType Directory -Force $dest | Out-Null

$skills = Get-ChildItem $source -Directory | Where-Object { $_.Name -ne '_archived' }

# Borrar en destino lo que ya no existe en origen
$names = $skills.Name
Get-ChildItem $dest -Directory | Where-Object { $_.Name -notin $names } | ForEach-Object {
    if ($PSCmdlet.ShouldProcess($_.FullName, 'Remove (huérfano)')) {
        Remove-Item $_.FullName -Recurse -Force
        Write-Host "  - $($_.Name) (eliminado)" -ForegroundColor DarkYellow
    }
}

foreach ($s in $skills) {
    if ($PSCmdlet.ShouldProcess($s.FullName, "Copy -> $dest")) {
        Remove-Item (Join-Path $dest $s.Name) -Recurse -Force -ErrorAction SilentlyContinue
        Copy-Item $s.FullName -Destination $dest -Recurse -Force
    }
}

Write-Host "Sincronizadas $($skills.Count) skills -> .claude\skills\" -ForegroundColor Green
