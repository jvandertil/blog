param(
    [string]$PulumiArtifact = "",
    [string]$PulumiStack = "tst"
)

. $PSScriptRoot\helpers.ps1

$azurePluginVersion = "v3.14.0"
$cloudFlarePluginVersion = "v2.4.0"

Exec { & pulumi plugin install resource azure $azurePluginVersion | Write-Host } "Error installing azure plugin"
Exec { & pulumi plugin install resource cloudflare $cloudFlarePluginVersion | Write-Host } "Error installing cloudflare plugin"

Exec { & pulumi -C $PulumiArtifact stack select $PulumiStack | Write-Host } "Error selecting pulumi stack"
Exec { & pulumi -C $PulumiArtifact up --stack $PulumiStack --yes | Write-Host } "Error running pulumi up"

$azureConnectionString = Exec { & pulumi -C $PulumiArtifact stack output ConnectionString --show-secrets }
$storageAccountName = Exec { & pulumi -C $PulumiArtifact stack output StorageAccountName --show-secrets }
$fullDomainName = Exec { & pulumi -C $PulumiArtifact stack output FullDomainName --show-secrets }

@{
    AzureConnectionString = "$azureConnectionString"
    StorageAccountName = "$storageAccountName"
    FullDomainName = "https://$fullDomainName"
}