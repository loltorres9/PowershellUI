using System.Collections.ObjectModel;
using System.Windows.Input;
using PowerShellUI.Core.Models;

namespace PowerShellUI.App.ViewModels;

/// <summary>
/// ViewModel für den Profil-Editor-Dialog: Name des Profils sowie eine frei erweiterbare
/// Liste von Name/Wert-Paaren (z. B. TenantId, ClientId, AdminUrl), die beim Ausführen von
/// Skripten mit gleichnamigen Parametern automatisch vorausgefüllt werden.
/// </summary>
public sealed class ProfileEditorViewModel : ObservableObject
{
    private string _name;

    public ProfileEditorViewModel(TenantProfile? existing)
    {
        Id = existing?.Id ?? Guid.NewGuid().ToString("N");
        _name = existing?.Name ?? string.Empty;

        Entries = new ObservableCollection<ProfileEntryRowViewModel>();
        foreach (var entry in existing?.Values ?? new Dictionary<string, string>())
        {
            Entries.Add(CreateRow(entry.Key, entry.Value));
        }

        if (Entries.Count == 0)
        {
            Entries.Add(CreateRow("TenantId", string.Empty));
            Entries.Add(CreateRow("ClientId", string.Empty));
        }

        AddEntryCommand = new RelayCommand(() =>
        {
            Entries.Add(CreateRow(string.Empty, string.Empty));
            return Task.CompletedTask;
        });
    }

    public string Id { get; }

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public ObservableCollection<ProfileEntryRowViewModel> Entries { get; }

    public ICommand AddEntryCommand { get; }

    public TenantProfile ToProfile()
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in Entries)
        {
            if (!string.IsNullOrWhiteSpace(entry.Key))
            {
                values[entry.Key.Trim()] = entry.Value ?? string.Empty;
            }
        }

        return new TenantProfile { Id = Id, Name = Name.Trim(), Values = values };
    }

    private ProfileEntryRowViewModel CreateRow(string key, string value)
        => new(key, value, row => Entries.Remove(row));
}
