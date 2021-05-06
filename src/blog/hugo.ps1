# Update the version in eng/build-blog.sh as well
$hugoVersion = "0.83.1"
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

mkdir $destination -ErrorAction SilentlyContinue > $null

if( (Test-Path (Join-Path $destination $hugoFilename)) -eq $false )
{
    Write-Host "Downloading new hugo binary"

    Download-File -Url $hugoRelease -Destination (Join-Path $destination $hugoFilename)
    Expand-Archive -Path (Join-Path $destination $hugoFilename) -Destination $destination -Force
}

$hugoExe = (Join-Path $destination hugo.exe)

& $hugoExe $args