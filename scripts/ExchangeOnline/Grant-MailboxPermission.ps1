#Requires -Modules ExchangeOnlineManagement

<#
.SYNOPSIS
    Vergibt FullAccess-Postfachberechtigungen in Exchange Online.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "Ziel-Postfach (E-Mail-Adresse)")]
    [string]$Mailbox,

    [Parameter(Mandatory = $true, HelpMessage = "Benutzer, der Zugriff erhalten soll")]
    [string]$User,

    [Parameter(HelpMessage = "Automatisches Mapping in Outlook aktivieren")]
    [switch]$AutoMapping
)

if (-not (Get-ConnectionInformation)) {
    Connect-ExchangeOnline | Out-Null
}

Add-MailboxPermission -Identity $Mailbox -User $User -AccessRights FullAccess -AutoMapping:$AutoMapping -InheritanceType All
