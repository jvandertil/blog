$root = Join-Path $PSScriptRoot ".."

$artifacts = Join-Path $root "artifacts"
$infraSource = Join-Path $root "infra"

$infraArtifact = Join-Path $artifacts "infra"
$infraProject = Join-Path $infraSource "blog.csproj"

dotnet build -c Release $infraProject
dotnet publish --no-build -c Release -o $infraArtifact $infraProject
