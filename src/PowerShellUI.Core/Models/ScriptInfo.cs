namespace PowerShellUI.Core.Models;

public sealed class ScriptInfo
{
    public required string FilePath { get; init; }

    public required string Name { get; init; }

    public required string Category { get; init; }

    public string? Synopsis { get; init; }

    public IReadOnlyList<ScriptParameterInfo> Parameters { get; init; } = Array.Empty<ScriptParameterInfo>();

    public IReadOnlyList<RequiredModuleInfo> RequiredModules { get; init; } = Array.Empty<RequiredModuleInfo>();

    public IReadOnlyList<string> RequiredPSEditions { get; init; } = Array.Empty<string>();

    public string? RequiredPSVersion { get; init; }

    public override string ToString() => Name;
}
