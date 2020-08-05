param(
    [string]$BlogArtifact = "",
    [string]$UploaderArtifact = "",
    [string]$PulumiArtifact = "",
    [string]$PulumiStack = "tst", 
    [string]$CloudFlareZoneId = "",
    [string]$CloudFlareZoneUrlRoot = "https://www.jvandertil.nl/",
    [string]$CloudFlareApiKey = ""
)

. $PSScriptRoot\helpers.ps1

$root = Join-Path $PSScriptRoot ".." | Join-Path -ChildPath ".."
$artifacts = Join-Path $root "artifacts"

$blogArtifact = if ( $BlogArtifact -eq "" ) { Join-Path $artifacts "blog" } else { $BlogArtifact }
$uploaderArtifact = if ( $UploaderArtifact -eq "" ) { Join-Path $artifacts "uploader" } else { $UploaderArtifact }
$pulumiArtifact = if ( $PulumiArtifact -eq "" ) { Join-Path $artifacts "infra" } else { $PulumiArtifact }

$uploaderBin = Join-Path $UploaderArtifact "Uploader.exe"

Write-Host "Provisioning cloud infrastructure"
$pulumiOutput = & $PSScriptRoot\run-pulumi.ps1 -PulumiArtifact $pulumiArtifact -PulumiStack $PulumiStack

$storageAccountName = $pulumiOutput.StorageAccountName
$azureConnectionString = $pulumiOutput.AzureConnectionString

Write-Host "Authorizing client ip address"
& $PSScriptRoot\add-current-client-authorization.ps1 -StorageAccountName $storageAccountName

Write-Host "Waiting for firewall rules to propagate."
sleep -Seconds 15

try {

    Write-Host "Invoking uploader"
    Exec { & $uploaderBin --source $blogArtifact --destination $azureConnectionString --cf-apikey $CloudFlareApiKey --cf-zoneid $CloudFlareZoneId --cf-urlroot $CloudFlareZoneUrlRoot | Write-Host }

} finally {
    Write-Host "Removing client ip address authorization"
    & $PSScriptRoot\remove-current-client-authorization.ps1 -StorageAccountName $storageAccountName
}

Write-Host "Done."
