$root = Join-Path $PSScriptRoot ".."

$artifacts = Join-Path $root "artifacts"
$source = Join-Path $root "src"

$uploaderArtifact = Join-Path $artifacts "uploader"
$uploaderSolution = Join-Path $source "Uploader" | Join-Path -ChildPath "Uploader.sln"

dotnet build -c Release $uploaderSolution
dotnet test -c Release --no-build $uploaderSolution
dotnet publish -c Release -o $uploaderArtifact $uploaderSolution