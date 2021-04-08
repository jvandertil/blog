$root = Join-Path $PSScriptRoot ".."

$artifacts = Join-Path $root "artifacts"
$source = Join-Path $root "src"

$artifact = Join-Path $artifacts "blog-comment-function"
$solution = Join-Path $source "blog-comment-function" | Join-Path -ChildPath "src" | Join-Path -ChildPath "BlogComments" | Join-Path -ChildPath "BlogComments.csproj"

dotnet build -c Release $solution
dotnet test --no-build -c Release $solution
dotnet publish --no-build -c Release -o $artifact $solution

Compress-Archive $artifact\* -DestinationPath "$artifact.zip" -Force