using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Microsoft.Win32;
using PowerShellUI.Core.Models;
using PowerShellUI.Core.Services;

namespace PowerShellUI.App.ViewModels;

public sealed class EditionOption
{
    public required PowerShellEdition Value { get; init; }

    public required string Label { get; init; }
}

public sealed class MainViewModel : ObservableObject
{
    private readonly PowerShellHostLocator _hostLocator = new();
    private readonly PowerShellProcessRunner _processRunner = new();
    private readonly ScriptIntrospectionService _introspectionService;
    private readonly ScriptLibraryService _libraryService;
    private readonly ModuleManagerService _moduleManagerService;
    private readonly ScriptExecutionService _executionService;

    private string _libraryPath = Path.Combine(AppContext.BaseDirectory, "ScriptLibrary");
    private ScriptInfo? _selectedScript;
    private EditionOption _selectedEdition;
    private bool _isBusy;
    private string _statusMessage = "Bereit.";

    public MainViewModel()
    {
        _introspectionService = new ScriptIntrospectionService(_processRunner);
        _libraryService = new ScriptLibraryService(_introspectionService);
        _moduleManagerService = new ModuleManagerService(_processRunner);
        _executionService = new ScriptExecutionService(_processRunner);

        Editions = new List<EditionOption>
        {
            new() { Value = PowerShellEdition.Auto, Label = "Automatisch (bevorzugt PS 7)" },
            new() { Value = PowerShellEdition.Core7, Label = "PowerShell 7" },
            new() { Value = PowerShellEdition.Desktop51, Label = "Windows PowerShell 5.1" },
        };
        _selectedEdition = Editions[0];

        BrowseLibraryCommand = new RelayCommand(BrowseLibraryAsync);
        LoadLibraryCommand = new RelayCommand(LoadLibraryAsync);
        CheckModulesCommand = new RelayCommand(CheckModulesAsync, () => SelectedScript is not null);
        InstallMissingModulesCommand = new RelayCommand(
            InstallMissingModulesAsync,
            () => RequiredModules.Any(m => m.Status == ModuleStatus.Missing));
        RunScriptCommand = new RelayCommand(RunScriptAsync, () => SelectedScript is not null && !IsBusy);

        _ = LoadLibraryAsync();
    }

    public ObservableCollection<ScriptInfo> Scripts { get; } = new();

    public ObservableCollection<ScriptParameterViewModel> Parameters { get; } = new();

    public ObservableCollection<RequiredModuleViewModel> RequiredModules { get; } = new();

    public ObservableCollection<string> OutputLines { get; } = new();

    public IReadOnlyList<EditionOption> Editions { get; }

    public ICommand BrowseLibraryCommand { get; }

    public ICommand LoadLibraryCommand { get; }

    public ICommand CheckModulesCommand { get; }

    public ICommand InstallMissingModulesCommand { get; }

    public ICommand RunScriptCommand { get; }

    public string LibraryPath
    {
        get => _libraryPath;
        set => SetField(ref _libraryPath, value);
    }

    public ScriptInfo? SelectedScript
    {
        get => _selectedScript;
        set
        {
            if (SetField(ref _selectedScript, value))
            {
                OnSelectedScriptChanged();
            }
        }
    }

