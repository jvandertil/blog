+++
author = "Jos van der Til"
title = "Hosting a Hugo blog on Azure"
date  = 2019-10-19T15:00:00+01:00
type = "post"
tags = [ "Azure", "Azure DevOps", "Powershell" ]
draft = true
+++

Script in repository to download and run Hugo:
```powershell
$hugoVersion = "0.58.3"
$hugoFilename = "hugo_extended_$hugoVersion`_Windows-64bit.zip"
$hugoRelease = "https://github.com/gohugoio/hugo/releases/download/v$hugoVersion/$hugoFilename"
$destination = Join-Path $PSScriptRoot ".bin"

function Download-File($Url, $Destination)
{
    try
    {
        $wc = New-Object System.Net.WebClient
        $wc.DownloadFile($Url, $Destination)
    } finally {
        $wc.Dispose()
    }
}

mkdir $destination -ErrorAction SilentlyContinue

if( (Test-Path (Join-Path $destination $hugoFilename)) -eq $false )
{
    Download-File -Url $hugoRelease -Destination (Join-Path $destination $hugoFilename)
    Expand-Archive -Path (Join-Path $destination $hugoFilename) -Destination $destination -Force
}

$hugoExe = (Join-Path $destination hugo.exe)

& $hugoExe $args
```

Now you can use `.\hugo.ps1` to invoke hugo from the root of your repository.

Azure powershell step to build:
```
.\hugo.ps1 --destination $(Build.ArtifactStagingDirectory)
```

Variables needed for publishing to Azure DevOps:

| Name | Value |
|------|-------|
| StorageAccountKey | *secret*  |
| StorageAccountName | *secret* |
| StorageContainer | $web |
| StaticContentLocation | $(System.ArtifactStagingDirectory)\\\<artifact name\> |

For the powershell scripts using the `az` command line client.
Be sure to set the `AZURE_STORAGE_KEY` and `AZURE_STORAGE_ACCOUNT` environment variables.

Cleaning the container from powershell:
```powershell
az storage blob delete-batch -s '$(StorageContainer)' --pattern *
```

Uploading the new content to the container
```powershell
az storage blob upload-batch -d '$(StorageContainer)' -s '$(StaticContentLocation)' --pattern *
```

Variables needed for clearing the CloudFlare cache:

| Name | Source |
| ---  | ---   |
| CloudFlareApiKey | From your account |
| CloudFlareEmail | From your account |
| CloudFlareZoneId | From the site in cloudflare | 

Be sure to set these variables as environment variables in the script step to keep them secure.

Purging the entire CloudFlare cache for your site, probably needs some optimization when things get bigger so that only changed content is uploaded.
```powershell
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$headers = @{
"Content-Type" = "application/json"; 
"X-Auth-Email" = "$env:CloudFlareEmail";
"X-Auth-Key" = "$env:CloudFlareAPIKey"}

Invoke-RestMethod -Method POST -Body '{"purge_everything":true}' -Headers $headers "https://api.cloudflare.com/client/v4/zones/$env:CloudFlareZoneId/purge_cache"
```