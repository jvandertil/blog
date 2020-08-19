param(
    [string]$BlogArtifact = "",
    [string]$UploaderArtifact = "",
    [string]$PulumiArtifact = "",
    [string]$BlogCommentArtifact = "",
    [string]$PulumiStack = "tst", 
    [string]$CloudFlareZoneId = "",
    [string]$CloudFlareApiKey = ""
)

. $PSScriptRoot\helpers.ps1

$root = Join-Path $PSScriptRoot ".." | Join-Path -ChildPath ".."
$artifacts = Join-Path $root "artifacts"

$blogArtifact = if ( $BlogArtifact -eq "" ) { Join-Path $artifacts "blog" } else { $BlogArtifact }
$uploaderArtifact = if ( $UploaderArtifact -eq "" ) { Join-Path $artifacts "uploader" } else { $UploaderArtifact }
$pulumiArtifact = if ( $PulumiArtifact -eq "" ) { Join-Path $artifacts "infra" } else { $PulumiArtifact }
$blogCommentArtifact = if ( $BlogCommentArtifact -eq "" ) { Join-Path $artifacts "blog-comment-function.zip" }

$uploaderBin = Join-Path $UploaderArtifact "Uploader.exe"

Write-Host "Provisioning cloud infrastructure"
$pulumiOutput = & $PSScriptRoot\run-pulumi.ps1 -PulumiArtifact $pulumiArtifact -PulumiStack $PulumiStack

$storageAccountName = $pulumiOutput.StorageAccountName
$azureConnectionString = $pulumiOutput.AzureConnectionString
$cloudFlareZoneUrlRoot = $pulumiOutput.FullDomainName

Write-Host "Deploying Azure Function"
Exec { & az functionapp deployment source config-zip --src $blogCommentArtifact --resource-group $pulumiOutput.ResourceGroup --name $pulumiOutput.FunctionApp }

Write-Host "Authorizing client ip address"
& $PSScriptRoot\add-current-client-authorization.ps1 -StorageAccountName $storageAccountName

Write-Host "Waiting for firewall rules to propagate."
sleep -Seconds 15

try {

    Write-Host "Invoking uploader"
    Exec { & $uploaderBin --source $blogArtifact --destination $azureConnectionString --cf-apikey $CloudFlareApiKey --cf-zoneid $CloudFlareZoneId --cf-urlroot $cloudFlareZoneUrlRoot | Write-Host }

} finally {
    Write-Host "Removing client ip address authorization"
    & $PSScriptRoot\remove-current-client-authorization.ps1 -StorageAccountName $storageAccountName
}

Write-Host "Done."
