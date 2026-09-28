namespace PowerShellUI.Core.Models;

/// <summary>
/// Ein benanntes Profil (z. B. pro M365-Mandant), das wiederkehrende Parameterwerte
/// (Mandanten-ID, PnP-App-Client-ID, Admin-URL, ...) bündelt. Beim Ausführen eines Skripts
/// werden Parameter mit passendem Namen automatisch aus dem aktiven Profil vorausgefüllt.
/// Wie bei den "gemerkten" Werten gilt: unverschlüsselte lokale Speicherung, daher keine
/// Kennwörter/Client-Secrets, nur unkritische Werte wie IDs/URLs.
/// </summary>
public sealed class TenantProfile
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = string.Empty;

    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
