using System.Threading;
using PowerShellUI.Core.Models;

namespace PowerShellUI.Core.Services;

public sealed class ModuleCheckResult
{
    public required string ModuleName { get; init; }

    public required bool IsInstalled { get; init; }

    public string? InstalledVersion { get; init; }
}

/// <summary>
/// Prüft, ob von einem Skript benötigte Module (z. B. PnP.PowerShell, ExchangeOnlineManagement,
/// Microsoft.Graph, ActiveDirectory) im jeweiligen PowerShell-Host verfügbar sind, und kann
/// fehlende Module per Knopfdruck über Install-Module (Scope CurrentUser) nachinstallieren.
/// </summary>
public sealed class ModuleManagerService
{
    private readonly PowerShellProcessRunner _runner;
    private string? _checkScriptPath;
    private string? _installScriptPath;

    public ModuleManagerService(PowerShellProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<ModuleCheckResult> CheckModuleAsync(
        PowerShellHostInfo host,
        RequiredModuleInfo module,
        CancellationToken cancellationToken = default)
    {
        var scriptPath = GetCheckScriptPath();
        var arguments = new List<string> { "-Name", module.Name };
        if (!string.IsNullOrWhiteSpace(module.Version))
        {
            arguments.Add("-MinimumVersion");
            arguments.Add(module.Version);
        }

        var result = await _runner.RunFileAsync(host, scriptPath, arguments, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var output = result.StandardOutput.Trim();
        if (result.ExitCode == 0 && output.Length > 0 && output != "NOTFOUND")
        {
            return new ModuleCheckResult { ModuleName = module.Name, IsInstalled = true, InstalledVersion = output };
        }

        return new ModuleCheckResult { ModuleName = module.Name, IsInstalled = false };
    }

    public Task<PowerShellProcessResult> InstallModuleAsync(
        PowerShellHostInfo host,
        RequiredModuleInfo module,
        IProgress<ProcessOutputEventArgs>? outputProgress = null,
        CancellationToken cancellationToken = default)
    {
        var scriptPath = GetInstallScriptPath();
        var arguments = new List<string> { "-Name", module.Name };
        if (!string.IsNullOrWhiteSpace(module.Version))
        {
            arguments.Add("-MinimumVersion");
            arguments.Add(module.Version);
        }

        return _runner.RunFileAsync(host, scriptPath, arguments, outputProgress, cancellationToken);
    }

    private string GetCheckScriptPath()
        => _checkScriptPath ??= EmbeddedAssets.ExtractToTempFile("Test-ModuleAvailability.ps1", "Test-ModuleAvailability.ps1");

    private string GetInstallScriptPath()
        => _installScriptPath ??= EmbeddedAssets.ExtractToTempFile("Install-RequiredModule.ps1", "Install-RequiredModule.ps1");
}
