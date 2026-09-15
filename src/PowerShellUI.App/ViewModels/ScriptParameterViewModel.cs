using PowerShellUI.Core.Models;

namespace PowerShellUI.App.ViewModels;

public enum ParameterInputKind
{
    Text,
    Switch,
    Choice,
}

/// <summary>
/// UI-Wrapper um einen aus dem Skript geparsten Parameter. Stellt je nach Parametertyp
/// (ValidateSet -> Dropdown, switch/bool -> Checkbox, sonst -> Textfeld) den passenden
/// bearbeitbaren Wert für die WPF-Bindings bereit.
/// </summary>
public sealed class ScriptParameterViewModel : ObservableObject
{
    private string? _textValue;
    private bool _switchValue;
    private string? _selectedChoice;

    public ScriptParameterViewModel(ScriptParameterInfo info)
    {
        Info = info;
        _textValue = info.DefaultValue;
        _selectedChoice = info.ValidateSet.Count > 0 ? info.ValidateSet[0] : null;

        Kind = info.ValidateSet.Count > 0
            ? ParameterInputKind.Choice
            : info.IsSwitch
                ? ParameterInputKind.Switch
                : ParameterInputKind.Text;
    }

    public ScriptParameterInfo Info { get; }

    public ParameterInputKind Kind { get; }

    public string Name => Info.Name;

    public string DisplayLabel => Info.Mandatory ? $"{Info.Name} *" : Info.Name;

    public string? HelpMessage => Info.HelpMessage;

    public IReadOnlyList<string> Choices => Info.ValidateSet;

    public string? TextValue
    {
        get => _textValue;
        set => SetField(ref _textValue, value);
    }

    public bool SwitchValue
    {
        get => _switchValue;
        set => SetField(ref _switchValue, value);
    }

    public string? SelectedChoice
    {
        get => _selectedChoice;
        set => SetField(ref _selectedChoice, value);
    }

    public string? GetRawValue() => Kind switch
    {
        ParameterInputKind.Switch => SwitchValue ? "true" : "false",
        ParameterInputKind.Choice => SelectedChoice,
        _ => TextValue,
    };
}
