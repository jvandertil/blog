#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('tst', 'prd')]
    [string] $Environment,

    [Parameter(Mandatory)]
    [string] $CloudflareApiKey,

    [Parameter(Mandatory)]
    [string] $CloudflareZoneId
)

$ErrorActionPreference = 'Stop'

Import-Module "$PSScriptRoot\CloudFlare.psm1" -Force

$AzureLocation     = 'westeurope'
$RepoRoot          = (Resolve-Path "$PSScriptRoot\..\..")
$InfraDir          = Join-Path $RepoRoot 'eng' 'infra'
$BicepFile         = Join-Path $InfraDir 'blog.bicep'
$ArtifactsDir      = Join-Path $RepoRoot 'artifacts'
$BlogArtifact      = Join-Path $ArtifactsDir 'blog.zip'
$FunctionArtifact  = Join-Path $ArtifactsDir 'blog-comments-function.zip'
$GeneratorProject  = Join-Path $RepoRoot 'eng' '_pipeline' 'BicepPsGenerator' 'BicepPsGenerator.csproj'
$GeneratedPsm1     = Join-Path $PSScriptRoot 'Bicep.generated.psm1'

$ResourceGroup = "rg-jvandertil-blog-$Environment"
$CustomDomain  = if ($Environment -eq 'prd') { 'www.jvandertil.nl' } else { "$Environment.jvandertil.nl" }

# ── Generate typed Bicep module ────────────────────────────────────────────────

Write-Host "Generating Bicep PowerShell module..."
& dotnet run --project $GeneratorProject -- --input $BicepFile --output $GeneratedPsm1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Import-Module $GeneratedPsm1 -Force

# ── Create resource group ──────────────────────────────────────────────────────

Write-Host "Creating resource group: $ResourceGroup"
az group create --name $ResourceGroup --location $AzureLocation
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# ── Download CloudFlare IP ranges (required by blog.bicep) ────────────────────

Write-Host "Downloading CloudFlare IP ranges..."
Invoke-WebRequest `
    -Uri 'https://api.cloudflare.com/client/v4/ips' `
    -OutFile (Join-Path $InfraDir 'cloudflare-ips.txt')

# ── Deploy infrastructure via Bicep ───────────────────────────────────────────

Write-Host "Deploying infrastructure to Azure..."
$bicepParams = New-BlogParameters -Env $Environment -Location $AzureLocation
try {
    az deployment group create `
        --mode Complete `
        --template-file $BicepFile `
        --resource-group $ResourceGroup `
        --parameters $bicepParams
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} catch {
    az deployment group list --resource-group $ResourceGroup
    throw
}

# ── Read deployment outputs ────────────────────────────────────────────────────

$deployment = Get-BlogDeployment -ResourceGroup $ResourceGroup

# ── Configure function app CORS ───────────────────────────────────────────────

