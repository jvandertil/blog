#Requires -Version 7.0

function Set-CfCnameRecord {
    <#
    .SYNOPSIS
        Creates or updates a CloudFlare CNAME DNS record.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $ZoneId,

        [Parameter(Mandatory)]
        [string] $ApiKey,

        [Parameter(Mandatory)]
        [string] $Name,

        [Parameter(Mandatory)]
        [string] $Content,

        [Parameter(Mandatory)]
        [bool] $Proxied
    )

    $headers = @{
        'Authorization' = "Bearer $ApiKey"
        'Content-Type'  = 'application/json'
    }

    $listResponse = Invoke-RestMethod `
        -Uri "https://api.cloudflare.com/client/v4/zones/$ZoneId/dns_records" `
        -Headers $headers `
        -ErrorAction Stop

    $existing = $listResponse.result | Where-Object { $_.name -ieq $Name } | Select-Object -First 1

    $body = @{
        type    = 'CNAME'
        name    = $Name
        content = $Content
        ttl     = 1
        proxied = $Proxied
    } | ConvertTo-Json

    if ($null -eq $existing) {
        Write-Host "Creating CNAME record: $Name -> $Content"
        $null = Invoke-RestMethod `
            -Method Post `
            -Uri "https://api.cloudflare.com/client/v4/zones/$ZoneId/dns_records" `
            -Headers $headers `
            -Body $body `
            -ErrorAction Stop
    } else {
        Write-Host "Updating CNAME record: $Name -> $Content"
        $null = Invoke-RestMethod `
            -Method Put `
            -Uri "https://api.cloudflare.com/client/v4/zones/$ZoneId/dns_records/$($existing.id)" `
            -Headers $headers `
            -Body $body `
            -ErrorAction Stop
    }
}

function Invoke-CfCachePurge {
    <#
    .SYNOPSIS
        Purges the entire CloudFlare cache for the given zone.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string] $ZoneId,

        [Parameter(Mandatory)]
        [string] $ApiKey
    )

    Write-Host "Purging CloudFlare cache for zone $ZoneId..."

    $headers = @{
        'Authorization' = "Bearer $ApiKey"
        'Content-Type'  = 'application/json'
    }

    $null = Invoke-RestMethod `
        -Method Post `
        -Uri "https://api.cloudflare.com/client/v4/zones/$ZoneId/purge_cache" `
        -Headers $headers `
        -Body '{"purge_everything":true}' `
        -ErrorAction Stop
}

Export-ModuleMember -Function Set-CfCnameRecord, Invoke-CfCachePurge
