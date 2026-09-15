[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path)) {
    Write-Error "Datei nicht gefunden: $Path"
    exit 1
}

$tokens = $null
$parseErrors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($Path, [ref]$tokens, [ref]$parseErrors)

$parameters = New-Object System.Collections.ArrayList

if ($ast.ParamBlock) {
    foreach ($parameterAst in $ast.ParamBlock.Parameters) {
        $name = $parameterAst.Name.VariablePath.UserPath
        $typeName = 'String'
        if ($parameterAst.StaticType) {
            $typeName = $parameterAst.StaticType.Name
        }

        $mandatory = $false
        $helpMessage = $null
        $validateSetValues = New-Object System.Collections.ArrayList

        foreach ($attributeAst in $parameterAst.Attributes) {
            if ($attributeAst.TypeName.Name -eq 'Parameter') {
                foreach ($namedArgument in $attributeAst.NamedArguments) {
                    if ($namedArgument.ArgumentName -eq 'Mandatory') {
                        $mandatory = $true
                    }
                    if ($namedArgument.ArgumentName -eq 'HelpMessage') {
                        $helpMessage = $namedArgument.Argument.Value
                    }
                }
            }
            elseif ($attributeAst.TypeName.Name -eq 'ValidateSet') {
                foreach ($positionalArgument in $attributeAst.PositionalArguments) {
                    [void]$validateSetValues.Add($positionalArgument.Value)
                }
            }
        }

        $defaultValue = $null
        if ($parameterAst.DefaultValue) {
            $defaultValue = $parameterAst.DefaultValue.Extent.Text
        }

        [void]$parameters.Add([PSCustomObject]@{
            Name         = $name
            Type         = $typeName
            Mandatory    = $mandatory
            DefaultValue = $defaultValue
            HelpMessage  = $helpMessage
            ValidateSet  = @($validateSetValues)
        })
    }
}

$synopsis = $null
try {
    $help = Get-Help -Name $Path -ErrorAction SilentlyContinue
    if ($help -and $help.Synopsis -and ($help.Synopsis -notmatch '^\s*$') -and ($help.Synopsis -notlike "$Path*")) {
        $synopsis = $help.Synopsis.Trim()
    }
}
catch {
    $synopsis = $null
}

$requiredModules = New-Object System.Collections.ArrayList
$requiredEditions = New-Object System.Collections.ArrayList
$requiredVersion = $null

if ($ast.ScriptRequirements) {
    $requirements = $ast.ScriptRequirements

    if ($requirements.RequiredModules) {
        foreach ($moduleSpec in $requirements.RequiredModules) {
            $moduleVersion = $null
            if ($moduleSpec.Version) {
                $moduleVersion = $moduleSpec.Version.ToString()
            }

            [void]$requiredModules.Add([PSCustomObject]@{
                Name    = $moduleSpec.Name
                Version = $moduleVersion
            })
        }
    }

    if ($requirements.RequiredPSEditions) {
        foreach ($edition in $requirements.RequiredPSEditions) {
            [void]$requiredEditions.Add($edition)
        }
    }

    if ($requirements.RequiredPSVersion) {
        $requiredVersion = $requirements.RequiredPSVersion.ToString()
    }
}

$result = [PSCustomObject]@{
    Synopsis           = $synopsis
    Parameters         = @($parameters)
    RequiredModules    = @($requiredModules)
    RequiredPSEditions = @($requiredEditions)
    RequiredPSVersion  = $requiredVersion
}

$result | ConvertTo-Json -Depth 8 -Compress