Write-Host "Configuring CORS on function app..."
az functionapp cors add `
    --resource-group $ResourceGroup `
    --name $deployment.FunctionAppName `
    --allowed-origins "https://$CustomDomain"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# ── Update CloudFlare DNS records ─────────────────────────────────────────────

$storageHost = ([Uri]$deployment.StorageAccountWebEndpoint).Host

Set-CfCnameRecord `
    -ZoneId $CloudflareZoneId -ApiKey $CloudflareApiKey `
    -Name "asverify.$CustomDomain" -Content "asverify.$storageHost" -Proxied $false

Set-CfCnameRecord `
    -ZoneId $CloudflareZoneId -ApiKey $CloudflareApiKey `
    -Name $CustomDomain -Content $storageHost -Proxied $true

# ── Upload blog content ────────────────────────────────────────────────────────

Write-Host "Uploading blog content..."

$ContentPath = Join-Path $ArtifactsDir 'blog-content'
Expand-Archive -Path $BlogArtifact -DestinationPath $ContentPath -Force

# Retrieve storage key, sync content, then rotate the key
$storageKey = (az storage account keys list `
    --resource-group $ResourceGroup `
    --account-name $deployment.StorageAccountName `
    --query "[?keyName == 'key1'].value | [0]" `
    --output tsv).Trim()
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

try {
    Wait-StorageAccess -StorageAccount $deployment.StorageAccountName -StorageKey $storageKey -ContainerName '$web'

    $maxAttempts = 5
    $uploaded    = $false
    for ($attempt = 1; $attempt -le $maxAttempts -and -not $uploaded; $attempt++) {
        try {
            az storage blob sync `
                --source (Join-Path $ContentPath $Environment) `
                --container '$web' `
                --account-name $deployment.StorageAccountName `
                --account-key $storageKey `
                --only-show-errors
            if ($LASTEXITCODE -ne 0) { throw "az storage blob sync exited with code $LASTEXITCODE" }
            $uploaded = $true
        } catch {
            Write-Host "Error while syncing content, retrying ($attempt/$maxAttempts)..."
            if ($attempt -ge $maxAttempts) { throw }
            Start-Sleep -Seconds 10
        }
    }
} finally {
    az storage account keys renew `
        --resource-group $ResourceGroup `
        --account-name $deployment.StorageAccountName `
        --key key1 `
        --output none
}

# ── Enable static website ──────────────────────────────────────────────────────

Write-Host "Enabling static website..."
az storage account update `
    --name $deployment.StorageAccountName `
    --custom-domain $CustomDomain `
    --use-subdomain true `
    --default-action Deny
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

az storage blob service-properties update `
    --auth-mode login `
    --account-name $deployment.StorageAccountName `
    --static-website true `
    --404-document 404.html `
    --index-document index.html
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# ── Purge CloudFlare cache ─────────────────────────────────────────────────────

Invoke-CfCachePurge -ZoneId $CloudflareZoneId -ApiKey $CloudflareApiKey

# ── Deploy function app ────────────────────────────────────────────────────────

Write-Host "Deploying function app..."
$ipAddress = (Invoke-WebRequest -Uri 'http://ipv4.icanhazip.com/').Content.Trim()

az functionapp config access-restriction add `
    --scm-site true `
    --resource-group $ResourceGroup `
    --name $deployment.FunctionAppName `
    --priority 100 `
    --action Allow `
    --rule-name PipelineDeployment `
    --ip-address $ipAddress
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

try {
    az functionapp deployment source config-zip `
        --resource-group $ResourceGroup `
        --name $deployment.FunctionAppName `
        --src $FunctionArtifact `
        --only-show-errors
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally {
    try {
        az functionapp config access-restriction remove `
            --scm-site true `
            --resource-group $ResourceGroup `
            --name $deployment.FunctionAppName `
            --rule-name PipelineDeployment
    } catch {
        Write-Warning "Failed to remove SCM firewall rule: $_"
    }
}

Write-Host "Deployment complete!"

# ── Helper functions ───────────────────────────────────────────────────────────

function Wait-StorageAccess {
    param(
        [Parameter(Mandatory)] [string] $StorageAccount,
        [Parameter(Mandatory)] [string] $StorageKey,
        [Parameter(Mandatory)] [string] $ContainerName
    )

    Write-Host "Checking if storage network rule has propagated..."

    $waitTimeSecs  = 5
    $timeoutSecs   = 300
    $elapsedSecs   = 0

    while ($true) {
        # Capture output and suppress it; only the exit code matters.
        $null = az storage blob list `
            --account-name $StorageAccount `
            --account-key $StorageKey `
            --container-name $ContainerName `
            --num-results 1 `
            --output none 2>&1

        if ($LASTEXITCODE -eq 0) { return }

        if ($elapsedSecs -ge $timeoutSecs) {
            throw "Storage network rule did not propagate within ${timeoutSecs}s."
        }

        Write-Host "Storage request failed, sleeping ${waitTimeSecs}s and retrying..."
        Start-Sleep -Seconds $waitTimeSecs
        $elapsedSecs += $waitTimeSecs
    }
}
