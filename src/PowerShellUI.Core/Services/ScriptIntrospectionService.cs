using System.Text.Json;
using System.Threading;
using PowerShellUI.Core.Models;

namespace PowerShellUI.Core.Services;

/// <summary>
/// Liest ein PowerShell-Skript per <c>System.Management.Automation.Language.Parser</c> ein
/// (ausgeführt im jeweiligen PowerShell-Host, siehe Assets/Introspect-Script.ps1) und wandelt
/// dessen param()-Block, #Requires-Angaben und Kommentar-Hilfe in ein <see cref="ScriptInfo"/> um.
/// </summary>
public sealed class ScriptIntrospectionService
{
    private readonly PowerShellProcessRunner _runner;
    private string? _introspectScriptPath;

    public ScriptIntrospectionService(PowerShellProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<ScriptInfo> IntrospectAsync(
        PowerShellHostInfo host,
        string scriptFilePath,
        string category,
        CancellationToken cancellationToken = default)
    {
        var introspectScriptPath = GetIntrospectScriptPath();

        var result = await _runner.RunFileAsync(
            host,
            introspectScriptPath,
            new[] { "-Path", scriptFilePath },
            outputProgress: null,
            cancellationToken).ConfigureAwait(false);

        if (result.ExitCode != 0 || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return new ScriptInfo
            {
                FilePath = scriptFilePath,
                Name = Path.GetFileNameWithoutExtension(scriptFilePath),
                Category = category,
                Synopsis = $"Konnte nicht analysiert werden: {result.StandardError.Trim()}",
            };
        }

        return ParseIntrospectionJson(result.StandardOutput, scriptFilePath, category);
    }

    private string GetIntrospectScriptPath()
        => _introspectScriptPath ??= EmbeddedAssets.ExtractToTempFile("Introspect-Script.ps1", "Introspect-Script.ps1");

    private static ScriptInfo ParseIntrospectionJson(string json, string scriptFilePath, string category)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var parameters = new List<ScriptParameterInfo>();
        foreach (var parameterElement in EnumerateArray(root, "Parameters"))
        {
            parameters.Add(new ScriptParameterInfo
            {
                Name = GetString(parameterElement, "Name") ?? "Parameter",
                TypeName = GetString(parameterElement, "Type") ?? "String",
                Mandatory = GetBool(parameterElement, "Mandatory"),
                DefaultValue = GetString(parameterElement, "DefaultValue"),
                HelpMessage = GetString(parameterElement, "HelpMessage"),
                ValidateSet = GetStringArray(parameterElement, "ValidateSet"),
            });
        }

        var requiredModules = new List<RequiredModuleInfo>();
        foreach (var moduleElement in EnumerateArray(root, "RequiredModules"))
        {
            var name = GetString(moduleElement, "Name");
            if (name is null)
            {
                continue;
            }

            requiredModules.Add(new RequiredModuleInfo
            {
                Name = name,
                Version = GetString(moduleElement, "Version"),
            });
        }

        return new ScriptInfo
        {
            FilePath = scriptFilePath,
            Name = Path.GetFileNameWithoutExtension(scriptFilePath),
            Category = category,
            Synopsis = GetString(root, "Synopsis"),
            Parameters = parameters,
            RequiredModules = requiredModules,
            RequiredPSEditions = GetStringArray(root, "RequiredPSEditions"),
            RequiredPSVersion = GetString(root, "RequiredPSVersion"),
        };
    }

    private static IEnumerable<JsonElement> EnumerateArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            yield break;
        }

        // ConvertTo-Json kann ein einzelnes Objekt statt eines Ein-Elemente-Arrays liefern.
        if (value.ValueKind == JsonValueKind.Object)
        {
            yield return value;
            yield break;
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var item in value.EnumerateArray())
        {
            yield return item;
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    private static bool GetBool(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return false;
        }

        return value.ValueKind == JsonValueKind.True;
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return Array.Empty<string>();
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            var text = value.GetString();
            return text is null ? Array.Empty<string>() : new[] { text };
        }

        if (value.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<string>();
        }

        var list = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var text = item.GetString();
                if (text is not null)
                {
                    list.Add(text);
                }
            }
        }

        return list;
    }
}
