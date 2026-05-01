#Requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$RepoRoot    = (Resolve-Path "$PSScriptRoot\..\..")
$ArtifactsDir = Join-Path $RepoRoot 'artifacts'
$SourceDir    = Join-Path $RepoRoot 'src'

Write-Host "Cleaning bin/obj directories..."
Get-ChildItem -Path $SourceDir -Recurse -Directory -Include 'bin', 'obj' |
    ForEach-Object { Remove-Item $_.FullName -Recurse -Force }

Write-Host "Cleaning artifacts directory..."
if (Test-Path $ArtifactsDir) {
    Remove-Item $ArtifactsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $ArtifactsDir | Out-Null

Write-Host "Clean complete."
