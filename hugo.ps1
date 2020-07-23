$hugoVersion = "0.74.2"
$hugoFilename = "hugo_extended_$hugoVersion`_Windows-64bit.zip"
$hugoRelease = "https://github.com/gohugoio/hugo/releases/download/v$hugoVersion/$hugoFilename"
$destination = Join-Path $PSScriptRoot ".bin"

function Download-File($Url, $Destination)
{
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12;
    Invoke-WebRequest -Uri $Url -OutFile $Destination
}

mkdir $destination -ErrorAction SilentlyContinue > $null

if( (Test-Path (Join-Path $destination $hugoFilename)) -eq $false )
{
    Write-Host "Downloading new hugo binary"

    Download-File -Url $hugoRelease -Destination (Join-Path $destination $hugoFilename)
    Expand-Archive -Path (Join-Path $destination $hugoFilename) -Destination $destination -Force
}

$hugoExe = (Join-Path $destination hugo.exe)

& $hugoExe $args