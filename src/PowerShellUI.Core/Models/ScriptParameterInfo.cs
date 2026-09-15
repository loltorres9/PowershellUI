namespace PowerShellUI.Core.Models;

public sealed class ScriptParameterInfo
{
    public required string Name { get; init; }

    public required string TypeName { get; init; }

    public bool Mandatory { get; init; }

    public string? DefaultValue { get; init; }

    public string? HelpMessage { get; init; }

    public IReadOnlyList<string> ValidateSet { get; init; } = Array.Empty<string>();

    public bool IsSwitch =>
        TypeName.Equals("switch", StringComparison.OrdinalIgnoreCase) ||
        TypeName.Equals("bool", StringComparison.OrdinalIgnoreCase) ||
        TypeName.Equals("boolean", StringComparison.OrdinalIgnoreCase);
}
