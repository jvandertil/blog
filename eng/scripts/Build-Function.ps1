#Requires -Version 7.0
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$RepoRoot         = (Resolve-Path "$PSScriptRoot\..\..")
$ArtifactsDir     = Join-Path $RepoRoot 'artifacts'
$SolutionRoot     = Join-Path $RepoRoot 'src' 'blog-comment-function'
$Solution         = Join-Path $SolutionRoot 'BlogComments.sln'
$FunctionProject  = Join-Path $SolutionRoot 'src' 'BlogComments' 'BlogComments.csproj'
$WorkDir          = Join-Path $ArtifactsDir 'work'
$TestResultsDir   = Join-Path $ArtifactsDir 'TestResults'

$Configuration = if ($env:TF_BUILD -or $env:CI) { 'Release' } else { 'Debug' }

Write-Host "Building blog comment function ($Configuration)..."

& dotnet restore $Solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

& dotnet build $Solution --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Running tests..."
New-Item -ItemType Directory -Path $TestResultsDir -Force | Out-Null
& dotnet test --solution $Solution `
    --configuration $Configuration `
    --no-build `
    --report-trx `
    --results-directory $TestResultsDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Publishing function app..."
New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null
& dotnet publish $FunctionProject `
    --configuration $Configuration `
    --output $WorkDir `
    --self-contained `
    --runtime linux-x64
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Compressing function app..."
Push-Location $WorkDir
try {
    Compress-Archive -Path '*' -DestinationPath (Join-Path $ArtifactsDir 'blog-comments-function.zip') -Force
} finally {
    Pop-Location
}

Remove-Item $WorkDir -Recurse -Force

Write-Host "Function app build complete."
