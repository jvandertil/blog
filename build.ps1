[CmdletBinding()]
Param(
    [Parameter(Position=0)]
    [string] $Target = 'Build',

    [Parameter(Position=1, ValueFromRemainingArguments=$true)]
    [string[]] $TargetArgs
)

Write-Output "PowerShell $($PSVersionTable.PSEdition) version $($PSVersionTable.PSVersion)"

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { Write-Error $_ -ErrorAction Continue; exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

###########################################################################
# DOTNET BOOTSTRAP
###########################################################################

$TempDirectory = "$PSScriptRoot/.tmp/dotnet"

$DotNetGlobalFile = "$PSScriptRoot/global.json"
$DotNetInstallUrl = "https://dot.net/v1/dotnet-install.ps1"
$DotNetChannel = "STS"

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
$env:DOTNET_MULTILEVEL_LOOKUP = 0

function ExecSafe([scriptblock] $cmd) {
    & $cmd
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

# If dotnet CLI is installed globally, use it
if ($null -ne (Get-Command "dotnet" -ErrorAction SilentlyContinue) -and `
     $(dotnet --version) -and $LASTEXITCODE -eq 0) {
    $env:DOTNET_EXE = (Get-Command "dotnet").Path
}
else {
    # Download install script
    $DotNetInstallFile = "$TempDirectory\dotnet-install.ps1"
    New-Item -ItemType Directory -Path $TempDirectory -Force | Out-Null
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    (New-Object System.Net.WebClient).DownloadFile($DotNetInstallUrl, $DotNetInstallFile)

    # If global.json exists, load expected version
    if (Test-Path $DotNetGlobalFile) {
        $DotNetGlobal = $(Get-Content $DotNetGlobalFile | Out-String | ConvertFrom-Json)
        if ($DotNetGlobal.PSObject.Properties["sdk"] -and $DotNetGlobal.sdk.PSObject.Properties["version"]) {
            $DotNetVersion = $DotNetGlobal.sdk.version
        }
    }

    # Install by channel or version
    $DotNetDirectory = "$TempDirectory\dotnet-win"
    if (!(Test-Path variable:DotNetVersion)) {
        ExecSafe { & powershell $DotNetInstallFile -InstallDir $DotNetDirectory -Channel $DotNetChannel -NoPath }
    } else {
        ExecSafe { & powershell $DotNetInstallFile -InstallDir $DotNetDirectory -Version $DotNetVersion -NoPath }
    }
    $env:DOTNET_EXE = "$DotNetDirectory\dotnet.exe"
    $env:PATH = "$DotNetDirectory;$env:PATH"
}

Write-Output "Microsoft (R) .NET Core SDK version $(& $env:DOTNET_EXE --version)"

###########################################################################
# TARGET DISPATCH
###########################################################################

$ScriptsDir = "$PSScriptRoot/eng/scripts"

switch ($Target) {
    'Clean' {
        ExecSafe { & "$ScriptsDir/Clean.ps1" }
    }
    'Build' {
        ExecSafe { & "$ScriptsDir/Build-Content.ps1" }
        ExecSafe { & "$ScriptsDir/Build-Function.ps1" }
    }
    'CleanBuild' {
        ExecSafe { & "$ScriptsDir/Clean.ps1" }
        ExecSafe { & "$ScriptsDir/Build-Content.ps1" }
        ExecSafe { & "$ScriptsDir/Build-Function.ps1" }
    }
    'Serve' {
        ExecSafe { & "$ScriptsDir/Serve.ps1" }
    }
    'Deploy' {
        ExecSafe { & "$ScriptsDir/Deploy.ps1" @TargetArgs }
    }
    'GenerateBicep' {
        $GeneratorProject = "$PSScriptRoot/eng/_pipeline/BicepPsGenerator/BicepPsGenerator.csproj"
        $BicepFile        = "$PSScriptRoot/eng/infra/blog.bicep"
        $OutputPsm1       = "$ScriptsDir/Bicep.generated.psm1"
        ExecSafe { & $env:DOTNET_EXE run --project $GeneratorProject -- --input $BicepFile --output $OutputPsm1 }
    }
    default {
        Write-Error "Unknown target: '$Target'. Valid targets: Clean, Build, CleanBuild, Serve, Deploy, GenerateBicep"
        exit 1
    }
}
