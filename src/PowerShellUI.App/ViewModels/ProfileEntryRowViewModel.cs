using System.Windows.Input;

namespace PowerShellUI.App.ViewModels;

/// <summary>
/// Eine bearbeitbare Name/Wert-Zeile im Profil-Editor (z. B. "TenantId" / "contoso.onmicrosoft.com").
/// </summary>
public sealed class ProfileEntryRowViewModel : ObservableObject
{
    private string _key;
    private string _value;

    public ProfileEntryRowViewModel(string key, string value, Action<ProfileEntryRowViewModel> onRemove)
    {
        _key = key;
        _value = value;
        RemoveCommand = new RelayCommand(() =>
        {
            onRemove(this);
            return Task.CompletedTask;
        });
    }

    public string Key
    {
        get => _key;
        set => SetField(ref _key, value);
    }

    public string Value
    {
        get => _value;
        set => SetField(ref _value, value);
    }

    public ICommand RemoveCommand { get; }
}
