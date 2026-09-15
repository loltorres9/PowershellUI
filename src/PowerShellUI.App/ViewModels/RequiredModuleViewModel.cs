using PowerShellUI.Core.Models;

namespace PowerShellUI.App.ViewModels;

public sealed class RequiredModuleViewModel : ObservableObject
{
    private ModuleStatus _status = ModuleStatus.Unknown;
    private string? _installedVersion;

    public RequiredModuleViewModel(RequiredModuleInfo info)
    {
        Info = info;
    }

    public RequiredModuleInfo Info { get; }

    public string DisplayName => string.IsNullOrEmpty(Info.Version)
        ? Info.Name
        : $"{Info.Name} (>= {Info.Version})";

    public ModuleStatus Status
    {
        get => _status;
        set
        {
            if (SetField(ref _status, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string? InstalledVersion
    {
        get => _installedVersion;
        set
        {
            if (SetField(ref _installedVersion, value))
            {
                OnPropertyChanged(nameof(StatusText));
            }
        }
    }

    public string StatusText => Status switch
    {
        ModuleStatus.Checking => "Wird geprüft...",
        ModuleStatus.Installed => $"Installiert (v{InstalledVersion})",
        ModuleStatus.Missing => "Fehlt",
        ModuleStatus.Installing => "Wird installiert...",
        ModuleStatus.InstallFailed => "Installation fehlgeschlagen",
        _ => "Unbekannt",
    };
}
