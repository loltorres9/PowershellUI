using System.Threading;
using PowerShellUI.Core.Models;

namespace PowerShellUI.Core.Services;

/// <summary>
/// Führt ein Bibliotheksskript mit den vom Benutzer im UI eingegebenen Parameterwerten aus.
/// Werte werden als separate Prozessargumente übergeben (kein String-Zusammenbau), damit keine
/// Quotierungs- oder Injektionsprobleme entstehen.
/// </summary>
public sealed class ScriptExecutionService
{
    private readonly PowerShellProcessRunner _runner;

    public ScriptExecutionService(PowerShellProcessRunner runner)
    {
        _runner = runner;
    }

    public Task<PowerShellProcessResult> ExecuteAsync(
        PowerShellHostInfo host,
        ScriptInfo script,
        IReadOnlyDictionary<string, string?> parameterValues,
        IProgress<ProcessOutputEventArgs>? outputProgress = null,
        CancellationToken cancellationToken = default)
    {
        var arguments = BuildArguments(script, parameterValues);
        return _runner.RunFileAsync(host, script.FilePath, arguments, outputProgress, cancellationToken);
    }

    internal static List<string> BuildArguments(ScriptInfo script, IReadOnlyDictionary<string, string?> parameterValues)
    {
        var arguments = new List<string>();

        foreach (var parameter in script.Parameters)
        {
            if (!parameterValues.TryGetValue(parameter.Name, out var value))
            {
                continue;
            }

            if (parameter.IsSwitch)
            {
                if (string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1")
                {
                    arguments.Add($"-{parameter.Name}");
                }

                continue;
            }

            if (string.IsNullOrEmpty(value))
            {
                if (parameter.Mandatory)
                {
                    throw new InvalidOperationException($"Pflichtparameter '{parameter.Name}' wurde nicht ausgefüllt.");
                }

                continue;
            }

            arguments.Add($"-{parameter.Name}");
            arguments.Add(value);
        }

        return arguments;
    }
}
