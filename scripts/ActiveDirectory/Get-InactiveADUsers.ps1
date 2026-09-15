#Requires -Modules ActiveDirectory

<#
.SYNOPSIS
    Listet Active Directory-Benutzer auf, die seit einer bestimmten Anzahl Tage nicht angemeldet waren.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Anzahl Tage ohne Anmeldung")]
    [int]$InactiveDays,

    [Parameter(HelpMessage = "Ziel-OU (DistinguishedName), optional")]
    [string]$SearchBase,

    [Parameter(HelpMessage = "Deaktivierte Benutzer ebenfalls anzeigen")]
    [switch]$IncludeDisabled
)

$cutoffDate = (Get-Date).AddDays(-$InactiveDays)

$searchParams = @{
    Filter     = { LastLogonTimestamp -lt $cutoffDate }
    Properties = 'LastLogonTimestamp', 'Enabled'
}

if ($SearchBase) {
    $searchParams['SearchBase'] = $SearchBase
}

$users = Get-ADUser @searchParams

if (-not $IncludeDisabled) {
    $users = $users | Where-Object { $_.Enabled }
}

$users |
    Select-Object Name, SamAccountName, Enabled, @{Name = 'LastLogon'; Expression = { [DateTime]::FromFileTime($_.LastLogonTimestamp) } } |
    Format-Table -AutoSize
