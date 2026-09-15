[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [string]$MinimumVersion
)

$ErrorActionPreference = 'Stop'

$modules = Get-Module -ListAvailable -Name $Name -ErrorAction SilentlyContinue

if (-not $modules) {
    Write-Output 'NOTFOUND'
    exit 0
}

if ($MinimumVersion) {
    $requiredVersion = [Version]$MinimumVersion
    $matchingModule = $modules | Where-Object { $_.Version -ge $requiredVersion } | Sort-Object Version -Descending | Select-Object -First 1
}
else {
    $matchingModule = $modules | Sort-Object Version -Descending | Select-Object -First 1
}

if (-not $matchingModule) {
    Write-Output 'NOTFOUND'
    exit 0
}

Write-Output $matchingModule.Version.ToString()
