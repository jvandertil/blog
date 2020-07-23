$root = Join-Path $PSScriptRoot ".."
$artifacts = Join-Path $root "artifacts"
$blogArtifact = Join-Path $artifacts "blog"

$source = Join-Path $root "src"

$blogSource = Join-Path $source "blog"

$hugoBin = Join-Path $blogSource "hugo.ps1"

& $hugoBin --source $blogSource --destination $blogArtifact --minify