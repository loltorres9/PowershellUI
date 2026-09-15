#Requires -Modules Az.Resources
#Requires -PSEdition Core

<#
.SYNOPSIS
    Erstellt eine neue Azure Resource Group.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Name der Resource Group")]
    [string]$Name,

    [Parameter(Mandatory = $true, HelpMessage = "Azure-Region")]
    [ValidateSet('westeurope', 'northeurope', 'germanywestcentral', 'eastus')]
    [string]$Location,

    [Parameter(HelpMessage = "Tags im Format Key=Value, mehrere durch Komma getrennt")]
    [string]$Tags
)

if (-not (Get-AzContext)) {
    Connect-AzAccount | Out-Null
}

$tagTable = @{}
if ($Tags) {
    foreach ($pair in $Tags -split ',') {
        $key, $value = $pair -split '=', 2
        if ($key -and $value) {
            $tagTable[$key.Trim()] = $value.Trim()
        }
    }
}

New-AzResourceGroup -Name $Name -Location $Location -Tag $tagTable