    public EditionOption SelectedEdition
    {
        get => _selectedEdition;
        set => SetField(ref _selectedEdition, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetField(ref _isBusy, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    private void OnSelectedScriptChanged()
    {
        Parameters.Clear();
        RequiredModules.Clear();
        OutputLines.Clear();

        if (SelectedScript is null)
        {
            return;
        }

        foreach (var parameter in SelectedScript.Parameters)
        {
            Parameters.Add(new ScriptParameterViewModel(parameter));
        }

        foreach (var module in SelectedScript.RequiredModules)
        {
            RequiredModules.Add(new RequiredModuleViewModel(module));
        }

        if (RequiredModules.Count > 0)
        {
            _ = CheckModulesAsync();
        }
    }

    private async Task BrowseLibraryAsync()
    {
        var dialog = new OpenFolderDialog
        {
            InitialDirectory = Directory.Exists(LibraryPath) ? LibraryPath : AppContext.BaseDirectory,
        };

        if (dialog.ShowDialog() == true)
        {
            LibraryPath = dialog.FolderName;
            await LoadLibraryAsync();
        }
    }

    private async Task LoadLibraryAsync()
    {
        IsBusy = true;
        StatusMessage = "Lade Skript-Bibliothek...";

        try
        {
            var introspectionHost = _hostLocator.Resolve(PowerShellEdition.Auto);
            var scripts = await _libraryService.LoadLibraryAsync(LibraryPath, introspectionHost);

            Scripts.Clear();
            foreach (var script in scripts)
            {
                Scripts.Add(script);
            }

            StatusMessage = $"{Scripts.Count} Skript(e) geladen.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler beim Laden der Bibliothek: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CheckModulesAsync()
    {
        if (SelectedScript is null)
        {
            return;
        }

        PowerShellHostInfo host;
        try
        {
            host = _hostLocator.ResolveForScript(SelectedScript, SelectedEdition.Value);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            return;
        }

        foreach (var moduleViewModel in RequiredModules)
        {
            moduleViewModel.Status = ModuleStatus.Checking;
            var result = await _moduleManagerService.CheckModuleAsync(host, moduleViewModel.Info);
            moduleViewModel.InstalledVersion = result.InstalledVersion;
            moduleViewModel.Status = result.IsInstalled ? ModuleStatus.Installed : ModuleStatus.Missing;
        }
    }

    private async Task InstallMissingModulesAsync()
    {
        if (SelectedScript is null)
        {
            return;
        }

        PowerShellHostInfo host;
        try
        {
            host = _hostLocator.ResolveForScript(SelectedScript, SelectedEdition.Value);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            return;
        }

        IsBusy = true;
        var progress = new Progress<ProcessOutputEventArgs>(e => OutputLines.Add(e.Line));

        try
        {
            foreach (var moduleViewModel in RequiredModules.Where(m => m.Status == ModuleStatus.Missing).ToList())
            {
                moduleViewModel.Status = ModuleStatus.Installing;
                var result = await _moduleManagerService.InstallModuleAsync(host, moduleViewModel.Info, progress);

                if (result.ExitCode == 0)
                {
                    var check = await _moduleManagerService.CheckModuleAsync(host, moduleViewModel.Info);
                    moduleViewModel.InstalledVersion = check.InstalledVersion;
                    moduleViewModel.Status = check.IsInstalled ? ModuleStatus.Installed : ModuleStatus.InstallFailed;
                }
                else
                {
                    moduleViewModel.Status = ModuleStatus.InstallFailed;
                }
            }

            StatusMessage = "Modulinstallation abgeschlossen.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunScriptAsync()
    {
        if (SelectedScript is null)
        {
            return;
        }

        var missingMandatory = Parameters
            .Where(p => p.Info.Mandatory && string.IsNullOrWhiteSpace(p.GetRawValue()))
            .Select(p => p.Name)
            .ToList();

        if (missingMandatory.Count > 0)
        {
            StatusMessage = $"Bitte Pflichtfelder ausfüllen: {string.Join(", ", missingMandatory)}";
            return;
        }

        PowerShellHostInfo host;
        try
        {
            host = _hostLocator.ResolveForScript(SelectedScript, SelectedEdition.Value);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            return;
        }

        var parameterValues = Parameters.ToDictionary(p => p.Name, p => p.GetRawValue());

        IsBusy = true;
        OutputLines.Clear();
        StatusMessage = $"Führe '{SelectedScript.Name}' aus ({host})...";
        var progress = new Progress<ProcessOutputEventArgs>(e => OutputLines.Add(e.Line));

        try
        {
            var result = await _executionService.ExecuteAsync(host, SelectedScript, parameterValues, progress);
            StatusMessage = result.ExitCode == 0
                ? "Skript erfolgreich beendet."
                : $"Skript wurde mit Exitcode {result.ExitCode} beendet.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fehler bei der Ausführung: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
