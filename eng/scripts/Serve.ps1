#Requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$RepoRoot         = (Resolve-Path "$PSScriptRoot\..\..")
$ContentSourceDir = Join-Path $RepoRoot 'src' 'blog'
$HugoVersion      = '0.161.1'
$HugoBinDir       = Join-Path $RepoRoot '.bin' 'hugo'

# ── Hugo binary restore ────────────────────────────────────────────────────────

if ($IsWindows) {
    $HugoFileName = "hugo_extended_${HugoVersion}_windows-amd64.zip"
    $HugoExe      = Join-Path $HugoBinDir 'hugo.exe'
} else {
    $HugoFileName = "hugo_extended_${HugoVersion}_linux-amd64.tar.gz"
    $HugoExe      = Join-Path $HugoBinDir 'hugo'
}

$HugoReleaseUrl = "https://github.com/gohugoio/hugo/releases/download/v${HugoVersion}/${HugoFileName}"
$DestFile       = Join-Path $HugoBinDir $HugoFileName

if (-not (Test-Path $DestFile)) {
    Write-Host "Restoring Hugo binary v$HugoVersion..."
    New-Item -ItemType Directory -Path $HugoBinDir -Force | Out-Null
    Invoke-WebRequest `
        -Uri $HugoReleaseUrl `
        -OutFile $DestFile `
        -UserAgent 'jvandertil/blog build script' `
        -TimeoutSec 30

    if ($IsWindows) {
        Expand-Archive -Path $DestFile -DestinationPath $HugoBinDir -Force
    } else {
        & tar -xzf $DestFile -C $HugoBinDir
        & chmod +x $HugoExe
    }
} else {
    Write-Host "Skipping Hugo restore, already restored."
}

# ── Serve ──────────────────────────────────────────────────────────────────────

Write-Host "Starting Hugo dev server (dev environment, includes drafts/future/expired)..."
& $HugoExe serve --environment dev --source $ContentSourceDir --minify -DEF
