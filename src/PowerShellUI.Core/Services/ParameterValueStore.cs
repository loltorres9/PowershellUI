using System.Text.Json;

namespace PowerShellUI.Core.Services;

/// <summary>
/// Persistiert Parameterwerte, die der Benutzer explizit "merken" möchte (z. B. eine
/// PnP-App-Client-ID), lokal je Benutzer statt in einem Skript. Werte werden anhand des
/// Parameternamens gespeichert, gelten also skriptübergreifend für gleichnamige Parameter.
/// Die Datei liegt unverschlüsselt als JSON im Benutzerprofil — nicht für Kennwörter/Secrets
/// geeignet, das obliegt der Verantwortung des Benutzers (siehe UI-Hinweis).
/// </summary>
public sealed class ParameterValueStore
{
    private readonly string _filePath;
    private readonly Dictionary<string, string> _values;

    public ParameterValueStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PowerShellUI",
            "SavedParameters.json");

        _values = Load(_filePath);
    }

    public string? Get(string parameterName)
        => _values.TryGetValue(parameterName, out var value) ? value : null;

    public bool IsRemembered(string parameterName) => _values.ContainsKey(parameterName);

    public IReadOnlyDictionary<string, string> GetAll() => _values;

    public void Set(string parameterName, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            Remove(parameterName);
            return;
        }

        _values[parameterName] = value;
        Persist();
    }

    public void Remove(string parameterName)
    {
        if (_values.Remove(parameterName))
        {
            Persist();
        }
    }

    private static Dictionary<string, string> Load(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new Dictionary<string, string>();
            }

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            // Beschädigte oder nicht lesbare Datei -> ohne gespeicherte Werte starten.
            return new Dictionary<string, string>();
        }
    }

    private void Persist()
    {
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_values, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Best effort: ein Schreibfehler (z. B. Berechtigungen) soll die App nicht abstürzen lassen.
        }
    }
}
