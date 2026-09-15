using System.Threading;
using PowerShellUI.Core.Models;

namespace PowerShellUI.Core.Services;

/// <summary>
/// Durchsucht eine Ordnerstruktur nach .ps1-Dateien. Der oberste Unterordner unterhalb des
/// Bibliotheksstamms wird als Kategorie verwendet (z. B. "Azure", "Entra", "ExchangeOnline", "PnP").
/// </summary>
public sealed class ScriptLibraryService
{
    private readonly ScriptIntrospectionService _introspectionService;

    public ScriptLibraryService(ScriptIntrospectionService introspectionService)
    {
        _introspectionService = introspectionService;
    }

    public IReadOnlyList<string> FindScriptFiles(string libraryRoot)
    {
        if (!Directory.Exists(libraryRoot))
        {
            return Array.Empty<string>();
        }

        return Directory.EnumerateFiles(libraryRoot, "*.ps1", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<IReadOnlyList<ScriptInfo>> LoadLibraryAsync(
        string libraryRoot,
        PowerShellHostInfo introspectionHost,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ScriptInfo>();

        foreach (var filePath in FindScriptFiles(libraryRoot))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var category = GetCategory(libraryRoot, filePath);
            var info = await _introspectionService
                .IntrospectAsync(introspectionHost, filePath, category, cancellationToken)
                .ConfigureAwait(false);

            results.Add(info);
        }

        return results;
    }

    private static string GetCategory(string libraryRoot, string filePath)
    {
        var relativePath = Path.GetRelativePath(libraryRoot, filePath);
        var directoryPart = Path.GetDirectoryName(relativePath);

        if (string.IsNullOrEmpty(directoryPart) || directoryPart == ".")
        {
            return "Allgemein";
        }

        return directoryPart.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
    }
}
