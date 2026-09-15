using PowerShellUI.Core.Models;

namespace PowerShellUI.Core.Services;

public sealed class PowerShellHostInfo
{
    public required string ExecutablePath { get; init; }

    public required PowerShellEdition Edition { get; init; }

    public override string ToString() => Edition switch
    {
        PowerShellEdition.Core7 => "PowerShell 7",
        PowerShellEdition.Desktop51 => "Windows PowerShell 5.1",
        _ => "PowerShell",
    };
}

/// <summary>
/// Findet installierte PowerShell-Hosts (Windows PowerShell 5.1 und PowerShell 7) auf dem System.
/// </summary>
public sealed class PowerShellHostLocator
{
    private static readonly string[] Core7CandidatePaths =
    {
        @"C:\Program Files\PowerShell\7\pwsh.exe",
        @"C:\Program Files (x86)\PowerShell\7\pwsh.exe",
    };

    private static readonly string[] Desktop51CandidatePaths =
    {
        @"C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe",
        @"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe",
    };

    public PowerShellHostInfo? FindCore7()
        => FindExecutable("pwsh", Core7CandidatePaths, PowerShellEdition.Core7);

    public PowerShellHostInfo? FindDesktop51()
        => FindExecutable("powershell", Desktop51CandidatePaths, PowerShellEdition.Desktop51);

    public PowerShellHostInfo Resolve(PowerShellEdition requested)
    {
        if (requested == PowerShellEdition.Core7)
        {
            return FindCore7()
                ?? throw new InvalidOperationException("PowerShell 7 (pwsh.exe) wurde nicht gefunden. Bitte installieren Sie PowerShell 7.");
        }

        if (requested == PowerShellEdition.Desktop51)
        {
            return FindDesktop51()
                ?? throw new InvalidOperationException("Windows PowerShell 5.1 wurde nicht gefunden.");
        }

        return FindCore7()
            ?? FindDesktop51()
            ?? throw new InvalidOperationException("Es wurde weder PowerShell 7 noch Windows PowerShell 5.1 gefunden.");
    }

    /// <summary>
    /// Wählt den passenden Host für ein Skript. Ein per '#Requires -PSEdition' erzwungenes
    /// Edition-Erfordernis des Skripts hat Vorrang vor der vom Benutzer gewählten Edition.
    /// </summary>
    public PowerShellHostInfo ResolveForScript(ScriptInfo script, PowerShellEdition preferred)
    {
        var wantsCore = script.RequiredPSEditions.Contains("Core", StringComparer.OrdinalIgnoreCase);
        var wantsDesktop = script.RequiredPSEditions.Contains("Desktop", StringComparer.OrdinalIgnoreCase);

        if (wantsCore && !wantsDesktop)
        {
            return FindCore7()
                ?? throw new InvalidOperationException($"'{script.Name}' benötigt PowerShell 7 (Core), das aber nicht installiert ist.");
        }

        if (wantsDesktop && !wantsCore)
        {
            return FindDesktop51()
                ?? throw new InvalidOperationException($"'{script.Name}' benötigt Windows PowerShell 5.1 (Desktop), das aber nicht installiert ist.");
        }

        return Resolve(preferred);
    }

    private static PowerShellHostInfo? FindExecutable(string commandName, string[] candidatePaths, PowerShellEdition edition)
    {
        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
            {
                return new PowerShellHostInfo { ExecutablePath = path, Edition = edition };
            }
        }

        var fromPath = FindOnPath(commandName);
        return fromPath is null ? null : new PowerShellHostInfo { ExecutablePath = fromPath, Edition = edition };
    }

    private static string? FindOnPath(string commandName)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathVariable))
        {
            return null;
        }

        var executableNames = OperatingSystem.IsWindows()
            ? new[] { commandName + ".exe" }
            : new[] { commandName };

        foreach (var directory in pathVariable.Split(Path.PathSeparator))
        {
            foreach (var executableName in executableNames)
            {
                var candidate = Path.Combine(directory, executableName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }
}
