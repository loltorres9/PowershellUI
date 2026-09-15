[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [string]$MinimumVersion
)

$ErrorActionPreference = 'Stop'

Write-Host "Pruefe NuGet-Paketanbieter..."
$nugetProvider = Get-PackageProvider -Name NuGet -ListAvailable -ErrorAction SilentlyContinue
if (-not $nugetProvider) {
    Write-Host "Installiere NuGet-Paketanbieter..."
    Install-PackageProvider -Name NuGet -MinimumVersion 2.8.5.201 -Force -Scope CurrentUser | Out-Null
}

$psGallery = Get-PSRepository -Name PSGallery -ErrorAction SilentlyContinue
if ($psGallery -and $psGallery.InstallationPolicy -ne 'Trusted') {
    Write-Host "Vertraue PSGallery fuer diese Installation..."
    Set-PSRepository -Name PSGallery -InstallationPolicy Trusted
}

$installArgs = @{
    Name         = $Name
    Scope        = 'CurrentUser'
    Force        = $true
    AllowClobber = $true
    ErrorAction  = 'Stop'
}

if ($MinimumVersion) {
    $installArgs['MinimumVersion'] = $MinimumVersion
}

Write-Host "Installiere Modul '$Name'..."
Install-Module @installArgs
Write-Host "Modul '$Name' wurde installiert."
