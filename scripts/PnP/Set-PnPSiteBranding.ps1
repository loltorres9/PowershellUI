#Requires -Modules PnP.PowerShell

<#
.SYNOPSIS
    Setzt Titel und Logo einer SharePoint-Website ueber PnP PowerShell.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, HelpMessage = "URL der SharePoint-Website")]
    [string]$SiteUrl,

    [Parameter(Mandatory = $true, HelpMessage = "Neuer Websitetitel")]
    [string]$Title,

    [Parameter(HelpMessage = "Pfad oder URL zum Logo")]
    [string]$LogoUrl
)

Connect-PnPOnline -Url $SiteUrl -Interactive

Set-PnPSite -Title $Title

if ($LogoUrl) {
    Set-PnPSite -LogoFilePath $LogoUrl
}

Write-Host "Branding fuer '$SiteUrl' aktualisiert."
