param(
	$StorageAccountName = ""
)

. $PSScriptRoot\helpers.ps1

$ipAddress = $(Invoke-WebRequest https://icanhazip.com | Select -ExpandProperty Content)

Exec { & az storage account network-rule add -n $StorageAccountName --ip-address $ipAddress > $null } "Error authorizing client ip address"
