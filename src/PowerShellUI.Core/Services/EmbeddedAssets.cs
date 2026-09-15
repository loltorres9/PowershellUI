namespace PowerShellUI.Core.Services;

internal static class EmbeddedAssets
{
    public static string ExtractToTempFile(string resourceFileName, string targetFileName)
    {
        var assembly = typeof(EmbeddedAssets).Assembly;
        var fullResourceName = $"PowerShellUI.Core.Assets.{resourceFileName}";

        using var stream = assembly.GetManifestResourceStream(fullResourceName)
            ?? throw new InvalidOperationException($"Eingebettete Ressource '{fullResourceName}' wurde nicht gefunden.");

        var tempDirectory = Path.Combine(Path.GetTempPath(), "PowerShellUI");
        Directory.CreateDirectory(tempDirectory);
        var targetPath = Path.Combine(tempDirectory, targetFileName);

        using (var fileStream = File.Create(targetPath))
        {
            stream.CopyTo(fileStream);
        }

        return targetPath;
    }
}
