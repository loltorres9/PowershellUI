#Requires -Modules Microsoft.Graph.Groups

<#
.SYNOPSIS
    Erstellt eine neue Microsoft Entra ID-Gruppe ueber Microsoft Graph.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Anzeigename der Gruppe")]
    [string]$DisplayName,

    [Parameter(Mandatory = $true, HelpMessage = "E-Mail-Nickname (ohne Leerzeichen)")]
    [string]$MailNickname,

    [Parameter(HelpMessage = "Gruppentyp")]
    [ValidateSet('Security', 'Microsoft365')]
    [string]$GroupType = 'Security',

    [Parameter(HelpMessage = "Gruppenbeschreibung")]
    [string]$Description
)

if (-not (Get-MgContext)) {
    Connect-MgGraph -Scopes 'Group.ReadWrite.All' | Out-Null
}

$groupParams = @{
    DisplayName     = $DisplayName
    MailNickname    = $MailNickname
    MailEnabled     = ($GroupType -eq 'Microsoft365')
    SecurityEnabled = $true
    Description     = $Description
}

if ($GroupType -eq 'Microsoft365') {
    $groupParams['GroupTypes'] = @('Unified')
}

New-MgGroup @groupParams
