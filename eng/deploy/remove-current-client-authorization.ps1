param(
	$StorageAccountName = ""
)

. $PSScriptRoot\helpers.ps1

$ipAddress = $(Invoke-WebRequest https://icanhazip.com | Select -ExpandProperty Content)

Exec { & az storage account network-rule remove -n $storageAccountName --ip-address $ipAddress > $null } "Error removing client ip address authorization"
