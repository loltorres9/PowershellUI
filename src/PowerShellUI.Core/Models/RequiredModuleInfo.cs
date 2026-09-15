namespace PowerShellUI.Core.Models;

public sealed class RequiredModuleInfo
{
    public required string Name { get; init; }

    public string? Version { get; init; }
}
