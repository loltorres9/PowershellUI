using System.Text.Json;
using PowerShellUI.Core.Models;

namespace PowerShellUI.Core.Services;

internal sealed class ProfileData
{
    public List<TenantProfile> Profiles { get; set; } = new();

    public string? ActiveProfileId { get; set; }
}

/// <summary>
/// Persistiert Mandanten-Profile (<see cref="TenantProfile"/>) sowie das zuletzt aktive Profil
/// lokal je Benutzer. Die Datei liegt unverschlüsselt als JSON im Benutzerprofil — wie bei
/// <see cref="ParameterValueStore"/> nicht für Kennwörter/Secrets geeignet.
/// </summary>
public sealed class ProfileStore
{
    private readonly string _filePath;
    private readonly ProfileData _data;

    public ProfileStore(string? filePath = null)
    {
        _filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PowerShellUI",
            "Profiles.json");

        _data = Load(_filePath);
    }

    public IReadOnlyList<TenantProfile> GetAll() => _data.Profiles;

    public string? ActiveProfileId => _data.ActiveProfileId;

    public TenantProfile? GetActive()
        => _data.Profiles.FirstOrDefault(p => p.Id == _data.ActiveProfileId);

    public void SetActive(string? profileId)
    {
        _data.ActiveProfileId = profileId;
        Persist();
    }

    public void AddOrUpdate(TenantProfile profile)
    {
        var index = _data.Profiles.FindIndex(p => p.Id == profile.Id);
        if (index >= 0)
        {
            _data.Profiles[index] = profile;
        }
        else
        {
            _data.Profiles.Add(profile);
        }

        Persist();
    }

    public void Remove(string profileId)
    {
        _data.Profiles.RemoveAll(p => p.Id == profileId);
        if (_data.ActiveProfileId == profileId)
        {
            _data.ActiveProfileId = null;
        }

        Persist();
    }

    private static ProfileData Load(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return new ProfileData();
            }

            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<ProfileData>(json) ?? new ProfileData();
        }
        catch
        {
            // Beschädigte oder nicht lesbare Datei -> ohne gespeicherte Profile starten.
            return new ProfileData();
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

            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Best effort: ein Schreibfehler (z. B. Berechtigungen) soll die App nicht abstürzen lassen.
        }
    }
}
