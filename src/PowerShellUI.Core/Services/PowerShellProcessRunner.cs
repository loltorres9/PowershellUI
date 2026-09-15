using System.Diagnostics;
using System.Text;
using System.Threading;

namespace PowerShellUI.Core.Services;

public sealed class ProcessOutputEventArgs : EventArgs
{
    public required string Line { get; init; }

    public required bool IsError { get; init; }
}

public sealed class PowerShellProcessResult
{
    public required int ExitCode { get; init; }

    public required string StandardOutput { get; init; }

    public required string StandardError { get; init; }
}

/// <summary>
/// Startet powershell.exe/pwsh.exe als Prozess und streamt dessen Ausgabe.
/// Parameterwerte werden ausschließlich über <see cref="ProcessStartInfo.ArgumentList"/>
/// übergeben, damit keine manuelle Quotierung nötig ist und keine Befehlsinjektion möglich ist.
/// </summary>
public sealed class PowerShellProcessRunner
{
    public Task<PowerShellProcessResult> RunFileAsync(
        PowerShellHostInfo host,
        string scriptPath,
        IReadOnlyList<string> scriptArguments,
        IProgress<ProcessOutputEventArgs>? outputProgress = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = CreateBaseStartInfo(host);
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);

        foreach (var argument in scriptArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        return RunAsync(startInfo, outputProgress, cancellationToken);
    }

    public Task<PowerShellProcessResult> RunCommandAsync(
        PowerShellHostInfo host,
        string command,
        IProgress<ProcessOutputEventArgs>? outputProgress = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = CreateBaseStartInfo(host);
        startInfo.ArgumentList.Add("-Command");
        startInfo.ArgumentList.Add(command);

        return RunAsync(startInfo, outputProgress, cancellationToken);
    }

    private static ProcessStartInfo CreateBaseStartInfo(PowerShellHostInfo host)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = host.ExecutablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-NonInteractive");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");

        return startInfo;
    }

    private static async Task<PowerShellProcessResult> RunAsync(
        ProcessStartInfo startInfo,
        IProgress<ProcessOutputEventArgs>? outputProgress,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var stdOut = new StringBuilder();
        var stdErr = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            stdOut.AppendLine(e.Data);
            outputProgress?.Report(new ProcessOutputEventArgs { Line = e.Data, IsError = false });
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is null)
            {
                return;
            }

            stdErr.AppendLine(e.Data);
            outputProgress?.Report(new ProcessOutputEventArgs { Line = e.Data, IsError = true });
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        return new PowerShellProcessResult
        {
            ExitCode = process.ExitCode,
            StandardOutput = stdOut.ToString(),
            StandardError = stdErr.ToString(),
        };
    }
}
